using UnityEngine;
using UnityEngine.EventSystems;

// ONE-TIME SETUP: create an empty GameObject in your scene, attach this
// script, press Play. Everything else (world, bike, camera, HUD, audio)
// is built from code at runtime. No prefabs, no art assets.
public class GameBootstrap : MonoBehaviour
{
    void Awake()
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
        var rig = cam.gameObject.AddComponent<CameraRig>();
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

        gm.bike = bike;
        gm.cameraRig = rig;
        gm.hud = hud;
        gm.pendantFx = fx;
        gm.chase = chase;
    }
}
