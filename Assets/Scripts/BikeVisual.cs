using UnityEngine;

// Procedural 2.5D bike + rider built from primitives: spinning wheels,
// leaning rider, suspension dip on landing. Chunky stylized look.
public class BikeVisual : MonoBehaviour
{
    Transform wheelF, wheelR, riderRoot, bikeRoot;
    float wheelFBaseY, wheelRBaseY;
    float suspension; // 0..1 compression spring

    static Material tireMat, frameMat, redMat, chromeMat, jacketMat, denimMat, helmetMat, skinMat;

    static void EnsureMats()
    {
        if (tireMat != null) return;
        tireMat = WorldBuilder.MakeMat(new Color(0.10f, 0.10f, 0.12f));
        frameMat = WorldBuilder.MakeMat(new Color(0.16f, 0.16f, 0.18f));
        redMat = WorldBuilder.MakeMat(new Color(0.80f, 0.16f, 0.14f));
        chromeMat = WorldBuilder.MakeMat(new Color(0.75f, 0.78f, 0.82f));
        jacketMat = WorldBuilder.MakeMat(new Color(0.72f, 0.22f, 0.14f));
        denimMat = WorldBuilder.MakeMat(new Color(0.20f, 0.30f, 0.58f));
        helmetMat = WorldBuilder.MakeMat(new Color(0.92f, 0.92f, 0.95f));
        skinMat = WorldBuilder.MakeMat(new Color(0.95f, 0.76f, 0.60f));
    }

    void Awake()
    {
        EnsureMats();
        Build();
    }

    static GameObject Part(PrimitiveType type, string name, Vector3 localPos, Vector3 localScale, Material mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        foreach (var c in go.GetComponents<Collider>()) Object.Destroy(c);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        go.GetComponent<Renderer>().material = mat;
        return go;
    }

    // Cylinder stretched between two local points.
    static GameObject Limb(Vector3 a, Vector3 b, float r, Material mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Limb";
        foreach (var c in go.GetComponents<Collider>()) Object.Destroy(c);
        go.transform.SetParent(parent, false);
        Vector3 d = b - a;
        go.transform.localPosition = (a + b) / 2f;
        go.transform.localScale = new Vector3(r * 2f, d.magnitude / 2f, r * 2f);
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
        go.GetComponent<Renderer>().material = mat;
        return go;
    }

