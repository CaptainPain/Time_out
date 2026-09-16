using UnityEngine;
using UnityEngine.EventSystems;

// ONE-TIME SETUP: create an empty GameObject in your scene, attach this
// script, press Play. Everything else (world, bike, camera, HUD, audio)
// is built from code at runtime. No prefabs, no art assets.
//
// RESTART-SAFE: if Unity's "Reload Scene" is disabled (Enter Play Mode
// Options), pressing Stop then Play does NOT reload the scene, so Awake()
// never re-runs and the old session's objects linger. The
// RuntimeInitializeOnLoadMethod below fires on EVERY Play press with a fresh
// session ID; the boot only runs once per session ID, tearing down leftovers
// first, so the menu always shows.
public class GameBootstrap : MonoBehaviour
{
    static string currentSessionId;
    string bootedSessionId;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnPlayPressed()
    {
        currentSessionId = System.Guid.NewGuid().ToString();
        // Destroy any stale GameBootstrap hosts; create one fresh host.
        foreach (var gb in FindObjectsByType<GameBootstrap>())
            Object.DestroyImmediate(gb.gameObject);
        var go = new GameObject("GameBootstrap");
        var fresh = go.AddComponent<GameBootstrap>();
        fresh.EnsureBooted(currentSessionId);
    }

    void EnsureBooted(string sessionId)
    {
        if (bootedSessionId == sessionId) return; // already booted for this Play press
        bootedSessionId = sessionId;
        CleanupPreviousSession();
        Boot();
    }

    // Destroy runtime objects left over from a previous Play session
    // (only happens when the scene wasn't reloaded on Stop). Nuclear option:
    // destroy EVERYTHING at the scene root except the camera, the light, and
    // this GameBootstrap's own GameObject. Anything the game built at runtime
    // (world, clouds, Jack, HUD, etc.) is gone, no matter what it's named.
    static void CleanupPreviousSession()
    {
        // Destroy any leftover Hud/Canvas from a previous session first —
        // a stale Canvas rendering on top would hide the new menu banner.
        foreach (var hud in FindObjectsByType<Hud>())
            Object.DestroyImmediate(hud.gameObject);
        foreach (var canvas in FindObjectsByType<Canvas>())
            Object.DestroyImmediate(canvas.gameObject);
        foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go.name == "Main Camera") continue;
            if (go.name == "Directional Light") continue;
            if (go.GetComponent<GameBootstrap>() != null) continue; // don't destroy ourselves
            Object.DestroyImmediate(go);
        }
        GameManager.Instance = null;
        GameManager.State = GameState.Menu;
    }

    void Boot()
    {
        // Landscape on phones.
        if (Application.isMobilePlatform)
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        Application.targetFrameRate = 60;

        InputState.Reset();
        TimelineManager.Reset();

        // World (all three timelines).
        WorldBuilder.Build();

        // Game manager.
        var gm = new GameObject("GameManager").AddComponent<GameManager>();

        // Jack + bike.
        var bikeGo = new GameObject("Jack");
        var bike = bikeGo.AddComponent<BikeController>();
        bikeGo.transform.position = new Vector3(WorldBuilder.StartX, 1f, 0f);

        // Time-cop cruiser (starts hidden; the run begins the chase).
        var chaseGo = new GameObject("ChaseCar");
        var chase = chaseGo.AddComponent<ChaseCar>();
        chase.bike = bike;

        // Camera.
        Camera cam = Camera.main;
        if (cam == null) cam = new GameObject("Main Camera").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.tag = "MainCamera";
        var rig = cam.gameObject.GetComponent<CameraRig>();
        if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();
        rig.target = bikeGo.transform;
        rig.SnapTo(bikeGo.transform.position);

        // Sky color needs a live camera, so apply after it exists.
        WorldBuilder.ApplyTimeline(Timeline.Present);

        // UI buttons and touch controls need an EventSystem or taps do nothing.
        // The project runs on the new Input System, so the UI gets its module.
        if (EventSystem.current == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Pendant FX, audio, HUD.
        var fx = new GameObject("PendantFX").AddComponent<PendantFX>();
        new GameObject("AudioDirector").AddComponent<AudioDirector>();
        var hud = new GameObject("HUD").AddComponent<Hud>();

        // Story systems (v9).
        var csGo = new GameObject("CutsceneManager");
        var cutscenes = csGo.AddComponent<CutsceneManager>();
        var camDirGo = new GameObject("CameraDirector");
        camDirGo.AddComponent<CameraDirector>();

        // On-foot Jack (starts disabled; enabled inside Lackluster Video).
        var footGo = new GameObject("JackOnFoot");
        var onFoot = footGo.AddComponent<OnFootController>();
        footGo.SetActive(false);

        // Town locations.
        LacklusterVideo.Build(WorldBuilder.TownRoot);
        MomsDiner.Build(WorldBuilder.TownRoot);

        // Present-timeline obstacles (barrels, cars, pallets, food trucks).
        ObstacleManager.Build(WorldBuilder.TownRoot);

        // Ambient life.
        var npcGo = new GameObject("NPCManager");
        var npcs = npcGo.AddComponent<NPCManager>();
        NPCManager.Build(WorldBuilder.TownRoot);
        var trafficGo = new GameObject("TrafficManager");
        var traffic = trafficGo.AddComponent<TrafficManager>();
        TrafficManager.Build(WorldBuilder.TownRoot);

        gm.bike = bike;
        gm.cameraRig = rig;
        gm.hud = hud;
        gm.pendantFx = fx;
        gm.chase = chase;
        gm.onFoot = onFoot;
        gm.cutscenes = cutscenes;
        // Tick ambient systems.
        var ticker = new GameObject("TownTicker").AddComponent<TownTicker>();
        ticker.npcs = npcs;
        ticker.traffic = traffic;
        ticker.gm = gm;
    }
}

// Per-frame tick for ambient town systems (NPCs react to player position,
// traffic loops). Separated so NPCManager/TrafficManager stay static builders.
public class TownTicker : MonoBehaviour
{
    public NPCManager npcs;
    public TrafficManager traffic;
    public GameManager gm;

    void Update()
    {
        Vector3 pp = gm != null && gm.bike != null ? gm.bike.transform.position : Vector3.zero;
        if (GameManager.State == GameState.OnFoot && gm != null && gm.onFoot != null)
            pp = gm.onFoot.transform.position;
        if (npcs != null) npcs.Tick(pp);
        if (traffic != null) traffic.Tick(Time.deltaTime);
    }
}
