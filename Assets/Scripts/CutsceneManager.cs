using System;
using System.Collections.Generic;
using UnityEngine;

// NOTE: the global `Motif` enum lives in AudioDirector.cs (added by parent).
// This file references it (Motif.Clean / .Lonely / .BluesRock /
// .DarkDistorted / .None) and calls AudioDirector.PlayMotif(Motif).

// Data-driven cutscene player. Scenes are authored in code as beat lists;
// each beat frames a camera shot and shows dialogue. Brief by design —
// cutscenes are transitions into gameplay, not movies (locked rule).
//
// Wiring (parent): GameBootstrap must AddComponent<CutsceneManager>() and
// AddComponent<CameraDirector>() (on the Main Camera, next to CameraRig).
// Hud already exposes ShowDialogue/HideDialogue/SetContinueHint.
public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance;

    // Static flag the camera rig reads: while true, CameraDirector drives
    // the camera and CameraRig.LateUpdate yields.
    public static bool IsPlaying { get; private set; }

    // ---- Data model ----
    [Serializable]
    public struct CameraShot
    {
        public Vector3 pos;
        public Vector3 lookAt;
        public float fov;
        public float moveTime;
        public CameraShot(Vector3 pos, Vector3 lookAt, float fov, float moveTime)
        {
            this.pos = pos;
            this.lookAt = lookAt;
            this.fov = fov;
            this.moveTime = moveTime;
        }
    }

    [Serializable]
    public struct Beat
    {
        public string speaker;  // "" = narration (no name shown)
        public string text;
        public CameraShot shot;
        public float duration;  // >0: auto-advance after this many seconds; 0 = wait for input
        public Beat(string speaker, string text, CameraShot shot, float duration)
        {
            this.speaker = speaker;
            this.text = text;
            this.shot = shot;
            this.duration = duration;
        }
    }

    public class Scene
    {
        public string id;
        public Motif motif;
        public List<Beat> beats = new List<Beat>();
        public Scene(string id, Motif motif)
        {
            this.id = id;
            this.motif = motif;
        }
        public Scene Add(string speaker, string text, CameraShot shot, float duration)
        {
            beats.Add(new Beat(speaker, text, shot, duration));
            return this;
        }
    }

    // ---- Runtime state ----
    readonly Dictionary<string, Scene> scenes = new Dictionary<string, Scene>();
    Scene current;
    int beatIndex;
    float beatTimer;
    Action onComplete;
    Hud hud;

    // Lackluster Video storefront anchor. Matches LacklusterVideo.Build
    // (store centered at x=-20).
    const float StoreX = -20f;

    void Awake()
    {
        Instance = this;
        BuildScenes();
    }

    void Update()
    {
        if (!IsPlaying) return;
        if (KeyPoll.Down(KeyCode.Return) || KeyPoll.Down(KeyCode.KeypadEnter) || KeyPoll.Down(KeyCode.Space))
        {
            Advance();
            return;
        }
        if (beatTimer > 0f)
        {
            beatTimer -= Time.deltaTime;
            if (beatTimer <= 0f) Advance();
        }
    }

    // Starts a scene. onComplete fires when the scene ends or is skipped;
    // it owns the handoff (e.g. entering OnFoot gameplay) and the next state.
    public void Play(string sceneId, Action onComplete)
    {
        if (IsPlaying) return;
        Scene scene;
        if (!scenes.TryGetValue(sceneId, out scene))
        {
            Debug.LogWarning("[TIMEOUT] Cutscene '" + sceneId + "' not found.");
            if (onComplete != null) onComplete();
            return;
        }
        this.onComplete = onComplete;
        current = scene;
        beatIndex = 0;
        IsPlaying = true;
        GameManager.State = GameState.Cutscene;
        if (GameManager.Instance != null && GameManager.Instance.hud != null)
            GameManager.Instance.hud.ShowBanner("", 0f); // clear any menu banner
        AudioDirector.PlayMotif(current.motif);
        ShowBeat();
    }

    // Tap / Enter: advance to the next beat (or end the scene).
    public void Advance()
    {
        if (!IsPlaying) return;
        beatIndex++;
        if (current == null || beatIndex >= current.beats.Count) End();
        else ShowBeat();
    }

    // Skip straight to the end; onComplete still fires.
    public void Skip()
    {
        if (!IsPlaying) return;
        End();
    }

    void ShowBeat()
    {
        Beat b = current.beats[beatIndex];
        if (CameraDirector.Instance != null)
            CameraDirector.Instance.Shot(b.shot.pos, b.shot.lookAt, b.shot.fov, b.shot.moveTime);
        if (hud == null && GameManager.Instance != null) hud = GameManager.Instance.hud;
        if (hud != null)
        {
            hud.ShowDialogue(b.speaker, b.text);
            hud.SetContinueHint(true);
        }
        beatTimer = b.duration;
    }

    void End()
    {
        IsPlaying = false;
        if (hud != null) hud.HideDialogue();
        if (CameraDirector.Instance != null) CameraDirector.Instance.EndCinematic();
        Action cb = onComplete;
        onComplete = null;
        current = null;
        if (cb != null) cb();
    }

    // ---- Scene authoring (from ~/memory/time-out/script.md) ----

    void BuildScenes()
    {
        // CINEMATIC 05 — "LACKLUSTER VIDEO"
        // Jack arrives, the sign buzzes yellow/blue, he walks in, the guitar
        // disappears, fluorescent hum — ordinary, safe, boring.
        // Hands off to on-foot gameplay inside the store.
        var open = new Scene("lackluster_open", Motif.None);
        open.Add("", "The LACKLUSTER VIDEO sign buzzes — yellow and blue.",
            new CameraShot(new Vector3(StoreX, 1.2f, -13f), new Vector3(StoreX, 9f, 0f), 55f, 2.5f), 5f);
        open.Add("", "Jack kills the engine and heads inside.",
            new CameraShot(new Vector3(StoreX - 7f, 2.2f, -11f), new Vector3(StoreX, 2.5f, 0f), 50f, 3f), 5f);
        open.Add("", "The guitar disappears. Fluorescent lights hum.",
            new CameraShot(new Vector3(StoreX + 4f, 3.2f, -10f), new Vector3(StoreX - 1f, 3f, 0f), 50f, 3f), 5f);
        open.Add("", "Ordinary. Safe. Boring. — Shift starts now.",
            new CameraShot(new Vector3(StoreX + 1.5f, 3.6f, -6.5f), new Vector3(StoreX - 1.5f, 3f, 0f), 42f, 2.5f), 0f);
        scenes.Add(open.id, open);

        // CINEMATIC 06 — "CLOSING TIME"
        // 8:57 PM, empty store, Jack behind the counter, clock ticks to 8:59,
        // he pulls out the pendant: "Dad... You ever get bored?"
        // Lights flicker. Hands off to the ride.
        var closing = new Scene("closing_time", Motif.Lonely);
        closing.Add("", "8:57 PM. The store is empty. The lights are dim.",
            new CameraShot(new Vector3(StoreX + 4f, 3.4f, -11f), new Vector3(StoreX - 1f, 3f, 0f), 50f, 3f), 5f);
        closing.Add("", "Jack sits behind the counter. The clock ticks.",
            new CameraShot(new Vector3(StoreX - 3f, 4.2f, -5f), new Vector3(StoreX - 3f, 5.2f, 0f), 35f, 2.5f), 4f);
        closing.Add("JACK", "Dad...",
            new CameraShot(new Vector3(StoreX - 1f, 3.2f, -4.5f), new Vector3(StoreX - 1.5f, 3.4f, 0f), 32f, 2f), 0f);
        closing.Add("JACK", "You ever get bored?",
            new CameraShot(new Vector3(StoreX - 0.8f, 3.1f, -4f), new Vector3(StoreX - 1.5f, 3.3f, 0f), 30f, 2f), 0f);
        closing.Add("", "8:59. He pulls out his father's pendant.",
            new CameraShot(new Vector3(StoreX - 1f, 2.8f, -3.5f), new Vector3(StoreX - 1.2f, 3f, 0f), 28f, 2f), 4f);
        closing.Add("JACK", "You left... didn't you?",
            new CameraShot(new Vector3(StoreX - 1f, 3.2f, -4.5f), new Vector3(StoreX - 1.5f, 3.4f, 0f), 32f, 2f), 0f);
        closing.Add("", "The lights flicker. The pendant begins to glow.",
            new CameraShot(new Vector3(StoreX, 3.5f, -8f), new Vector3(StoreX - 1f, 3f, 0f), 45f, 2.5f), 0f);
        scenes.Add(closing.id, closing);

        // The BTTF comedy bit: Jack recommends Back to the Future, the customer
        // has seen all three. "You people make this job very difficult."
        var bttf = new Scene("bttf_bit", Motif.None);
        // Frame Jack at the counter (counter at StoreX, z=1.2; Jack at z=-1).
        // Camera in front of the store, low enough to see the counter.
        bttf.Add("CUSTOMER", "Got anything good?",
            new CameraShot(new Vector3(StoreX, 2f, -6f), new Vector3(StoreX, 1f, 0f), 40f, 2f), 0f);
        bttf.Add("JACK", "Back to the Future. Changed my life.",
            new CameraShot(new Vector3(StoreX - 2f, 1.8f, -5f), new Vector3(StoreX, 1.2f, 1f), 32f, 2f), 0f);
        bttf.Add("CUSTOMER", "Seen it.",
            new CameraShot(new Vector3(StoreX, 2f, -6f), new Vector3(StoreX, 1f, 0f), 40f, 1.5f), 0f);
        bttf.Add("JACK", "Part II? Part III?",
            new CameraShot(new Vector3(StoreX - 2f, 1.8f, -5f), new Vector3(StoreX, 1.2f, 1f), 32f, 1.5f), 0f);
        bttf.Add("CUSTOMER", "Seen 'em.",
            new CameraShot(new Vector3(StoreX, 2f, -6f), new Vector3(StoreX, 1f, 0f), 40f, 1.5f), 0f);
        bttf.Add("JACK", "You people make this job very difficult.",
            new CameraShot(new Vector3(StoreX - 2f, 1.8f, -5f), new Vector3(StoreX, 1.2f, 1f), 32f, 2f), 0f);
        scenes.Add(bttf.id, bttf);

        // CINEMATIC 02 — "MOM'S RESTAURANT"
        // Camera stays outside; Jack and Mom seen through the diner window,
        // dialogue inaudible. Soft clean guitar, camera pushes toward window.
        var mom = new Scene("mom_window", Motif.Clean);
        mom.Add("", "Across the street: MOM'S. Warm light in the window.",
            new CameraShot(new Vector3(31f - 12f, 3.5f, -14f), new Vector3(31f, 2.5f, 0f), 50f, 4f), 5f);
        mom.Add("", "Through the glass: Jack and his mom. No words carry.",
            new CameraShot(new Vector3(31f - 7f, 3f, -9f), new Vector3(31f, 2.8f, 0f), 38f, 4f), 5f);
        mom.Add("", "She laughs at something. He almost smiles.",
            new CameraShot(new Vector3(31f - 4f, 2.8f, -6f), new Vector3(31f, 2.8f, 0f), 30f, 3f), 4f);
        mom.Add("", "The camera drifts closer. The pendant on the wall glints.",
            new CameraShot(new Vector3(31f - 2f, 2.6f, -4f), new Vector3(31f, 3.2f, 0f), 26f, 3f), 0f);
        scenes.Add(mom.id, mom);
    }
}
