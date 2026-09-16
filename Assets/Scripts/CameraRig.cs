using UnityEngine;

// Side-view camera: follows with lookahead, punches the FOV wider during
// fast airtime (the signature-jump swoop), shakes on landing/crash.
public class CameraRig : MonoBehaviour
{
    public Transform target;

    Camera cam;
    BikeController bike;
    Vector3 smoothVel;
    float shake;
    const float BaseFov = 50f;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = gameObject.AddComponent<Camera>();
        cam.orthographic = false;
        cam.fieldOfView = BaseFov;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 400f;
    }

    public void SnapTo(Vector3 p)
    {
        transform.position = new Vector3(p.x + 2f, 3.4f, -15f);
        transform.rotation = Quaternion.identity;
        smoothVel = Vector3.zero;
    }

    // Snap the camera to on-foot Jack (inside the store). Call this when
    // entering OnFoot so the player is NEVER left looking at the street.
    public void SnapToOnFoot(Vector3 p)
    {
        transform.position = new Vector3(p.x + 1.5f, 2.6f, -8f);
        transform.rotation = Quaternion.identity;
        smoothVel = Vector3.zero;
        // Force-clear the bike ref so we use on-foot follow immediately.
        bike = null;
    }

    public void AddShake(float s)
    {
        shake = Mathf.Max(shake, s);
    }

    void LateUpdate()
    {
        // A cutscene owns the camera — yield to CameraDirector.
        if (CutsceneManager.IsPlaying) return;
        if (target == null) return;
        // Target may change (bike <-> on-foot Jack); re-resolve the bike ref.
        var bc = target.GetComponent<BikeController>();
        if (bc != bike) bike = bc;

        Vector3 tp = target.position;
        if (bike != null)
        {
            float look = Mathf.Clamp(bike.vel.x * 0.22f, 0f, 5f);
            Vector3 want = new Vector3(tp.x + 2.5f + look, Mathf.Max(3.2f, tp.y * 0.5f + 1.8f), -15f);
            transform.position = Vector3.SmoothDamp(transform.position, want, ref smoothVel, 0.22f);
        }
        else
        {
            // On-foot: tight side view, close enough to see Jack clearly.
            // Snap faster (less smoothing) so the player is never lost.
            Vector3 want = new Vector3(tp.x + 1.5f, 2.6f, -8f);
            transform.position = Vector3.SmoothDamp(transform.position, want, ref smoothVel, 0.12f);
        }
        transform.rotation = Quaternion.identity;

        // FOV punch on fast airtime.
        float targetFov = BaseFov;
        if (bike != null && !bike.grounded && bike.vel.magnitude > 14f) targetFov = BaseFov + 11f;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 1f - Mathf.Exp(-3f * Time.deltaTime));

        if (shake > 0f)
        {
            transform.position += new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * shake * 0.35f;
            shake = Mathf.Max(0f, shake - Time.deltaTime * 2.5f);
        }
    }
}
