using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

// Touch/keyboard input flags. Keyboard is polled directly by the bike;
// on-screen buttons set these on mobile.
public static class InputState
{
    public static bool gas, brake, leanFwd, leanBack;

    public static void Reset()
    {
        gas = brake = leanFwd = leanBack = false;
    }
}

public class Hud : MonoBehaviour
{
    Text timelineLabel;
    Text speedLabel;
    Text hintLabel;
    GameObject bannerRoot;
    Text bannerLabel;
    Text cooldownLabel;
    Image cooldownFill;
    float bannerTimer;
    float labelPop;
    GameObject menuTap;
    GameObject touchRoot;
    // Dialogue UI (cutscenes).
    GameObject dialogueRoot;
    Text dialogueSpeaker;
    Text dialogueText;
    Text continueHint;

    void Awake()
    {
        var canvasGo = new GameObject("HUDCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        var root = canvasGo.transform;

        // Top status bar (header): dark translucent strip holding ALL hud text
        // (timeline, pendant, speed, hints). Story text goes on the bottom.
        var statusGo = new GameObject("StatusBar");
        statusGo.transform.SetParent(root, false);
        var srt = statusGo.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 1f); srt.anchorMax = new Vector2(1f, 1f);
        srt.pivot = new Vector2(0.5f, 1f);
        srt.anchoredPosition = Vector2.zero;
        srt.sizeDelta = new Vector2(0f, 96f);
        var sbg = statusGo.AddComponent<Image>();
        sbg.color = new Color(0f, 0f, 0f, 0.55f);
        sbg.raycastTarget = false;

        timelineLabel = AddLabel(statusGo.transform, "", 34, Color.white, 0f, 0.5f, new Vector2(24f, 0f));
        timelineLabel.alignment = TextAnchor.MiddleLeft;
        cooldownLabel = AddLabel(statusGo.transform, "PENDANT", 20, new Color(1f, 1f, 1f, 0.85f), 0.5f, 0.5f, new Vector2(-190f, 10f));
        var barBg = AddBar(statusGo.transform, new Vector2(0.5f, 0.5f), new Vector2(-20f, -12f), new Vector2(340f, 14f), new Color(0f, 0f, 0f, 0.45f));
        cooldownFill = AddBar(barBg.transform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(340f, 14f), new Color(0.4f, 0.9f, 1f, 0.95f));
        var fillRt = cooldownFill.rectTransform;
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.anchoredPosition = Vector2.zero;

        speedLabel = AddLabel(statusGo.transform, "", 30, Color.white, 1f, 0.5f, new Vector2(-24f, 0f));
        speedLabel.alignment = TextAnchor.MiddleRight;

        hintLabel = AddLabel(statusGo.transform, "", 20, new Color(1f, 1f, 1f, 0.9f), 0.5f, 0.5f, new Vector2(180f, 10f));
        hintLabel.alignment = TextAnchor.MiddleCenter;

        // Banner: compact panel with dark background (max 1/4 screen).
        // ALL banner text lives inside this box — nothing floats over the sky.
        // Positioned at the bottom of the screen, above the status bar.
        bannerRoot = new GameObject("Banner");
        bannerRoot.transform.SetParent(root, false);
        var brt = bannerRoot.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 0f); brt.anchorMax = new Vector2(1f, 0f);
        brt.pivot = new Vector2(0.5f, 0f);
        brt.anchoredPosition = new Vector2(0f, 8f);
        brt.sizeDelta = new Vector2(1400f, 220f);
        var bbg = bannerRoot.AddComponent<Image>();
        bbg.color = new Color(0f, 0f, 0f, 0.72f);
        bbg.raycastTarget = false;
        bannerLabel = AddLabel(bannerRoot.transform, "", 52, Color.white, 0.5f, 0.5f, Vector2.zero);
        var blrt = bannerLabel.rectTransform;
        blrt.anchorMin = Vector2.zero; blrt.anchorMax = Vector2.one;
        blrt.offsetMin = Vector2.zero; blrt.offsetMax = Vector2.zero;
        bannerRoot.SetActive(false);

        // Tap-to-start layer for the menu.
        menuTap = new GameObject("MenuTap");
        menuTap.transform.SetParent(root, false);
        var tapImg = menuTap.AddComponent<Image>();
        tapImg.color = new Color(0f, 0f, 0f, 0f);
        tapImg.raycastTarget = true;
        var tapRt = menuTap.GetComponent<RectTransform>();
        tapRt.anchorMin = Vector2.zero; tapRt.anchorMax = Vector2.one;
        tapRt.offsetMin = Vector2.zero; tapRt.offsetMax = Vector2.zero;
        var tapBtn = menuTap.AddComponent<Button>();
        tapBtn.onClick.AddListener(() => {
            if (GameManager.State == GameState.Menu) GameManager.Instance.AdvanceMenu();
            else if (GameManager.State == GameState.Cutscene && CutsceneManager.Instance != null) CutsceneManager.Instance.Advance();
        });

