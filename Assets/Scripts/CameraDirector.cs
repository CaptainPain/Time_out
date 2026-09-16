using UnityEngine;

// Cinematic camera. While CutsceneManager.IsPlaying is true this drives the
// camera through framed shots; otherwise it stays quiet and CameraRig
// follows the bike (CameraRig.LateUpdate yields on the same flag).
// Attach to the Main Camera alongside CameraRig.
public class CameraDirector : MonoBehaviour
{
    public static CameraDirector Instance;

    Camera cam;
    bool active;
    Vector3 fromPos, toPos;
    Quaternion fromRot, toRot;
    float fromFov, toFov;
    float moveTime, t;

    const float DefaultFov = 50f;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        if (cam == null) cam = gameObject.AddComponent<Camera>();
    }

    // Smooth move to a framed shot: position, look-target, FOV, travel time.
    // Holds the shot once it arrives until the next Shot() call.
    public void Shot(Vector3 pos, Vector3 lookAt, float fov, float moveTime)
    {
        fromPos = transform.position;
        fromRot = transform.rotation;
        fromFov = cam != null ? cam.fieldOfView : DefaultFov;
        toPos = pos;
        Vector3 d = lookAt - pos;
        toRot = d.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(d) : Quaternion.identity;
        toFov = fov;
        this.moveTime = Mathf.Max(0.01f, moveTime);
        t = 0f;
        active = true;
    }

    // Return control to CameraRig follow mode. CameraRig SmoothDamps back to
    // the bike from the current position, so the handoff glides instead of
    // snapping.
    public void EndCinematic()
    {
        active = false;
    }

    void LateUpdate()
    {
        if (!active || !CutsceneManager.IsPlaying) return;
        t += Time.deltaTime / moveTime;
        float k = Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t));
        transform.position = Vector3.Lerp(fromPos, toPos, k);
        transform.rotation = Quaternion.Slerp(fromRot, toRot, k);
        if (cam != null) cam.fieldOfView = Mathf.Lerp(fromFov, toFov, k);
    }
}
