using UnityEngine;

// Ambient background traffic on the town road (z = +12, behind the play
// lane). Per timeline: boxy 1999 cars, medieval horse carts, future
// hover-cars. Vehicles loop at constant speed and wrap around. No collision
// with the player — pure set dressing, no colliders anywhere.
public class TrafficManager : MonoBehaviour
{
    static GameObject root;
    static Vehicle[] vehicles;
    static float time;

    const float RoadZ = 12f;
    const float MinX = -25f;
    const float MaxX = 55f; // flat ground ends at HillStart = 60

    class Vehicle
    {
        public GameObject go;
        public float x, z, speed, dir;
        public float baseY;
        public float phase;
        public bool hover;
    }

    // ---------------- build ----------------

    public static void Build(Transform parent)
    {
        if (root != null) Object.Destroy(root);
        root = new GameObject("Traffic");
        root.transform.SetParent(parent, false);
        // Background road strip so the cars aren't driving on grass.
        var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "TrafficRoad";
        foreach (var c in road.GetComponents<Collider>()) Object.Destroy(c);
        road.transform.SetParent(root.transform, false);
        road.transform.localPosition = new Vector3((MinX + MaxX) * 0.5f, -0.05f, RoadZ);
        road.transform.localScale = new Vector3(MaxX - MinX + 20f, 0.1f, 4f);
        road.GetComponent<Renderer>().material = WorldBuilder.MakeMat(new Color(0.25f, 0.25f, 0.28f));
        time = 0f;
        ApplyTimeline(TimelineManager.Active);
    }

