using UnityEngine;

// Giant-build world dressing: the medieval watchtower hazard, the future
// neon arch, per-timeline shift rings, coins, and Mom's diner (the fixed
// point across every timeline).
public static class TimelineProps
{
    public static void Build(Transform present, Transform medieval, Transform future, Transform shared)
    {
        GameData.ClearWorld();
        BuildPresentProps(present);
        BuildMedievalProps(medieval);
        BuildFutureProps(future);
        BuildCoins(shared);
    }

    // ---------------- helpers (local copies; WorldBuilder's are private) ----------------
    static void NoColliders(GameObject go)
    {
        foreach (var c in go.GetComponents<Collider>()) Object.Destroy(c);
    }

    static GameObject Box(string name, Vector3 pos, Vector3 size, Material mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        NoColliders(go);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().material = mat;
        return go;
    }

    static GameObject Cyl(string name, Vector3 pos, float radius, float height, Material mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        NoColliders(go);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(radius * 2f, height / 2f, radius * 2f);
        go.GetComponent<Renderer>().material = mat;
        return go;
    }

    static GameObject Cone(string name, Vector3 pos, float radius, float height, Material mat, Transform parent)
    {
        // Custom cone mesh (unit-sized): scale maps it to radius/height.
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(radius * 2f, height, radius * 2f);
        var mf = go.AddComponent<MeshFilter>();
        var mesh = new Mesh();
        int seg = 8;
        var verts = new System.Collections.Generic.List<Vector3> { new Vector3(0f, 0.5f, 0f) };
        for (int i = 0; i <= seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            verts.Add(new Vector3(Mathf.Cos(a) * 0.5f, -0.5f, Mathf.Sin(a) * 0.5f));
        }
        var tris = new System.Collections.Generic.List<int>();
        for (int i = 1; i <= seg; i++) { tris.Add(0); tris.Add(i); tris.Add(i + 1); }
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mf.mesh = mesh;
        go.AddComponent<MeshRenderer>().material = mat;
        return go;
    }

    // ---------------- shift rings ----------------
    static void BuildRing(Transform parent, Vector3 pos, float radius, Color glow, int timeline)
    {
        var root = new GameObject("ShiftRing");
        root.transform.SetParent(parent, false);
        root.transform.position = pos;
        var mat = WorldBuilder.MakeGlow(glow, 2.5f);
        int segs = 12;
        float segLen = 2f * Mathf.PI * radius / segs * 1.2f;
        for (int i = 0; i < segs; i++)
        {
            float a = i / (float)segs * Mathf.PI * 2f;
            var b = Box("Seg" + i,
                new Vector3(0f, Mathf.Cos(a) * radius, Mathf.Sin(a) * radius),
                new Vector3(0.55f, segLen, 0.55f), mat, root.transform);
            b.transform.localPosition = new Vector3(0f, Mathf.Cos(a) * radius, Mathf.Sin(a) * radius);
            b.transform.localRotation = Quaternion.Euler(-a * Mathf.Rad2Deg, 0f, 0f);
        }
        root.AddComponent<RingSpin>();
        GameData.rings.Add(new RingDef { timeline = timeline, pos = pos, radius = radius, taken = false });
    }

    // ---------------- coins ----------------
    static void BuildCoin(Transform parent, Vector3 pos)
    {
        var root = new GameObject("Coin");
        root.transform.SetParent(parent, false);
        root.transform.position = pos;
        var gold = WorldBuilder.MakeGlow(new Color(1f, 0.8f, 0.25f), 1.6f);
        var c = Cyl("C", Vector3.zero, 0.55f, 0.14f, gold, root.transform);
        c.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        root.AddComponent<CoinSpin>();
        GameData.coins.Add(new CoinDef { pos = pos, taken = false, node = root.transform });
    }