        BuildDialogue(root);
        BuildSkip(root);

        if (Application.isMobilePlatform) BuildTouch(root);
        else hintLabel.text = "W/S throttle · A/D lean · SPACE time-jump · R restart";

        TimelineManager.OnShift += OnShift;
        RefreshTimelineLabel();
    }

    void OnDestroy()
    {
        TimelineManager.OnShift -= OnShift;
    }

    Text AddLabel(Transform parent, string text, int size, Color color, float ax, float ay, Vector2 offset)
    {
        var t = UIUtil.MakeLabel(text, size, color);
        t.transform.SetParent(parent, false);
        var rt = t.rectTransform;
        rt.anchorMin = new Vector2(ax, ay);
        rt.anchorMax = new Vector2(ax, ay);
        rt.pivot = new Vector2(ax, ay);
        rt.anchoredPosition = offset;
        rt.sizeDelta = new Vector2(1200f, size * 2.2f);
        return t;
    }

    Image AddBar(Transform parent, Vector2 anchor, Vector2 offset, Vector2 size, Color color)
    {
        var go = new GameObject("Bar");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = size;
        return img;
    }

    void OnShift(Timeline t)
    {
        RefreshTimelineLabel();
        labelPop = 1f;
        AudioDirector.PlayShift();
        WorldBuilder.ApplyTimeline(t);
        NPCManager.ApplyTimeline(t);
        TrafficManager.ApplyTimeline(t);
        MomsDiner.ApplyTimeline(t);
    }

    public void RefreshTimelineLabel()
    {
        var t = TimelineManager.Active;
        timelineLabel.text = TimelineManager.Year(t) + " · " + TimelineManager.Name(t);
    }

    public void ShowBanner(string text, float dur)
    {
        if (string.IsNullOrEmpty(text))
        {
            bannerRoot.SetActive(false);
            bannerTimer = 0f;
            return;
        }
        bannerLabel.text = text;
        bannerRoot.SetActive(true);
        bannerTimer = dur;
    }

    void BuildDialogue(Transform root)
    {
        dialogueRoot = new GameObject("Dialogue");
        dialogueRoot.transform.SetParent(root, false);
        var drt = dialogueRoot.AddComponent<RectTransform>();
        // Bottom of screen: story text box. Translucent black, max 1/4 height.
        drt.anchorMin = new Vector2(0f, 0f); drt.anchorMax = new Vector2(1f, 0f);
        drt.pivot = new Vector2(0.5f, 0f);
        drt.anchoredPosition = new Vector2(0f, 8f);
        drt.sizeDelta = new Vector2(1600f, 140f);
        var bg = dialogueRoot.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);
        bg.raycastTarget = false;

        dialogueSpeaker = AddLabel(dialogueRoot.transform, "", 24, new Color(1f, 0.85f, 0.4f), 0f, 1f, new Vector2(40f, -12f));
        dialogueSpeaker.alignment = TextAnchor.UpperLeft;
        dialogueText = AddLabel(dialogueRoot.transform, "", 27, Color.white, 0f, 1f, new Vector2(40f, -48f));
        dialogueText.alignment = TextAnchor.UpperLeft;
        var trt = dialogueText.rectTransform;
        trt.sizeDelta = new Vector2(1520f, 90f);

        continueHint = AddLabel(root, "TAP TO CONTINUE  ·  ENTER", 18, new Color(1f, 1f, 1f, 0.6f), 1f, 0f, new Vector2(-40f, 112f));
        continueHint.alignment = TextAnchor.LowerRight;
        continueHint.gameObject.SetActive(false);

        dialogueRoot.SetActive(false);
    }

    GameObject skipBtn;
    void BuildSkip(Transform root)
    {
        skipBtn = new GameObject("SkipBtn");
        skipBtn.transform.SetParent(root, false);
        var img = skipBtn.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.5f);
        img.raycastTarget = true;
        var rt = skipBtn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-24f, -24f);
        rt.sizeDelta = new Vector2(160f, 64f);
        var label = UIUtil.MakeLabel("SKIP →", 28, Color.white);
        label.transform.SetParent(skipBtn.transform, false);
        var lrt = label.rectTransform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var btn = skipBtn.AddComponent<Button>();
        btn.onClick.AddListener(() => {
            if (GameManager.State == GameState.Cutscene && CutsceneManager.Instance != null)
                CutsceneManager.Instance.Skip();
            else if (GameManager.State == GameState.OnFoot && GameManager.Instance != null)
                GameManager.Instance.StartRun();
        });
        skipBtn.SetActive(false);
    }

    public void ShowDialogue(string speaker, string text)
    {
        dialogueSpeaker.text = speaker ?? "";
        dialogueSpeaker.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
        dialogueText.text = text ?? "";
        dialogueRoot.SetActive(true);
        SetContinueHint(true);
    }

    public void HideDialogue()
    {
        dialogueRoot.SetActive(false);
        SetContinueHint(false);
    }

    public void SetContinueHint(bool on)
    {
        if (continueHint != null) continueHint.gameObject.SetActive(on);
    }

    void BuildTouch(Transform root)
    {
        hintLabel.text = "";
        touchRoot = new GameObject("TouchControls");
        touchRoot.transform.SetParent(root, false);
        var trt = touchRoot.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        var tr = touchRoot.transform;
        AddTouchButton(tr, "BRAKE", new Vector2(0f, 0f), new Vector2(50f, 50f), new Vector2(300f, 180f),
            () => InputState.brake = true, () => InputState.brake = false);
        AddTouchButton(tr, "GAS", new Vector2(1f, 0f), new Vector2(-50f, 50f), new Vector2(300f, 180f),
            () => InputState.gas = true, () => InputState.gas = false);
        AddTouchButton(tr, "▲", new Vector2(0f, 0f), new Vector2(50f, 250f), new Vector2(140f, 140f),
            () => InputState.leanBack = true, () => InputState.leanBack = false);
        AddTouchButton(tr, "▼", new Vector2(0f, 0f), new Vector2(205f, 250f), new Vector2(140f, 140f),
            () => InputState.leanFwd = true, () => InputState.leanFwd = false);
        AddTouchButton(tr, "TIME", new Vector2(1f, 0f), new Vector2(-70f, 260f), new Vector2(220f, 220f),
            () => { if (!TimelineManager.TryShift()) AudioDirector.PlayDenied(); }, null);
    }

    void AddTouchButton(Transform root, string label, Vector2 anchor, Vector2 offset, Vector2 size, Action down, Action up)
    {
        var go = new GameObject("Touch_" + label);
        go.transform.SetParent(root, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.22f);
        img.raycastTarget = true;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = size;
        var t = UIUtil.MakeLabel(label, 40, Color.white);
        t.transform.SetParent(go.transform, false);
        var trt = t.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        var trig = go.AddComponent<EventTrigger>();
        AddTrigger(trig, EventTriggerType.PointerDown, down);
        if (up != null) AddTrigger(trig, EventTriggerType.PointerUp, up);
        AddTrigger(trig, EventTriggerType.PointerExit, up);
    }

    void AddTrigger(EventTrigger trig, EventTriggerType type, Action act)
    {
        if (act == null) return;
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener((data) => act());
        trig.triggers.Add(entry);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        // Tap layer is active for menu AND cutscenes (tap to advance).
        menuTap.SetActive(gm != null && (GameManager.State == GameState.Menu || GameManager.State == GameState.Cutscene));
        // Skip button: cutscenes and on-foot sections can jump straight to the ride.
        if (skipBtn != null)
            skipBtn.SetActive(gm != null && (GameManager.State == GameState.Cutscene || GameManager.State == GameState.OnFoot));
        if (touchRoot != null) touchRoot.SetActive(GameManager.State == GameState.Playing);

        if (bannerTimer > 0f)
        {
            bannerTimer -= Time.unscaledDeltaTime;
            if (bannerTimer <= 0f) bannerRoot.SetActive(false);
        }
        if (labelPop > 0f)
        {
            labelPop = Mathf.Max(0f, labelPop - Time.unscaledDeltaTime * 2.5f);
            timelineLabel.transform.localScale = Vector3.one * (1f + labelPop * 0.35f);
        }
        float chargeFrac = (float)TimelineManager.Charges / TimelineManager.MaxCharges;
        var frt = cooldownFill.rectTransform;
        frt.sizeDelta = new Vector2(340f * Mathf.Clamp01(chargeFrac), 16f);
        cooldownLabel.text = TimelineManager.Charges > 0
            ? "PENDANT ×" + TimelineManager.Charges
            : "PENDANT DRAINED — RECHARGING";

        if (gm != null && gm.bike != null && GameManager.State == GameState.Playing)
        {
            speedLabel.text = Mathf.RoundToInt(gm.bike.SpeedKmh) + " km/h";
            string ctx = "";
            var ch = gm.chase;
            if (ch != null && ch.ChaseOn && ch.Heat > 0.55f) ctx = "COP ON YOUR TAIL!";
            else
            {
                float w = TimelineManager.WindX(TimelineManager.Active);
                if (w < 0f) ctx = "HEADWIND — pin the throttle!";
                else if (w > 0f) ctx = "TAILWIND — she pulls forward";
            }
            hintLabel.text = ctx;
        }
        else if (GameManager.State == GameState.OnFoot)
        {
            speedLabel.text = "";
            hintLabel.text = "A/D WALK · GO TO THE COUNTER →";
        }
        else if (GameManager.State == GameState.Cutscene)
        {
            speedLabel.text = "";
            hintLabel.text = "TAP TO CONTINUE · SKIP →";
        }
        else if (GameManager.State != GameState.Playing)
        {
            speedLabel.text = "";
            hintLabel.text = "";
        }
    }
}
