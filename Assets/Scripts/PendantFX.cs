using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// The pendant firing: fullscreen flash, expanding shockwave ring at the
// bike, and a brief hitstop so the timeline swap reads as an event.
public class PendantFX : MonoBehaviour
{
    Image flash;
    float flashT;
    Color flashColor = new Color(0.65f, 0.92f, 1f, 0f);

    LineRenderer ring;
    Material ringMat;
    float ringT;
    Color ringColor = new Color(0.4f, 0.9f, 1f, 1f);

    void Awake()
    {
        var canvasGo = new GameObject("PendantFXCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var flashGo = new GameObject("Flash");
        flashGo.transform.SetParent(canvas.transform, false);
        flash = flashGo.AddComponent<Image>();
        flash.color = flashColor;
        flash.raycastTarget = false;
        var rt = flash.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var ringGo = new GameObject("ShiftRing");
        ring = ringGo.AddComponent<LineRenderer>();
        ring.useWorldSpace = true;
        ring.loop = true;
        ring.positionCount = 48;
        ring.startWidth = 0.35f;
        ring.endWidth = 0.35f;
        ringMat = WorldBuilder.MakeMat(Color.white);
        ring.material = ringMat;
        ring.enabled = false;

        TimelineManager.OnShift += OnShift;
    }

    void OnDestroy()
    {
        TimelineManager.OnShift -= OnShift;
    }

    void OnShift(Timeline t)
    {
        if (GameManager.Instance == null || GameManager.Instance.bike == null) return;
        flashColor = new Color(0.65f, 0.92f, 1f, 0f);
        ringColor = new Color(0.4f, 0.9f, 1f, 1f);
        Fire(GameManager.Instance.bike.transform.position + Vector3.up * 0.8f);
        StartCoroutine(Hitstop());
    }

    public void CrashBurst(Vector3 pos)
    {
        flashColor = new Color(1f, 0.45f, 0.2f, 0f);
        ringColor = new Color(1f, 0.5f, 0.2f, 1f);
        Fire(pos + Vector3.up * 0.6f);
    }

    public void RingBurst(Vector3 pos)
    {
        flashColor = new Color(1f, 0.85f, 0.3f, 0f);
        ringColor = new Color(1f, 0.85f, 0.3f, 1f);
        Fire(pos + Vector3.up * 0.6f);
    }

    void Fire(Vector3 center)
    {
        flashT = 1f;
        ringT = 1f;
        ring.enabled = true;
        for (int i = 0; i < 48; i++)
        {
            float a = i / 48f * Mathf.PI * 2f;
            ring.SetPosition(i, center + new Vector3(Mathf.Cos(a) * 0.5f, Mathf.Sin(a) * 0.5f, 0f));
        }
        if (ringMat != null) ringMat.color = ringColor;
    }

    IEnumerator Hitstop()
    {
        Time.timeScale = 0.12f;
        yield return new WaitForSecondsRealtime(0.14f);
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (flashT > 0f)
        {
            flashT = Mathf.Max(0f, flashT - Time.unscaledDeltaTime * 3.5f);
            flash.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashT * 0.85f);
        }
        if (ringT > 0f && ring.enabled)
        {
            ringT = Mathf.Max(0f, ringT - Time.unscaledDeltaTime * 1.8f);
            float r = Mathf.Lerp(9f, 0.5f, ringT);
            // Rebuild ring around its current center.
            Vector3 center = Vector3.zero;
            for (int i = 0; i < 48; i++) center += ring.GetPosition(i);
            center /= 48f;
            for (int i = 0; i < 48; i++)
            {
                float a = i / 48f * Mathf.PI * 2f;
                ring.SetPosition(i, center + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
            }
            if (ringMat != null) ringMat.color = new Color(ringColor.r, ringColor.g, ringColor.b, ringT);
            if (ringT <= 0f) ring.enabled = false;
        }
    }
}