    static void BuildCoins(Transform shared)
    {
        // Reward arc over the gap.
        float[] xs = { 90f, 95f, 100f, 105f, 110f };
        float[] ys = { 11.5f, 12.5f, 13f, 12.5f, 11.5f };
        for (int i = 0; i < xs.Length; i++)
            BuildCoin(shared, new Vector3(xs[i], ys[i], 0f));
        // Landing-line coins.
        for (int i = 0; i < 4; i++)
            BuildCoin(shared, new Vector3(130f + i * 4f, 1.6f, 0f));
    }

    // ---------------- PRESENT: Mom's diner ----------------
    static void BuildPresentProps(Transform p)
    {
        var cream = WorldBuilder.MakeMat(new Color(0.92f, 0.88f, 0.80f));
        var red = WorldBuilder.MakeMat(new Color(0.75f, 0.18f, 0.16f));
        var warmGlow = WorldBuilder.MakeGlow(new Color(1f, 0.75f, 0.4f), 1.2f);
        Box("Diner", new Vector3(20f, 2f, 10f), new Vector3(9f, 4f, 6f), cream, p);
        Box("DinerRoof", new Vector3(20f, 4.4f, 10f), new Vector3(9.8f, 0.7f, 6.8f), red, p);
        Box("DinerWin", new Vector3(20f, 1.8f, 6.9f), new Vector3(7f, 1.6f, 0.2f), warmGlow, p);
        Box("DinerSign", new Vector3(20f, 6.2f, 10f), new Vector3(8f, 1.6f, 0.4f), red, p);
        var sl = UIUtil.MakeWorldLabel("MOM'S DINER", 0.024f, Color.white, 7.4f);
        sl.transform.SetParent(p, false);
        sl.transform.position = new Vector3(20f, 6.2f, 9.7f);
        // Ring over the gap: thread it for a boost + charge.
        BuildRing(p, new Vector3(100f, 12f, 0f), 3f, new Color(0.3f, 0.9f, 1f), 0);
    }

    // ---------------- MEDIEVAL: watchtower hazard ----------------
    static void BuildMedievalProps(Transform p)
    {
        var stone = WorldBuilder.MakeMat(new Color(0.45f, 0.42f, 0.38f));
        var darkStone = WorldBuilder.MakeMat(new Color(0.30f, 0.28f, 0.26f));
        var wood = WorldBuilder.MakeMat(new Color(0.42f, 0.30f, 0.18f));
        // The tower rises out of the ravine, right in the flight path.
        // Weak jumps clip it; fast ones sail over. Or just shift away.
        Box("Tower", new Vector3(100f, -1.75f, 0f), new Vector3(3.4f, 23.5f, 3.4f), stone, p);
        Box("TowerCap", new Vector3(100f, 10.5f, 0f), new Vector3(4.4f, 1f, 4.4f), darkStone, p);
        Box("TowerWin", new Vector3(100f, 6f, 1.75f), new Vector3(1f, 1.6f, 0.2f),
            WorldBuilder.MakeGlow(new Color(1f, 0.6f, 0.2f), 1.5f), p);
        // Banner pole off to the side, clear of the flight line.
        Cyl("BannerPole", new Vector3(101.6f, 12.5f, 1.5f), 0.12f, 4f, wood, p);
        Box("Banner", new Vector3(102.6f, 13.6f, 1.5f), new Vector3(1.8f, 1.1f, 0.1f),
            WorldBuilder.MakeMat(new Color(0.7f, 0.12f, 0.12f)), p);
        GameData.towers.Add(new TowerDef { timeline = 1, x = 100f, topY = 11f, halfW = 2.2f });

        // Palisade stakes along the road.
        for (int i = 0; i < 14; i++)
            Cone("Stake" + i, new Vector3(-4f + i * 2.4f, 1.5f, -5.5f), 0.5f, 3f, wood, p);
        // Dead trees.
        for (int i = 0; i < 3; i++)
        {
            float x = 24f + i * 18f;
            var trunk = Cyl("DeadTree" + i, new Vector3(x, 2f, -7f), 0.35f, 4f,
                WorldBuilder.MakeMat(new Color(0.25f, 0.2f, 0.16f)), p);
            trunk.transform.rotation = Quaternion.Euler(0f, 0f, 8f + i * 5f);
        }
        // Torch posts.
        for (int i = 0; i < 3; i++)
        {
            float x = 10f + i * 20f;
            Cyl("Torch" + i, new Vector3(x, 1f, -4f), 0.12f, 2f, wood, p);
            var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "Flame" + i;
            NoColliders(flame);
            flame.transform.SetParent(p, false);
            flame.transform.position = new Vector3(x, 2.3f, -4f);
            flame.transform.localScale = new Vector3(0.5f, 0.7f, 0.5f);
            flame.GetComponent<Renderer>().material = WorldBuilder.MakeGlow(new Color(1f, 0.55f, 0.15f), 2.5f);
        }
        // Low ring past the landing: ride through it.
        BuildRing(p, new Vector3(140f, 2.5f, 0f), 2.5f, new Color(1f, 0.7f, 0.25f), 1);
    }

