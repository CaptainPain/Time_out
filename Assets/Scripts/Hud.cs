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
    Text coinLabel;
    Text hintLabel;
    Text bannerLabel;
    Text cooldownLabel;
    Image cooldownFill;
    float bannerTimer;
    float labelPop;
    GameObject menuTap;
    GameObject touchRoot;

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

        timelineLabel = AddLabel(root, "", 46, Color.white, 0.5f, 1f, new Vector2(0f, -24f));
        cooldownLabel = AddLabel(root, "PENDANT", 22, new Color(1f, 1f, 1f, 0.85f), 0.5f, 1f, new Vector2(0f, -78f));
        var barBg = AddBar(root, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(340f, 16f), new Color(0f, 0f, 0f, 0.45f));
        cooldownFill = AddBar(barBg.transform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(340f, 16f), new Color(0.4f, 0.9f, 1f, 0.95f));
        var fillRt = cooldownFill.rectTransform;
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.anchoredPosition = Vector2.zero;

        speedLabel = AddLabel(root, "", 36, Color.white, 0f, 0f, new Vector2(36f, 30f));
        speedLabel.alignment = TextAnchor.LowerLeft;
        coinLabel = AddLabel(root, "", 36, Color.white, 1f, 1f, new Vector2(-36f, -24f));
        coinLabel.alignment = TextAnchor.UpperRight;

        hintLabel = AddLabel(root, "", 22, new Color(1f, 1f, 1f, 0.75f), 0.5f, 0f, new Vector2(0f, 24f));
        bannerLabel = AddLabel(root, "", 84, Color.white, 0.5f, 0.5f, Vector2.zero);
        bannerLabel.gameObject.SetActive(false);

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
        tapBtn.onClick.AddListener(() => { if (GameManager.State == GameState.Menu) GameManager.Instance.AdvanceMenu(); });

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
            bannerLabel.gameObject.SetActive(false);
            bannerTimer = 0f;
            return;
        }
        bannerLabel.text = text;
        bannerLabel.gameObject.SetActive(true);
        bannerTimer = dur;
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
        menuTap.SetActive(gm != null && GameManager.State == GameState.Menu);
        if (touchRoot != null) touchRoot.SetActive(GameManager.State == GameState.Playing);

        if (bannerTimer > 0f)
        {
            bannerTimer -= Time.unscaledDeltaTime;
            if (bannerTimer <= 0f) bannerLabel.gameObject.SetActive(false);
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
            : "PENDANT DRAINED — thread a ring";

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
        else if (GameManager.State != GameState.Playing)
        {
            speedLabel.text = "";
            hintLabel.text = "";
        }
        coinLabel.text = "COINS " + GameData.coinsGot + "/" + GameData.coins.Count;
    }
}
