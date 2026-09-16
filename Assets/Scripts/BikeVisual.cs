using UnityEngine;

// Procedural 2.5D offroad motorcycle + rider built from primitives.
// Proper dirt bike: tall stance, telescopic front fork with visible springs,
// rear swingarm with chain drive and sprockets, high exhaust, and a rider
// in full attack position — standing on the foot pegs, knees bent, elbows up.
// The spring/damper simulation lives in BikeController; this class poses the rig.
public class BikeVisual : MonoBehaviour
{
    Transform wheelF, wheelR, riderRoot, bikeRoot;
    float wheelFBaseY, wheelRBaseY;
    Vector3 riderBase;

    const float Travel = 0.30f; // max suspension travel in world units

    static Material tireMat, frameMat, redMat, chromeMat, jacketMat, denimMat, helmetMat, skinMat, plateMat, darkMat, chainMat;

    static void EnsureMats()
    {
        if (tireMat != null) return;
        tireMat = WorldBuilder.MakeMat(new Color(0.10f, 0.10f, 0.12f));
        frameMat = WorldBuilder.MakeMat(new Color(0.16f, 0.16f, 0.18f));
        darkMat = WorldBuilder.MakeMat(new Color(0.22f, 0.22f, 0.25f));
        chainMat = WorldBuilder.MakeMat(new Color(0.35f, 0.35f, 0.38f));
        redMat = WorldBuilder.MakeMat(new Color(0.80f, 0.16f, 0.14f));
        chromeMat = WorldBuilder.MakeMat(new Color(0.75f, 0.78f, 0.82f));
        jacketMat = WorldBuilder.MakeMat(new Color(0.72f, 0.22f, 0.14f));
        denimMat = WorldBuilder.MakeMat(new Color(0.20f, 0.30f, 0.58f));
        helmetMat = WorldBuilder.MakeMat(new Color(0.92f, 0.92f, 0.95f));
        skinMat = WorldBuilder.MakeMat(new Color(0.95f, 0.76f, 0.60f));
        plateMat = WorldBuilder.MakeMat(new Color(0.94f, 0.94f, 0.92f));
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

    // A wheel is an unscaled group (so children keep their own proportions):
    // tire, hub, and knobby tread blocks that spin with it.
    // Hub and knobs are NARROWER than the tire or each wheel reads as multiple tires.
    Transform BuildWheel(string name, Vector3 pos)
    {
        var g = new GameObject(name).transform;
        g.SetParent(bikeRoot, false);
        g.localPosition = pos;
        var tire = Part(PrimitiveType.Cylinder, name + "Tire", Vector3.zero, new Vector3(0.7f, 0.22f, 0.7f), tireMat, g);
        tire.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var hub = Part(PrimitiveType.Cylinder, name + "Hub", Vector3.zero, new Vector3(0.3f, 0.18f, 0.3f), chromeMat, g);
        hub.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        for (int i = 0; i < 10; i++)
        {
            float a = i / 10f * Mathf.PI * 2f;
            var knob = Part(PrimitiveType.Cube, name + "Knob" + i,
                new Vector3(Mathf.Cos(a) * 0.36f, Mathf.Sin(a) * 0.36f, 0f),
                new Vector3(0.10f, 0.10f, 0.18f), tireMat, g);
            knob.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
        }
        return g;
    }

    // A coil spring: stacked thin discs around a center point, along an axis.
    void BuildSpring(Vector3 center, Vector3 axis, float coils, float coilR, float length, Material mat)
    {
        Vector3 n = axis.normalized;
        for (int i = 0; i < coils; i++)
        {
            float t = (i / (coils - 1) - 0.5f) * length;
            var disc = Part(PrimitiveType.Cylinder, "Coil" + i, center + n * t,
                new Vector3(coilR * 2f, 0.025f, coilR * 2f), mat, bikeRoot);
            disc.transform.localRotation = Quaternion.FromToRotation(Vector3.up, n);
        }
    }

    void Build()
    {
        bikeRoot = new GameObject("BikeRoot").transform;
        bikeRoot.SetParent(transform, false);

        // Tall stance: wheels low, frame high. Real ground clearance.
        wheelR = BuildWheel("WheelR", new Vector3(-0.72f, -0.38f, 0f));
        wheelF = BuildWheel("WheelF", new Vector3(0.72f, -0.38f, 0f));

        // Frame + engine.
        Part(PrimitiveType.Cube, "Frame", new Vector3(0f, 0.18f, 0f), new Vector3(1.15f, 0.14f, 0.16f), frameMat, bikeRoot);
        Part(PrimitiveType.Cube, "Engine", new Vector3(0.08f, 0.02f, 0f), new Vector3(0.42f, 0.30f, 0.26f), darkMat, bikeRoot);
        // Skid plate under the engine.
        var skid = Part(PrimitiveType.Cube, "Skid", new Vector3(0.05f, -0.16f, 0f), new Vector3(0.65f, 0.05f, 0.24f), chromeMat, bikeRoot);
        skid.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
        // Tank + long flat seat.
        Part(PrimitiveType.Cube, "Tank", new Vector3(0.30f, 0.34f, 0f), new Vector3(0.40f, 0.22f, 0.24f), redMat, bikeRoot);
        Part(PrimitiveType.Cube, "Seat", new Vector3(-0.32f, 0.36f, 0f), new Vector3(0.58f, 0.10f, 0.22f), frameMat, bikeRoot);

        // Foot pegs: rider stands here, not on the seat.
        Part(PrimitiveType.Cube, "PegL", new Vector3(0.02f, -0.06f, 0.20f), new Vector3(0.18f, 0.04f, 0.08f), darkMat, bikeRoot);
        Part(PrimitiveType.Cube, "PegR", new Vector3(0.02f, -0.06f, -0.20f), new Vector3(0.18f, 0.04f, 0.08f), darkMat, bikeRoot);

        // ---- Front fork: telescopic with visible springs ----
        // Raked ~24°: axle forward of the triple clamp.
        Vector3 axleF = new Vector3(0.72f, -0.38f, 0f);
        Vector3 clamp = new Vector3(0.42f, 0.48f, 0f);
        // Two chrome stanchions (upper tubes).
        Limb(new Vector3(0.48f, 0.38f, 0.07f), new Vector3(0.40f, 0.52f, 0.07f), 0.035f, chromeMat, bikeRoot).name = "StanchionL";
        Limb(new Vector3(0.48f, 0.38f, -0.07f), new Vector3(0.40f, 0.52f, -0.07f), 0.035f, chromeMat, bikeRoot).name = "StanchionR";
        // Two lower sliders (thicker, darker).
        Limb(new Vector3(0.70f, -0.30f, 0.07f), new Vector3(0.50f, 0.30f, 0.07f), 0.050f, darkMat, bikeRoot).name = "SliderL";
        Limb(new Vector3(0.70f, -0.30f, -0.07f), new Vector3(0.50f, 0.30f, -0.07f), 0.050f, darkMat, bikeRoot).name = "SliderR";
        // Coil springs around the stanchions.
        Vector3 forkAxis = (clamp - axleF).normalized;
        BuildSpring(new Vector3(0.44f, 0.45f, 0.07f), forkAxis, 6, 0.055f, 0.22f, chromeMat);
        BuildSpring(new Vector3(0.44f, 0.45f, -0.07f), forkAxis, 6, 0.055f, 0.22f, chromeMat);
        // Triple clamp.
        Part(PrimitiveType.Cube, "TripleClamp", new Vector3(0.42f, 0.50f, 0f), new Vector3(0.14f, 0.08f, 0.22f), darkMat, bikeRoot);
        // Fork guards (plastic, red) on the sliders.
        Part(PrimitiveType.Cube, "GuardL", new Vector3(0.60f, -0.05f, 0.09f), new Vector3(0.10f, 0.45f, 0.04f), redMat, bikeRoot);
        Part(PrimitiveType.Cube, "GuardR", new Vector3(0.60f, -0.05f, -0.09f), new Vector3(0.10f, 0.45f, 0.04f), redMat, bikeRoot);
        // Handlebar.
        var bar = Part(PrimitiveType.Cylinder, "Bar", new Vector3(0.52f, 0.62f, 0f), new Vector3(0.42f, 0.06f, 0.06f), frameMat, bikeRoot);
        bar.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        // Number plate.
        Part(PrimitiveType.Cube, "Plate", new Vector3(0.58f, 0.42f, 0f), new Vector3(0.04f, 0.28f, 0.32f), plateMat, bikeRoot);
        // High front fender with real clearance above the tire.
        var ff = Part(PrimitiveType.Cube, "FenderF", new Vector3(0.72f, 0.08f, 0f), new Vector3(0.55f, 0.06f, 0.28f), redMat, bikeRoot);
        ff.transform.localRotation = Quaternion.Euler(0f, 0f, 4f);
        // Headlight.
        Part(PrimitiveType.Sphere, "Headlight", new Vector3(0.62f, 0.52f, 0f), new Vector3(0.14f, 0.14f, 0.14f), WorldBuilder.MakeGlow(new Color(1f, 0.95f, 0.7f), 1.5f), bikeRoot);

        // ---- Rear: swingarm, chain drive, sprockets ----
        // Swingarm: pivot near engine, back to rear axle, both sides.
        Limb(new Vector3(0.05f, -0.02f, 0.11f), new Vector3(-0.72f, -0.38f, 0.11f), 0.045f, frameMat, bikeRoot).name = "SwingarmL";
        Limb(new Vector3(0.05f, -0.02f, -0.11f), new Vector3(-0.72f, -0.38f, -0.11f), 0.045f, frameMat, bikeRoot).name = "SwingarmR";
        // Rear sprocket (the gear): big disc on the left side of the rear wheel.
        var sprocketR = Part(PrimitiveType.Cylinder, "SprocketR", new Vector3(-0.72f, -0.38f, 0.15f), new Vector3(0.40f, 0.035f, 0.40f), darkMat, bikeRoot);
        sprocketR.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        // Front sprocket (countershaft): small disc near the engine.
        var sprocketF = Part(PrimitiveType.Cylinder, "SprocketF", new Vector3(0.10f, -0.06f, 0.15f), new Vector3(0.16f, 0.035f, 0.16f), darkMat, bikeRoot);
        sprocketF.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        // Chain: top run and bottom run between the sprockets.
        Limb(new Vector3(0.10f, 0.02f, 0.15f), new Vector3(-0.72f, -0.18f, 0.15f), 0.022f, chainMat, bikeRoot).name = "ChainTop";
        Limb(new Vector3(0.10f, -0.14f, 0.15f), new Vector3(-0.72f, -0.58f, 0.15f), 0.022f, chainMat, bikeRoot).name = "ChainBottom";
        // Rear fender, upswept.
        var rf = Part(PrimitiveType.Cube, "FenderR", new Vector3(-0.95f, 0.52f, 0f), new Vector3(0.50f, 0.06f, 0.26f), redMat, bikeRoot);
        rf.transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
        // High exhaust: header from engine, muffler kicked up at the rear.
        Limb(new Vector3(0.28f, 0.05f, -0.10f), new Vector3(-0.20f, 0.10f, -0.16f), 0.055f, chromeMat, bikeRoot).name = "HeaderPipe";
        var muffler = Part(PrimitiveType.Cylinder, "Muffler", new Vector3(-0.52f, 0.28f, -0.16f), new Vector3(0.16f, 0.55f, 0.16f), chromeMat, bikeRoot);
        muffler.transform.localRotation = Quaternion.Euler(0f, 0f, 72f);

        // ---- Rider: full attack position, standing on the pegs ----
        riderRoot = new GameObject("Rider").transform;
        riderRoot.SetParent(bikeRoot, false);
        riderRoot.localPosition = Vector3.zero;

        // Feet planted on the pegs.
        Vector3 footL = new Vector3(0.02f, -0.03f, 0.20f);
        Vector3 footR = new Vector3(0.02f, -0.03f, -0.20f);
        // Knees bent: up and slightly forward from the feet.
        Vector3 kneeL = new Vector3(0.18f, 0.22f, 0.17f);
        Vector3 kneeR = new Vector3(0.18f, 0.22f, -0.17f);
        // Hips: back, crouched low over the seat (not sitting on it).
        Vector3 hips = new Vector3(-0.12f, 0.52f, 0f);
        // Shoulders: forward, over the bars.
        Vector3 shoulderC = new Vector3(0.24f, 0.82f, 0f);
        // Elbows: UP and out (attack position).
        Vector3 elbowL = new Vector3(0.34f, 0.98f, 0.22f);
        Vector3 elbowR = new Vector3(0.34f, 0.98f, -0.22f);
        // Hands: on the grips.
        Vector3 handL = new Vector3(0.52f, 0.62f, 0.16f);
        Vector3 handR = new Vector3(0.52f, 0.62f, -0.16f);
        // Head: up, looking ahead.
        Vector3 head = new Vector3(0.34f, 1.04f, 0f);

        // Legs: foot -> knee -> hip (thigh + shin).
        Limb(footL, kneeL, 0.085f, denimMat, riderRoot);
        Limb(kneeL, new Vector3(hips.x, hips.y, 0.10f), 0.095f, denimMat, riderRoot);
        Limb(footR, kneeR, 0.085f, denimMat, riderRoot);
        Limb(kneeR, new Vector3(hips.x, hips.y, -0.10f), 0.095f, denimMat, riderRoot);
        // Hips block.
        Part(PrimitiveType.Cube, "Hips", hips, new Vector3(0.30f, 0.20f, 0.24f), denimMat, riderRoot);
        // Torso: leaning forward, chest over the tank.
        Limb(hips, shoulderC, 0.16f, jacketMat, riderRoot).name = "Torso";
        // Arms: shoulder -> elbow (up) -> hand (down to bar).
        Limb(new Vector3(shoulderC.x, shoulderC.y, 0.10f), elbowL, 0.065f, jacketMat, riderRoot);
        Limb(elbowL, handL, 0.055f, jacketMat, riderRoot);
        Limb(new Vector3(shoulderC.x, shoulderC.y, -0.10f), elbowR, 0.065f, jacketMat, riderRoot);
        Limb(elbowR, handR, 0.055f, jacketMat, riderRoot);
        // Head + helmet.
        Part(PrimitiveType.Sphere, "Head", head, new Vector3(0.26f, 0.30f, 0.26f), skinMat, riderRoot);
        Part(PrimitiveType.Sphere, "Helmet", head + new Vector3(0f, 0.04f, 0f), new Vector3(0.32f, 0.30f, 0.32f), helmetMat, riderRoot);

        wheelFBaseY = wheelF.localPosition.y;
        wheelRBaseY = wheelR.localPosition.y;
        riderBase = riderRoot.localPosition;
    }

    // Suspension pose from the controller's spring/damper sim (0..1 each).
    // Wheels travel up toward the chassis; the chassis stays level and the
    // rider soaks up a little of it.
    public void SetSuspension(float f, float r)
    {
        wheelF.localPosition = new Vector3(wheelF.localPosition.x, wheelFBaseY + f * Travel, wheelF.localPosition.z);
        wheelR.localPosition = new Vector3(wheelR.localPosition.x, wheelRBaseY + r * Travel, wheelR.localPosition.z);
        Vector3 rp = riderRoot.localPosition;
        rp.y = riderBase.y - (f + r) * 0.5f * Travel * 0.45f;
        riderRoot.localPosition = rp;
    }

    // Called every frame by the controller.
    // lean: -1..1 air/steer lean. foreAft: +1 hard on the gas, -1 braking.
    public void SetFrame(float lean, bool wheelie, bool grounded, float speed, float dt, float foreAft)
    {
        // Knobby wheel spin about the axle (local Z).
        float spin = -speed / 0.38f * dt * Mathf.Rad2Deg;
        wheelF.Rotate(0f, 0f, spin, Space.Self);
        wheelR.Rotate(0f, 0f, spin, Space.Self);

        // Rider works the bike: back on the gas, forward under braking,
        // tucked and shifting with lean and wheelies.
        float targetX = riderBase.x + lean * 0.14f - (wheelie ? 0.18f : 0f) - foreAft * 0.12f;
        float targetRot = foreAft * 7f + (wheelie ? -10f : 0f);
        Vector3 rp = riderRoot.localPosition;
        rp.x = Mathf.Lerp(rp.x, targetX, 1f - Mathf.Exp(-8f * dt));
        riderRoot.localPosition = rp;
        float cr = riderRoot.localRotation.eulerAngles.z;
        float nr = Mathf.LerpAngle(cr > 180f ? cr - 360f : cr, targetRot, 1f - Mathf.Exp(-6f * dt));
        riderRoot.localRotation = Quaternion.Euler(0f, 0f, nr);
    }
}