    // ---------------- FUTURE: neon arch ----------------
    static void BuildFutureProps(Transform p)
    {
        var metal = WorldBuilder.MakeMat(new Color(0.22f, 0.25f, 0.32f));
        var cyan = WorldBuilder.MakeGlow(new Color(0.2f, 0.9f, 1f), 2.5f);
        var mag = WorldBuilder.MakeGlow(new Color(1f, 0.3f, 0.85f), 2.5f);
        // Arch straddling the gap: pylons rise from below, beam across.
        Box("ArchL", new Vector3(100f, 0f, -4.5f), new Vector3(1.4f, 26f, 1.4f), metal, p);
        Box("ArchR", new Vector3(100f, 0f, 4.5f), new Vector3(1.4f, 26f, 1.4f), metal, p);
        Box("ArchBeam", new Vector3(100f, 13.4f, 0f), new Vector3(1.4f, 1.4f, 10.4f), metal, p);
        Box("ArchGlow", new Vector3(100f, 12.5f, 0f), new Vector3(0.5f, 0.5f, 9f), cyan, p);
        // Thread the arch ring for a boost + charge.
        BuildRing(p, new Vector3(100f, 12f, 0f), 3f, new Color(1f, 0.3f, 0.85f), 2);
        // Holo billboards.
        HoloBillboard(p, new Vector3(58f, 8f, -8f), "TIME OUT", new Color(0.3f, 0.9f, 1f));
        HoloBillboard(p, new Vector3(150f, 7f, -9f), "JACK WAS HERE", new Color(1f, 0.4f, 0.85f));
        // Streetlight pylons.
        for (int i = 0; i < 3; i++)
        {
            float x = 20f + i * 22f;
            Box("Pylon" + i, new Vector3(x, 3f, -5f), new Vector3(0.5f, 6f, 0.5f), metal, p);
            Box("PylonGlow" + i, new Vector3(x, 5.8f, -5f), new Vector3(1.6f, 0.3f, 0.3f), cyan, p);
        }
    }

    static void HoloBillboard(Transform p, Vector3 pos, string text, Color glow)
    {
        var metal = WorldBuilder.MakeMat(new Color(0.22f, 0.25f, 0.32f));
        Box("HoloPole", new Vector3(pos.x, 2f, pos.z), new Vector3(0.6f, 4f, 0.6f), metal, p);
        var holo = Box("Holo", pos, new Vector3(7f, 3f, 0.25f),
            WorldBuilder.MakeMat(new Color(glow.r, glow.g, glow.b), 0.4f), p);
        holo.transform.rotation = Quaternion.Euler(0f, 12f, 0f);
        var sl = UIUtil.MakeWorldLabel(text, 0.03f, glow, 6.4f);
        sl.transform.SetParent(p, false);
        sl.transform.position = pos + new Vector3(0f, 0f, -0.4f);
        sl.transform.rotation = Quaternion.Euler(0f, 12f, 0f);
    }
}

// Slow in-plane spin for shift rings.
public class RingSpin : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(50f * Time.deltaTime, 0f, 0f);
    }
}

// Coin shimmer spin.
public class CoinSpin : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(0f, 140f * Time.deltaTime, 0f);
    }
}