    public static void ApplyTimeline(Timeline t)
    {
        if (root == null) return;
        // Clear the old fleet.
        for (int i = root.transform.childCount - 1; i >= 0; i--)
            Object.Destroy(root.transform.GetChild(i).gameObject);

        int count = t == Timeline.Present ? 4 : t == Timeline.Medieval ? 2 : 3;
        vehicles = new Vehicle[count];
        for (int i = 0; i < count; i++)
        {
            float x = MinX + (MaxX - MinX) * i / count;
            float z = RoadZ + (i % 2 == 0 ? 0.5f : -0.6f);
            float dir = i % 2 == 0 ? 1f : -1f;
            Vehicle v;
            if (t == Timeline.Present)
            {
                Color[] carColors = {
                    new Color(0.75f, 0.15f, 0.12f), new Color(0.12f, 0.25f, 0.65f),
                    new Color(0.85f, 0.70f, 0.15f), new Color(0.12f, 0.55f, 0.50f)
                };
                v = MakeCar(x, z, carColors[i % carColors.Length], 5f + i * 0.6f);
            }
            else if (t == Timeline.Medieval)
            {
                v = MakeCart(x, z, 2.5f + i * 0.5f);
            }
            else
            {
                v = MakeHover(x, z, 9f + i * 1.2f);
            }
            v.dir = dir;
            if (dir < 0f) v.go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            vehicles[i] = v;
        }
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

    static Vehicle MakeCar(float x, float z, Color color, float speed)
    {
        var v = new Vehicle { x = x, z = z, speed = speed, baseY = 0f, hover = false };
        var go = new GameObject("Car");
        go.transform.SetParent(root.transform, false);
        go.transform.position = new Vector3(x, 0f, z);
        v.go = go;
        var bodyMat = WorldBuilder.MakeMat(color);
        var dark = WorldBuilder.MakeMat(new Color(0.08f, 0.08f, 0.09f));
        Part(PrimitiveType.Cube, "Body", new Vector3(0f, 0.45f, 0f), new Vector3(1.9f, 0.5f, 0.85f), bodyMat, go.transform);
        Part(PrimitiveType.Cube, "Cabin", new Vector3(-0.15f, 0.85f, 0f), new Vector3(1.0f, 0.42f, 0.75f),
            WorldBuilder.MakeMat(new Color(0.15f, 0.18f, 0.22f)), go.transform);
        Vector3[] wp = {
            new Vector3(0.65f, 0.22f, 0.46f), new Vector3(0.65f, 0.22f, -0.46f),
            new Vector3(-0.65f, 0.22f, 0.46f), new Vector3(-0.65f, 0.22f, -0.46f)
        };
        for (int i = 0; i < 4; i++)
        {
            var w = Part(PrimitiveType.Cylinder, "Wheel" + i, wp[i], new Vector3(0.44f, 0.16f, 0.44f), dark, go.transform);
            w.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
        return v;
    }

    static Vehicle MakeCart(float x, float z, float speed)
    {
        var v = new Vehicle { x = x, z = z, speed = speed, baseY = 0f, hover = false };
        var go = new GameObject("Cart");
        go.transform.SetParent(root.transform, false);
        go.transform.position = new Vector3(x, 0f, z);
        v.go = go;
        var wood = WorldBuilder.MakeMat(new Color(0.45f, 0.30f, 0.18f));
        var darkWood = WorldBuilder.MakeMat(new Color(0.32f, 0.21f, 0.12f));
        // Cart bed + side rails.
        Part(PrimitiveType.Cube, "Bed", new Vector3(0f, 0.75f, 0f), new Vector3(1.7f, 0.18f, 0.95f), wood, go.transform);
        Part(PrimitiveType.Cube, "RailL", new Vector3(0f, 1.05f, 0.44f), new Vector3(1.7f, 0.30f, 0.08f), darkWood, go.transform);
        Part(PrimitiveType.Cube, "RailR", new Vector3(0f, 1.05f, -0.44f), new Vector3(1.7f, 0.30f, 0.08f), darkWood, go.transform);
        // Four wooden disc wheels.
        Vector3[] wp = {
            new Vector3(0.55f, 0.40f, 0.55f), new Vector3(0.55f, 0.40f, -0.55f),
            new Vector3(-0.55f, 0.40f, 0.55f), new Vector3(-0.55f, 0.40f, -0.55f)
        };
        for (int i = 0; i < 4; i++)
        {
            var w = Part(PrimitiveType.Cylinder, "Wheel" + i, wp[i], new Vector3(0.80f, 0.10f, 0.80f), darkWood, go.transform);
            w.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
        // Horse-ish: body box, head box, four cylinder legs, hitched ahead.
        var horseMat = WorldBuilder.MakeMat(new Color(0.38f, 0.26f, 0.16f));
        Part(PrimitiveType.Cube, "HorseBody", new Vector3(1.85f, 0.95f, 0f), new Vector3(1.1f, 0.55f, 0.45f), horseMat, go.transform);
        Part(PrimitiveType.Cube, "HorseHead", new Vector3(2.55f, 1.25f, 0f), new Vector3(0.35f, 0.45f, 0.30f), horseMat, go.transform);
        Vector3[] lp = {
            new Vector3(1.55f, 0.35f, 0.18f), new Vector3(1.55f, 0.35f, -0.18f),
            new Vector3(2.15f, 0.35f, 0.18f), new Vector3(2.15f, 0.35f, -0.18f)
        };
        for (int i = 0; i < 4; i++)
            Part(PrimitiveType.Cylinder, "Leg" + i, lp[i], new Vector3(0.12f, 0.70f, 0.12f), darkWood, go.transform);
        // Hitch pole from cart to horse.
        var pole = Part(PrimitiveType.Cylinder, "Pole", new Vector3(1.25f, 0.80f, 0f), new Vector3(0.08f, 1.1f, 0.08f), darkWood, go.transform);
        pole.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        return v;
    }

    static Vehicle MakeHover(float x, float z, float speed)
    {
        var v = new Vehicle { x = x, z = z, speed = speed, baseY = 0.55f, phase = x * 0.5f, hover = true };
        var go = new GameObject("HoverCar");
        go.transform.SetParent(root.transform, false);
        go.transform.position = new Vector3(x, 0.55f, z);
        v.go = go;
        // Sleek low body, glass canopy, glow underneath. No wheels.
        Part(PrimitiveType.Cube, "Body", new Vector3(0f, 0f, 0f), new Vector3(2.1f, 0.32f, 0.90f),
            WorldBuilder.MakeMat(new Color(0.18f, 0.20f, 0.26f)), go.transform);
        var nose = Part(PrimitiveType.Cube, "Nose", new Vector3(1.15f, -0.04f, 0f), new Vector3(0.50f, 0.20f, 0.70f),
            WorldBuilder.MakeMat(new Color(0.22f, 0.24f, 0.30f)), go.transform);
        nose.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
        Part(PrimitiveType.Cube, "Canopy", new Vector3(-0.15f, 0.28f, 0f), new Vector3(0.95f, 0.28f, 0.66f),
            WorldBuilder.MakeGlow(new Color(0.15f, 0.45f, 0.65f), 0.8f), go.transform);
        Part(PrimitiveType.Cube, "UnderGlow", new Vector3(0f, -0.30f, 0f), new Vector3(1.7f, 0.08f, 0.75f),
            WorldBuilder.MakeGlow(new Color(0.20f, 0.85f, 1f), 1.6f), go.transform);
        return v;
    }

    // ---------------- per-frame ----------------

    public void Tick(float dt)
    {
        if (vehicles == null) return;
        time += dt;
        foreach (var v in vehicles)
        {
            if (v == null || v.go == null) continue;
            v.x += v.dir * v.speed * dt;
            if (v.x > MaxX) v.x = MinX;
            else if (v.x < MinX) v.x = MaxX;
            float y = v.baseY;
            if (v.hover) y += Mathf.Sin(time * 3f + v.phase) * 0.08f;
            v.go.transform.position = new Vector3(v.x, y, v.z);
        }
    }
}