    void Build()
    {
        bikeRoot = new GameObject("BikeRoot").transform;
        bikeRoot.SetParent(transform, false);

        // Wheels (cylinder axis -> z).
        wheelR = Part(PrimitiveType.Cylinder, "WheelR", new Vector3(-0.72f, -0.20f, 0f), new Vector3(0.7f, 0.22f, 0.7f), tireMat, bikeRoot).transform;
        wheelR.localRotation = Quaternion.Euler(90f, 0f, 0f);
        wheelF = Part(PrimitiveType.Cylinder, "WheelF", new Vector3(0.72f, -0.20f, 0f), new Vector3(0.7f, 0.22f, 0.7f), tireMat, bikeRoot).transform;
        wheelF.localRotation = Quaternion.Euler(90f, 0f, 0f);
        // Hubs.
        var hubR = Part(PrimitiveType.Cylinder, "HubR", new Vector3(-0.72f, -0.20f, 0f), new Vector3(0.3f, 0.24f, 0.3f), chromeMat, bikeRoot).transform;
        hubR.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var hubF = Part(PrimitiveType.Cylinder, "HubF", new Vector3(0.72f, -0.20f, 0f), new Vector3(0.3f, 0.24f, 0.3f), chromeMat, bikeRoot).transform;
        hubF.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // Frame, tank, seat.
        Part(PrimitiveType.Cube, "Frame", new Vector3(0f, 0.05f, 0f), new Vector3(1.3f, 0.16f, 0.18f), frameMat, bikeRoot);
        Part(PrimitiveType.Cube, "Tank", new Vector3(0.25f, 0.24f, 0f), new Vector3(0.42f, 0.24f, 0.26f), redMat, bikeRoot);
        Part(PrimitiveType.Cube, "Seat", new Vector3(-0.35f, 0.26f, 0f), new Vector3(0.52f, 0.12f, 0.24f), frameMat, bikeRoot);
        // Front fork + handlebar.
        var fork = Part(PrimitiveType.Cube, "Fork", new Vector3(0.62f, 0.15f, 0f), new Vector3(0.09f, 0.85f, 0.09f), chromeMat, bikeRoot);
        fork.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
        var bar = Part(PrimitiveType.Cylinder, "Bar", new Vector3(0.80f, 0.52f, 0f), new Vector3(0.5f, 0.08f, 0.08f), frameMat, bikeRoot);
        bar.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        // Exhaust.
        var ex = Part(PrimitiveType.Cylinder, "Exhaust", new Vector3(-0.15f, -0.12f, 0.20f), new Vector3(0.14f, 0.9f, 0.14f), chromeMat, bikeRoot);
        ex.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        // Headlight.
        Part(PrimitiveType.Sphere, "Headlight", new Vector3(0.86f, 0.38f, 0f), new Vector3(0.16f, 0.16f, 0.16f), WorldBuilder.MakeGlow(new Color(1f, 0.95f, 0.7f), 1.5f), bikeRoot);

        // Rider.
        riderRoot = new GameObject("Rider").transform;
        riderRoot.SetParent(bikeRoot, false);
        riderRoot.localPosition = new Vector3(0f, 0.30f, 0f);
        Part(PrimitiveType.Cube, "Hips", new Vector3(-0.05f, 0.55f, 0f), new Vector3(0.34f, 0.22f, 0.26f), denimMat, riderRoot);
        var torso = Part(PrimitiveType.Capsule, "Torso", new Vector3(0.16f, 0.88f, 0f), new Vector3(0.34f, 0.62f, 0.30f), jacketMat, riderRoot);
        torso.transform.localRotation = Quaternion.Euler(0f, 0f, -38f);
        Part(PrimitiveType.Sphere, "Head", new Vector3(0.50f, 1.22f, 0f), new Vector3(0.30f, 0.34f, 0.30f), skinMat, riderRoot);
        Part(PrimitiveType.Sphere, "Helmet", new Vector3(0.50f, 1.26f, 0f), new Vector3(0.36f, 0.34f, 0.36f), helmetMat, riderRoot);
        // Arms to the handlebar.
        Limb(new Vector3(0.28f, 1.02f, 0.10f), new Vector3(0.78f, 0.54f, 0.10f), 0.07f, jacketMat, riderRoot);
        Limb(new Vector3(0.28f, 1.02f, -0.10f), new Vector3(0.78f, 0.54f, -0.10f), 0.07f, jacketMat, riderRoot);
        // Legs to the pegs.
        Limb(new Vector3(-0.05f, 0.50f, 0.12f), new Vector3(0.28f, 0.22f, 0.12f), 0.09f, denimMat, riderRoot);
        Limb(new Vector3(0.28f, 0.22f, 0.12f), new Vector3(0.05f, 0.02f, 0.12f), 0.07f, denimMat, riderRoot);
        Limb(new Vector3(-0.05f, 0.50f, -0.12f), new Vector3(0.28f, 0.22f, -0.12f), 0.09f, denimMat, riderRoot);
        Limb(new Vector3(0.28f, 0.22f, -0.12f), new Vector3(0.05f, 0.02f, -0.12f), 0.07f, denimMat, riderRoot);

        wheelFBaseY = wheelF.localPosition.y;
        wheelRBaseY = wheelR.localPosition.y;
    }

    public void KickSuspension(float fallSpeed)
    {
        suspension = Mathf.Min(0.30f, fallSpeed * 0.012f);
    }

    // Called every frame by the controller.
    public void SetFrame(float lean, bool wheelie, bool grounded, float speed, float dt)
    {
        // Wheel spin (about the axle = local Y after the 90° X rotation).
        float spin = -speed / 0.35f * dt * Mathf.Rad2Deg;
        wheelF.Rotate(0f, spin, 0f, Space.Self);
        wheelR.Rotate(0f, spin, 0f, Space.Self);

        // Suspension spring.
        suspension = Mathf.Lerp(suspension, 0f, 1f - Mathf.Exp(-10f * dt));
        bikeRoot.localPosition = new Vector3(0f, -suspension, 0f);

        // Rider shifts with lean / wheelie.
        float targetX = lean * 0.14f - (wheelie ? 0.18f : 0f);
        Vector3 rp = riderRoot.localPosition;
        rp.x = Mathf.Lerp(rp.x, targetX, 1f - Mathf.Exp(-8f * dt));
        riderRoot.localPosition = rp;
    }
}
