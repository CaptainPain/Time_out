using UnityEngine;
using System.Collections.Generic;

// Present-timeline ground surfaces: asphalt (grippy), dirt roads, gravel
// (loose, less grip, dust kick-up). Surface affects bike handling.
public enum Surface { Asphalt, Dirt, Gravel }

public static class SurfaceManager
{
    // Surface zones on the present-timeline map (x ranges).
    static readonly (float from, float to, Surface s)[] zones = new (float, float, Surface)[]
    {
        (-8f, 25f, Surface.Asphalt),   // town strip: paved
        (25f, 45f, Surface.Dirt),       // dirt road past the diner
        (45f, 60f, Surface.Asphalt),    // paved approach to the hill
        (60f, 80f, Surface.Dirt),       // hill: dirt
        (120f, 160f, Surface.Gravel),   // landing zone: loose gravel
        (160f, 220f, Surface.Asphalt),  // final stretch: paved
    };

    public static Surface At(float x)
    {
        foreach (var z in zones)
            if (x >= z.from && x < z.to) return z.s;
        return Surface.Asphalt;
    }

    public static float Grip(Surface s)
    {
        switch (s)
        {
            case Surface.Dirt: return 0.85f;
            case Surface.Gravel: return 0.70f;
            default: return 1f;
        }
    }

    public static float DustAmount(Surface s)
    {
        switch (s)
        {
            case Surface.Dirt: return 0.6f;
            case Surface.Gravel: return 1f;
            default: return 0.15f;
        }
    }

    public static string Name(Surface s)
    {
        return s == Surface.Dirt ? "DIRT" : s == Surface.Gravel ? "GRAVEL" : "ASPHALT";
    }
}

// Modern obstacles for the present timeline: barrels, parked cars, wooden
// pallets, food trucks. All have collision; food trucks double as comedy
// set pieces for NPC bits.
public static class ObstacleManager
{
    public struct Obstacle
    {
        public float x;
        public float halfW;
        public float topY;
        public string kind; // "barrel", "car", "pallet", "foodtruck"
    }

    static readonly List<Obstacle> obstacles = new List<Obstacle>();

    public static void Build(Transform parent)
    {
        obstacles.Clear();
        var root = new GameObject("Obstacles").transform;
        root.SetParent(parent, false);

        // Barrels near the start (knockable, but they slow you).
        AddBarrel(root, 18f);
        AddBarrel(root, 19.5f);
        AddBarrel(root, 52f);
        // Parked cars along the strip.
        AddCar(root, 32f, new Color(0.2f, 0.3f, 0.8f));
        AddCar(root, 48f, new Color(0.8f, 0.2f, 0.2f));
        // Wooden pallets before the hill.
        AddPallet(root, 58f);
        AddPallet(root, 59.5f);
        // Food trucks: comedy set pieces (NPC bits happen here).
        AddFoodTruck(root, 38f, "TACOS");
        AddFoodTruck(root, 150f, "NOODLES");
        // More barrels in the gravel landing zone.
        AddBarrel(root, 135f);
        AddBarrel(root, 145f);
    }

