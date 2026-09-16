using UnityEngine;

public enum GameState { Menu, Cutscene, OnFoot, Playing, Crashed, Busted, Complete }

// Owns game state, intro cards, crash/busted/restart, the chase, and the
// win screen with run stats. Created at runtime by GameBootstrap.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public static GameState State = GameState.Menu;

    public BikeController bike;
    public CameraRig cameraRig;
    public Hud hud;
    public PendantFX pendantFx;
    public ChaseCar chase;
    public OnFootController onFoot;
    public CutsceneManager cutscenes;

    float stateTimer;
    int card;
    bool menuBannerShown;

    readonly string[] cards = new string[]
    {
        "LACKLUSTER VIDEO · 1999\n<size=36>Jack, 21. Rewinds tapes. Rewinds his life.</size>",
        "HIS FATHER'S PENDANT\n<size=36>hums when he's angry. Tonight he's furious.</size>",
        "THE FREEWAY JUMP\n<size=36>No one has cleared it. The cops are already rolling.</size>",
        "TAP TO BEGIN\n<size=36>Story first — the ride comes after closing time.\nW/S throttle · A/D lean · SPACE time-shift</size>",
    };

    void Awake()
    {
        Instance = this;
        State = GameState.Menu;
        card = 0;
    }

    void Start()
    {
        menuBannerShown = false;
    }

    void Update()
    {
        if (State == GameState.Menu)
        {
            // Ensure the current card is visible (robust against Start-order issues).
            if (!menuBannerShown)
            {
                if (hud == null)
                    Debug.LogWarning("[TIMEOUT] Menu: hud is NULL, cannot show banner");
                else
                {
                    Debug.Log("[TIMEOUT] Menu: showing card " + card + " via hud.ShowBanner");
                    hud.ShowBanner(cards[card], 9999f);
                    menuBannerShown = true;
                }
            }
            if (KeyPoll.Down(KeyCode.Return) || KeyPoll.Down(KeyCode.KeypadEnter))
                AdvanceMenu();
            AudioDirector.SetEngine(0f, false);
        }
        else if (State == GameState.Cutscene)
        {
            // CutsceneManager drives; tap/Enter advances.
            if (KeyPoll.Down(KeyCode.Return) || KeyPoll.Down(KeyCode.KeypadEnter) || KeyPoll.Down(KeyCode.Space))
                if (cutscenes != null) cutscenes.Advance();
            AudioDirector.SetEngine(0f, false);
        }
        else if (State == GameState.OnFoot)
        {
            GameData.runTime += Time.deltaTime;
            AudioDirector.SetEngine(0f, false);
            if (KeyPoll.Down(KeyCode.R)) RestartRun();
        }
        else if (State == GameState.Playing)
        {
            GameData.runTime += Time.deltaTime;
            TimelineManager.Tick(Time.deltaTime);
            bool throttle = KeyPoll.Held(KeyCode.W) || KeyPoll.Held(KeyCode.UpArrow) || InputState.gas;
            AudioDirector.SetEngine(bike != null ? Mathf.Clamp01(bike.vel.magnitude / 33f) : 0f, throttle);
            if (KeyPoll.Down(KeyCode.R)) RestartRun();
        }
        else if (State == GameState.Crashed || State == GameState.Busted)
        {
            stateTimer -= Time.unscaledDeltaTime;
            AudioDirector.SetEngine(0f, false);
            if (stateTimer <= 0f) RestartRun();
        }
        else if (State == GameState.Complete)
        {
            AudioDirector.SetEngine(0f, false);
            if (KeyPoll.Down(KeyCode.R)) RestartRun();
        }
    }

    public void AdvanceMenu()
    {
        if (State != GameState.Menu) return;
        card++;
        menuBannerShown = false;
        if (card >= cards.Length) BeginStory();
    }

    public void StartRun()
    {
        State = GameState.Playing;
        TimelineManager.ResetRun();
        GameData.ResetRun();
        WorldBuilder.ApplyTimeline(Timeline.Present);
        if (bike != null)
        {
            bike.gameObject.SetActive(true);
            bike.Respawn(new Vector3(WorldBuilder.StartX, 1f, 0f));
        }
        if (onFoot != null) onFoot.gameObject.SetActive(false);
        if (cameraRig != null && bike != null) cameraRig.target = bike.transform;
        if (hud != null)
        {
            hud.ShowBanner("RIDE!\n<size=36>W/S throttle · A/D lean · SPACE time-shift</size>", 3f);
            hud.HideDialogue();
            hud.RefreshTimelineLabel();
        }
        AudioDirector.SetSiren(false);
        if (chase != null) chase.BeginChase();
    }

    // Story flow: Menu -> Cutscene ("lackluster_open") -> OnFoot (inside store)
    // -> Cutscene ("closing_time") -> Playing (the ride).
    public void BeginStory()
    {
        if (cutscenes != null) cutscenes.Play("lackluster_open", EnterStore);
        else EnterStore();
    }

    public void EnterStore()
    {
        // Story flow (simplified): play the BTTF counter bit as a cutscene,
        // then closing time, then the ride. The on-foot walking section is
        // disabled until the interior camera is fixed.
        if (cutscenes != null) cutscenes.Play("bttf_bit", EndShift);
        else EndShift();
    }

    public void EndShift()
    {
        // Closing time: play the pendant scene, then start the ride.
        if (cutscenes != null) cutscenes.Play("closing_time", StartRun);
        else StartRun();
    }

    public void RestartRun()
    {
        StartRun();
    }

    public void Quip()
    {
        if (hud != null) hud.ShowBanner("Okay. The movie made this look way easier.", 2.6f);
    }

    public void OnCrash(Vector3 pos)
    {
        if (State != GameState.Playing) return;
        State = GameState.Crashed;
        stateTimer = 1.2f;
        StopChase();
        if (pendantFx != null) pendantFx.CrashBurst(pos);
        if (cameraRig != null) cameraRig.AddShake(1f);
        if (hud != null) hud.ShowBanner("WRECKED!", 1.2f);
        AudioDirector.PlayCrash();
    }

    public void Busted()
    {
        if (State != GameState.Playing) return;
        State = GameState.Busted;
        stateTimer = 1.8f;
        StopChase();
        if (cameraRig != null) cameraRig.AddShake(0.4f);
        if (hud != null) hud.ShowBanner("BUSTED!\n<size=36>The jump was your only way out.</size>", 1.8f);
        AudioDirector.PlayDenied();
    }

    void StopChase()
    {
        if (chase != null) chase.EndChase();
        else AudioDirector.SetSiren(false);
    }

    public void Complete()
    {
        if (State != GameState.Playing) return;
        State = GameState.Complete;
        StopChase();
        if (hud != null)
            hud.ShowBanner("YOU CLEARED THE FREEWAY!\n<size=36>" +
                string.Format("{0:F1}s · {1} shifts · top {2:F0} km/h",
                    GameData.runTime, GameData.shiftsUsed, GameData.topSpeed) +
                "\nPress R to ride again.</size>", 9999f);
        AudioDirector.PlayWin();
    }
}
