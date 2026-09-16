using UnityEngine;
using UnityEngine.UI;

// Safe font + label helpers. Never throws: if no font is bundled, labels are
// created with a null font instead of crashing (Unity 6's built-in Arial
// lookup can throw, which is what killed the gate-runner menu on day one).
public static class UIUtil
{
    static Font cachedFont;
    static bool fontLookedUp;

    public static Font GetFont()
    {
        // If the cached font was destroyed (e.g. Stop→Play without domain
        // reload), the static still holds a dead reference — look it up again.
        if (fontLookedUp && cachedFont != null) return cachedFont;
        fontLookedUp = true;
        try { cachedFont = Resources.Load<Font>("DejaVuSans"); } catch { cachedFont = null; }
        if (cachedFont == null)
        {
            try { cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { cachedFont = null; }
        }
        if (cachedFont == null)
        {
            // Last resort: build a dynamic font from an OS font. Works on
            // Windows/macOS where Arial ships with the OS.
            try { cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 16); } catch { cachedFont = null; }
        }
        return cachedFont;
    }

    // Screen-space UGUI label.
    public static Text MakeLabel(string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        var go = new GameObject("Label");
        var t = go.AddComponent<Text>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = anchor;
        var f = GetFont();
        if (f != null) t.font = f;
        t.raycastTarget = false;
        return t;
    }

    // World-space label that faces the side camera (camera sits at -z).
    // maxWidth > 0: shrink characterSize so the text fits inside it.
    // Estimated width = chars x characterSize x fontSize x 0.55.
    public static TextMesh MakeWorldLabel(string text, float charSize, Color color, float maxWidth = 0f)
    {
        var go = new GameObject("WorldLabel");
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 64;
        tm.characterSize = charSize;
        if (maxWidth > 0f)
        {
            float est = text.Length * charSize * tm.fontSize * 0.55f;
            if (est > maxWidth) tm.characterSize = charSize * maxWidth / est;
        }
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        var f = GetFont();
        if (f != null) tm.font = f;
        go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        go.transform.localScale = new Vector3(-1f, 1f, 1f);
        return tm;
    }
}
