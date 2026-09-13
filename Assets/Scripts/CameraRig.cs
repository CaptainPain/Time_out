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

    public void AddShake(float s)
    {
        shake = Mathf.Max(shake, s);
    }

    void LateUpdate()
    {
        if (target == null) return;
        if (bike == null) bike = target.GetComponent<BikeController>();

        Vector3 tp = target.position;
        float look = bike != null ? Mathf.Clamp(bike.vel.x * 0.22f, 0f, 5f) : 0f;
        Vector3 want = new Vector3(tp.x + 2.5f + look, Mathf.Max(3.2f, tp.y * 0.5f + 1.8f), -15f);
        transform.position = Vector3.SmoothDamp(transform.position, want, ref smoothVel, 0.22f);
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
