using UnityEngine;

public enum GameState { Menu, Playing, Crashed, Busted, Complete }

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

    float stateTimer;
    int card;

    readonly string[] cards = new string[]
    {
        "LACKLUSTER VIDEO · 1999\n<size=36>Jack, 21. Rewinds tapes. Rewinds his life.</size>",
        "HIS FATHER'S PENDANT\n<size=36>hums when he's angry. Tonight he's furious.</size>",
        "THE FREEWAY JUMP\n<size=36>No one has cleared it. The cops are already rolling.</size>",
        "TAP TO RIDE\n<size=36>W/S throttle · A/D lean · SPACE time-shift</size>",
    };

    void Awake()
    {
        Instance = this;
        State = GameState.Menu;
        card = 0;
    }

    void Start()
    {
        if (hud != null) hud.ShowBanner(cards[0], 9999f);
    }

    void Update()
    {
        if (State == GameState.Menu)
        {
            if (KeyPoll.Down(KeyCode.Return) || KeyPoll.Down(KeyCode.KeypadEnter))
                AdvanceMenu();
            AudioDirector.SetEngine(0f, false);
        }
        else if (State == GameState.Playing)
        {
            GameData.runTime += Time.deltaTime;
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
        if (card >= cards.Length) StartRun();
        else if (hud != null) hud.ShowBanner(cards[card], 9999f);
    }

    public void StartRun()
    {
        State = GameState.Playing;
        TimelineManager.ResetRun();
        GameData.ResetRun();
        WorldBuilder.ApplyTimeline(Timeline.Present);
        if (bike != null) bike.Respawn(new Vector3(WorldBuilder.StartX, 1f, 0f));
        if (hud != null)
        {
            hud.ShowBanner("", 0f);
            hud.RefreshTimelineLabel();
        }
        AudioDirector.SetSiren(false);
        if (chase != null) chase.BeginChase();
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
                string.Format("{0:F1}s · {1} shifts · {2}/{3} coins · top {4:F0} km/h",
                    GameData.runTime, GameData.shiftsUsed, GameData.coinsGot,
                    GameData.coins.Count, GameData.topSpeed) +
                "\nPress R to ride again.</size>", 9999f);
        AudioDirector.PlayWin();
    }
}