    static void AddBarrel(Transform parent, float x)
    {
        float gy = WorldBuilder.GetGroundY(x);
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Barrel";
        foreach (var c in go.GetComponents<Collider>()) Object.Destroy(c);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, gy + 0.35f, 0f);
        go.transform.localScale = new Vector3(0.6f, 0.7f, 0.6f);
        go.GetComponent<Renderer>().material = WorldBuilder.MakeMat(new Color(0.7f, 0.3f, 0.1f));
        obstacles.Add(new Obstacle { x = x, halfW = 0.35f, topY = gy + 0.7f, kind = "barrel" });
    }

    static void AddCar(Transform parent, float x, Color color)
    {
        float gy = WorldBuilder.GetGroundY(x);
        var car = new GameObject("ParkedCar").transform;
        car.SetParent(parent, false);
        car.position = new Vector3(x, gy, 0f);
        // Body.
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        foreach (var c in body.GetComponents<Collider>()) Object.Destroy(c);
        body.transform.SetParent(car, false);
        body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        body.transform.localScale = new Vector3(3.2f, 0.7f, 1.6f);
        body.GetComponent<Renderer>().material = WorldBuilder.MakeMat(color);
        // Cabin.
        var cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.name = "Cabin";
        foreach (var c in cabin.GetComponents<Collider>()) Object.Destroy(c);
        cabin.transform.SetParent(car, false);
        cabin.transform.localPosition = new Vector3(-0.2f, 1.1f, 0f);
        cabin.transform.localScale = new Vector3(1.6f, 0.55f, 1.4f);
        cabin.GetComponent<Renderer>().material = WorldBuilder.MakeMat(new Color(0.15f, 0.18f, 0.22f));
        // Wheels.
        for (int i = 0; i < 4; i++)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            w.name = "Wheel" + i;
            foreach (var c in w.GetComponents<Collider>()) Object.Destroy(c);
            w.transform.SetParent(car, false);
            w.transform.localPosition = new Vector3(i < 2 ? 1.1f : -1.1f, 0.3f, i % 2 == 0 ? 0.85f : -0.85f);
            w.transform.localScale = new Vector3(0.6f, 0.25f, 0.6f);
            w.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            w.GetComponent<Renderer>().material = WorldBuilder.MakeMat(new Color(0.1f, 0.1f, 0.1f));
        }
        obstacles.Add(new Obstacle { x = x, halfW = 1.7f, topY = gy + 1.4f, kind = "car" });
    }

    static void AddPallet(Transform parent, float x)
    {
        float gy = WorldBuilder.GetGroundY(x);
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Pallet";
        foreach (var c in go.GetComponents<Collider>()) Object.Destroy(c);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, gy + 0.15f, 0f);
        go.transform.localScale = new Vector3(1.2f, 0.3f, 1.0f);
        go.GetComponent<Renderer>().material = WorldBuilder.MakeMat(new Color(0.55f, 0.42f, 0.28f));
        obstacles.Add(new Obstacle { x = x, halfW = 0.65f, topY = gy + 0.3f, kind = "pallet" });
    }

    static void AddFoodTruck(Transform parent, float x, string label)
    {
        float gy = WorldBuilder.GetGroundY(x);
        var truck = new GameObject("FoodTruck").transform;
        truck.SetParent(parent, false);
        truck.position = new Vector3(x, gy, 2.5f); // off to the side, not blocking
        // Truck body.
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "TruckBody";
        foreach (var c in body.GetComponents<Collider>()) Object.Destroy(c);
        body.transform.SetParent(truck, false);
        body.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        body.transform.localScale = new Vector3(4.5f, 2.0f, 2.2f);
        body.GetComponent<Renderer>().material = WorldBuilder.MakeMat(new Color(0.95f, 0.85f, 0.3f));
        // Serving window (glow).
        var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
        win.name = "Window";
        foreach (var c in win.GetComponents<Collider>()) Object.Destroy(c);
        win.transform.SetParent(truck, false);
        win.transform.localPosition = new Vector3(0f, 1.4f, -1.15f);
        win.transform.localScale = new Vector3(3.0f, 0.9f, 0.1f);
        win.GetComponent<Renderer>().material = WorldBuilder.MakeGlow(new Color(1f, 0.9f, 0.6f), 1.2f);
        // Sign.
        var sign = UIUtil.MakeWorldLabel(label, 0.5f, Color.red, 4f);
        sign.transform.SetParent(truck, false);
        sign.transform.localPosition = new Vector3(0f, 2.6f, 0f);
        // No collision: food trucks are off to the side (z=2.5), comedy set pieces.
    }

    // Returns the obstacle hit, or null if none. Only for present timeline.
    public static Obstacle? CheckHit(Vector3 p)
    {
        if (TimelineManager.Active != Timeline.Present) return null;
        foreach (var o in obstacles)
        {
            if (Mathf.Abs(p.x - o.x) < o.halfW + 0.5f && p.y - 0.6f < o.topY)
                return o;
        }
        return null;
    }
}
