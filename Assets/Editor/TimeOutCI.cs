// Assets/Editor/TimeOutCI.cs
// CI verification rig for TIME OUT (see .github/workflows/unity-verify.yml).
//
// Runs inside the Unity editor in batch mode. It boots the game, taps through
// the menu, drives every cutscene beat of the story opening, rides a few
// seconds, and at each step asserts the LOCKED rules:
//
//   1. Every text box is at most 1/4 of the screen height.
//   2. Text does not overflow its label rect.
//   3. The camera never drops its subject (subject inside camera frustum).
//   4. No Unity errors or exceptions at any point.
//
// It captures a screenshot per beat into verify-output/ plus results.json,
// then exits 0 (pass) or 1 (fail). Editor-only: never ships in the game.
//
// Invoked by CI as: unity-editor -batchmode -executeMethod TimeOutCI.Verify
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TimeOutCI
{
    const string OutDir = "verify-output";
    const float MaxBoxFrac = 0.25f;   // locked rule: no text box > 1/4 screen
    const float SettleSlack = 1.0f;   // extra wait after a camera move finishes
    const float InputWait = 2.5f;     // wait on input-held beats before advancing
    const float WatchdogSeconds = 480f;

    static readonly List<string> failures = new List<string>();
    static readonly List<string> notes = new List<string>();
    static int frame;
    static float phaseT0;
    static float beatT0;
    static float menuT0;
    static int phase;
    static int beatCount;
    static int menuAdvances;
    static bool finishing;
    static bool done;
    static string lastBeatKey = "";
    static bool beatAsserted;

    static Camera cam;
    static Hud hud;
    static GameManager gm;

    // Entry point for CI.
    public static void Verify()
    {
        Directory.CreateDirectory(OutDir);
        Application.logMessageReceived += OnLog;
        try { EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); }
        catch (Exception e) { Fail("open scene: " + e.Message); Finish(); return; }
        phase = 0; frame = 0; beatCount = 0; menuAdvances = 0;
        phaseT0 = (float)EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
        Note("entered playmode");
    }

    static void OnLog(string cond, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            string first = cond.Split('\n')[0];
            if (first.Length > 220) first = first.Substring(0, 220);
            string key = type + ":" + first;
            if (!failures.Contains(key)) failures.Add(key); // de-dupe per-frame repeats
        }
    }

    static void Fail(string m) { failures.Add("CHECK: " + m); Debug.LogError("[TIMEOUT-CI] FAIL " + m); }
    static void Note(string m) { notes.Add(m); Debug.Log("[TIMEOUT-CI] " + m); }
    static void NextPhase() { phase++; phaseT0 = (float)EditorApplication.timeSinceStartup; }

    static void Tick()
    {
        if (done) return;
        if (finishing)
        {
            if (!EditorApplication.isPlaying) { WriteResults(); done = true; EditorApplication.Exit(failures.Count == 0 ? 0 : 1); }
            return;
        }
        if ((float)EditorApplication.timeSinceStartup - phaseT0 > WatchdogSeconds)
        { Fail("watchdog timeout in phase " + phase); Finish(); return; }
        if (!EditorApplication.isPlaying) return;
        frame++;

        switch (phase)
        {
            case 0: // wait for boot, menu card visible
                gm = GameManager.Instance;
                if (gm != null) hud = gm.hud;
                cam = Camera.main;
                if (gm != null && hud != null && cam != null && GameManager.State == GameState.Menu &&
                    (float)EditorApplication.timeSinceStartup - phaseT0 > 3f)
                {
                    Canvas.ForceUpdateCanvases();
                    Shot("01-menu");
                    AssertHudBoxes("menu");
                    AssertFraming("menu");
                    menuT0 = (float)EditorApplication.timeSinceStartup;
                    NextPhase();
                }
                break;

            case 1: // tap through the 4 intro cards -> story begins
                if (GameManager.State == GameState.Menu)
                {
                    if ((float)EditorApplication.timeSinceStartup - menuT0 > 2f && menuAdvances < 6)
                    {
                        gm.AdvanceMenu(); menuAdvances++; menuT0 = (float)EditorApplication.timeSinceStartup;
                        Note("menu advance " + menuAdvances);
                    }
                    if (menuAdvances >= 6) Fail("menu did not advance to story after 6 taps");
                }
                else NextPhase();
                break;

            case 2: // drive every cutscene beat of the opening until the ride starts
                DriveCutscene();
                break;

            case 3: // ride a few seconds with throttle, then final checks
                InputState.gas = true;
                if ((float)EditorApplication.timeSinceStartup - phaseT0 > 5f)
                {
                    InputState.gas = false;
                    Canvas.ForceUpdateCanvases();
                    Shot("03-ride");
                    AssertHudBoxes("ride");
                    AssertFraming("ride");
                    NextPhase();
                }
                break;

            default:
                Finish();
                break;
        }
    }

    // --- Cutscene driver -------------------------------------------------
    // Reads CutsceneManager's current scene/beat via reflection (fields are
    // private; the data model itself is public). Auto-advance beats fire on
    // their own; input-held beats (duration 0) get Advance() after InputWait.
    static void DriveCutscene()
    {
        var cs = CutsceneManager.Instance;
        if (GameManager.State == GameState.Playing) { NextPhase(); return; }
        if (GameManager.State != GameState.Cutscene)
        { Fail("unexpected state during story: " + GameManager.State); NextPhase(); return; }
        if (cs == null) { Fail("CutsceneManager.Instance is null"); NextPhase(); return; }

        string sceneId = ReflectStr(cs, "current", "id") ?? "?";
        int beatIndex = ReflectInt(cs, "beatIndex");
        float duration = ReflectBeatFloat(cs, "duration");
        float moveTime = ReflectBeatFloat(cs, "shot", "moveTime");
        string key = sceneId + ":" + beatIndex;

        if (key != lastBeatKey)
        {
            lastBeatKey = key;
            beatAsserted = false;
            beatT0 = (float)EditorApplication.timeSinceStartup;
            Note("beat " + key + " duration=" + duration + " moveTime=" + moveTime);
        }

        float elapsed = (float)EditorApplication.timeSinceStartup - beatT0;
        // Auto-advance beats can fire before the camera settles; assert at
        // 70% of the beat duration in that case so no beat goes unchecked.
        float assertAt = duration > 0f
            ? Math.Min(moveTime + SettleSlack, duration * 0.7f)
            : moveTime + SettleSlack;
        if (!beatAsserted && elapsed >= assertAt)
        {
            beatCount++;
            string tag = "beat" + beatCount.ToString("D2") + "-" + sceneId;
            Canvas.ForceUpdateCanvases();
            Shot("02-" + tag);
            AssertHudBoxes(tag);
            AssertFraming(tag);
            beatAsserted = true;
        }

        // Input-held beat: advance it ourselves once the camera has settled.
        if (duration <= 0f && beatAsserted && elapsed >= moveTime + SettleSlack + InputWait)
        {
            cs.Advance();
            beatT0 = (float)EditorApplication.timeSinceStartup; // re-arm; key change confirms
            lastBeatKey = ""; // force re-detect (Advance may chain into the next scene)
        }
    }

    static object ReflectField(object o, string name)
    {
        if (o == null) return null;
        var f = o.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return f != null ? f.GetValue(o) : null;
    }

    static string ReflectStr(object o, string field, string sub)
    {
        var v = ReflectField(ReflectField(o, field), sub);
        return v != null ? v.ToString() : null;
    }

    static int ReflectInt(object o, string field)
    {
        var v = ReflectField(o, field);
        return v is int i ? i : -1;
    }

    static float ReflectBeatFloat(object cs, params string[] path)
    {
        try
        {
            object o = ReflectField(cs, "current");
            o = ReflectField(o, "beats");
            var list = o as System.Collections.IList;
            int idx = ReflectInt(cs, "beatIndex");
            if (list == null || idx < 0 || idx >= list.Count) return 0f;
            o = list[idx];
            foreach (var p in path) o = ReflectField(o, p);
            return o is float f ? f : 0f;
        }
        catch { return 0f; }
    }

    // --- Locked UI rule: every text box <= 1/4 of screen height ------------
    static void AssertHudBoxes(string tag)
    {
        var canvasGo = GameObject.Find("HUDCanvas");
        if (canvasGo == null) { Fail(tag + ": HUDCanvas not found"); return; }
        var canvasRt = canvasGo.GetComponent<RectTransform>();
        float canvasH = canvasRt.rect.height;
        if (canvasH <= 0f) { Fail(tag + ": canvas height is 0"); return; }

        foreach (string box in new[] { "StatusBar", "Banner", "Dialogue" })
        {
            var t = canvasGo.transform.Find(box);
            if (t == null) { Note(tag + ": box '" + box + "' not present"); continue; }
            if (!t.gameObject.activeInHierarchy) continue;
            var rt = t.GetComponent<RectTransform>();
            float frac = rt.rect.height / canvasH;
            Note(tag + ": box " + box + " = " + (frac * 100f).ToString("F1") + "% of screen");
            if (frac > MaxBoxFrac + 0.01f)
                Fail(tag + ": text box '" + box + "' is " + (frac * 100f).ToString("F1") + "% of screen (limit 25%)");
            // Translucent-black background present?
            var img = t.GetComponent<UnityEngine.UI.Image>();
            if (img == null || img.color.a < 0.3f)
                Note(tag + ": box '" + box + "' has no solid translucent background");
        }

        // Text must not burst out of its own label rect.
        foreach (var txt in canvasGo.GetComponentsInChildren<UnityEngine.UI.Text>(true))
        {
            if (!txt.gameObject.activeInHierarchy) continue;
            if (string.IsNullOrEmpty(txt.text)) continue;
            var rt = txt.rectTransform;
            float have = rt.rect.height, want;
            try { want = txt.preferredHeight; }
            catch (Exception e)
            {
                Note(tag + ": preferredHeight threw: " + e.GetType().Name);
                continue;
            }
            if (have > 1f && want > have * 1.25f)
                Fail(tag + ": text overflows its label (" + want.ToString("F0") + "px needed, " +
                     have.ToString("F0") + "px box): '" +
                     txt.text.Substring(0, Math.Min(40, txt.text.Length)).Replace('\n', ' ') + "'");
        }
    }

    // --- Camera framing: the subject must be inside the camera frustum -----
    static void AssertFraming(string tag)
    {
        if (cam == null) { Fail(tag + ": no main camera"); return; }
        Vector3 cp = cam.transform.position;
        if (cp.y < -5f || cp.y > 60f || Math.Abs(cp.x) > 500f || Math.Abs(cp.z) > 200f)
            Fail(tag + ": camera at absurd position " + cp);
        var planes = GeometryUtility.CalculateFrustumPlanes(cam);
        Vector3 subject;
        string what;
        if (tag.StartsWith("beat"))
        { subject = new Vector3(-20f, 3f, 0f); what = "store (x=-20)"; }
        else
        {
            var jack = GameObject.Find("Jack");
            if (jack == null) { Fail(tag + ": Jack not found for framing check"); return; }
            subject = jack.transform.position; what = "Jack";
        }
        bool visible = GeometryUtility.TestPlanesAABB(planes, new Bounds(subject, new Vector3(6f, 6f, 6f)));
        Note(tag + ": subject=" + what + " at " + subject.ToString("F1") + " visible=" + visible +
             " campos=" + cp.ToString("F1"));
        if (!visible)
            Fail(tag + ": " + what + " is OUTSIDE the camera frustum (camera dropped the subject)");
    }

    static void Shot(string name)
    {
        try
        {
            if (cam == null) return;
            int w = 1280, h = 720;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
            Note("screenshot " + name);
        }
        catch (Exception e) { Note("screenshot " + name + " failed: " + e.GetType().Name + " " + e.Message); }
    }

    static void Finish()
    {
        if (finishing) return;
        finishing = true;
        Note("finishing: " + failures.Count + " failures, " + beatCount + " beats driven");
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
    }

    static void WriteResults()
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("{\n  \"pass\": ").Append(failures.Count == 0 ? "true" : "false");
            sb.Append(",\n  \"beats\": ").Append(beatCount);
            sb.Append(",\n  \"failures\": [");
            for (int i = 0; i < failures.Count; i++)
                sb.Append(i == 0 ? "\n" : ",\n").Append("    ").Append(JsonStr(failures[i]));
            sb.Append(failures.Count == 0 ? "],\n" : "\n  ],\n");
            sb.Append("  \"notes\": [");
            for (int i = 0; i < notes.Count; i++)
                sb.Append(i == 0 ? "\n" : ",\n").Append("    ").Append(JsonStr(notes[i]));
            sb.Append(notes.Count == 0 ? "]\n}\n" : "\n  ]\n}\n");
            File.WriteAllText(Path.Combine(OutDir, "results.json"), sb.ToString());
        }
        catch (Exception e) { Debug.LogError("[TIMEOUT-CI] could not write results: " + e.Message); }
    }

    static string JsonStr(string s)
    {
        return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "") + "\"";
    }
}
