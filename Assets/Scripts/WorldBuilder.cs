using UnityEngine;
using System.Collections.Generic;

// Builds the entire level from primitives: one shared terrain ribbon, three
// timeline dressing sets occupying the SAME map coordinates, parallax
// backgrounds, lighting. Zero art assets required.
public static class WorldBuilder
{
    // ---- Shared geography (identical in every timeline) ----
    public const float HillStart = 60f;
    public const float GapStart = 80f;
    public const float GapEnd = 120f;
    public const float FinishX = 220f;
    public const float StartX = 6f;
    public const float KillY = -14f;

    // NaN = no ground (the gap).
    public static float GetGroundY(float x)
    {
        if (x < HillStart) return 0f;
        if (x < 74f)
        {
            float t = (x - HillStart) / 14f;
            t = t * t * (3f - 2f * t);
            return t * 6.2f;
        }
        if (x < GapStart)
        {
            float t = (x - 74f) / 6f;
            return 6.2f + 2.4f * Mathf.Pow(t, 1.35f); // launch lip
        }
        if (x < GapEnd) return float.NaN;
        return 0f;
    }

    public static float GetSlope(float x)
    {
        float h = 0.75f;
        float a = GetGroundY(x - h), b = GetGroundY(x + h);
        if (float.IsNaN(a) || float.IsNaN(b)) return 0f;
        return (b - a) / (2f * h);
    }

    // ---- Per-timeline look ----
    static readonly Color[] skyColors =
    {
        new Color(0.55f, 0.78f, 0.92f),  // 1999: warm afternoon
        new Color(0.87f, 0.74f, 0.55f),  // 999: dusty gold
        new Color(0.09f, 0.14f, 0.26f),  // 2999: cold neon dusk
    };
    static readonly Color[] sunColors =
    {
        new Color(1f, 0.96f, 0.88f),
        new Color(1f, 0.82f, 0.55f),
        new Color(0.55f, 0.75f, 1f),
    };
    static readonly float[] sunIntensity = { 1.15f, 1.05f, 0.9f };
    static readonly Color[] ambientColors =
    {
        new Color(0.55f, 0.60f, 0.66f),
        new Color(0.62f, 0.55f, 0.45f),
        new Color(0.25f, 0.32f, 0.45f),
    };

    static GameObject root;
    public static Transform TownRoot { get; private set; }
    static GameObject[] timelineGroups = new GameObject[3];
    static Renderer groundRenderer;
    static Material[] groundMats = new Material[3];
    static Light sun;

    public static void Build()
    {
        if (root != null) Object.Destroy(root);
        timelineGroups = new GameObject[3];
        groundMats = new Material[3];

        root = new GameObject("World");

        // Town strip root: storefronts, diner, NPCs, traffic all parent here.
        var townGo = new GameObject("Town");
        townGo.transform.SetParent(root.transform, false);
        TownRoot = townGo.transform;

        // Sun.
        var sunGo = new GameObject("Sun");
        sunGo.transform.SetParent(root.transform, false);
        sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 40f;
        RenderSettings.fogEndDistance = 160f;

        // Shared terrain ribbon (material swaps per timeline).
        groundMats[0] = MakeMat(new Color(0.28f, 0.28f, 0.30f)); // 1999 asphalt/dirt
        groundMats[1] = MakeMat(new Color(0.45f, 0.34f, 0.24f)); // 999 dirt
        groundMats[2] = MakeMat(new Color(0.14f, 0.16f, 0.21f)); // 2999 deck
        var ribbon = new GameObject("Ground");
        ribbon.transform.SetParent(root.transform, false);
        groundRenderer = ribbon.AddComponent<MeshRenderer>();
        var mf = ribbon.AddComponent<MeshFilter>();
        mf.mesh = BuildRibbon();
        groundRenderer.material = groundMats[0];

        // Grass strip on the 1999 hill.
        var grass = new GameObject("HillGrass");
        grass.transform.SetParent(root.transform, false);
        var gr = grass.AddComponent<MeshRenderer>();
        var gmf = grass.AddComponent<MeshFilter>();
        gmf.mesh = BuildHillSkin();
        gr.material = MakeMat(new Color(0.30f, 0.60f, 0.30f));
        timelineGroups[0] = new GameObject("T0");
        timelineGroups[0].transform.SetParent(root.transform, false);
        grass.transform.SetParent(timelineGroups[0].transform, false);

        // The three timeline worlds.
        timelineGroups[0].name = "T_Present";
        BuildPresent(timelineGroups[0].transform);
        timelineGroups[1] = new GameObject("T_Medieval");
        timelineGroups[1].transform.SetParent(root.transform, false);
        BuildMedieval(timelineGroups[1].transform);
        timelineGroups[2] = new GameObject("T_Future");
        timelineGroups[2].transform.SetParent(root.transform, false);
        BuildFuture(timelineGroups[2].transform);

        BuildFinishArch(root.transform);
        BuildJumpSign(root.transform);

        // Giant-build dressing: tower, arch, rings, coins, the diner.
        TimelineProps.Build(timelineGroups[0].transform, timelineGroups[1].transform,
            timelineGroups[2].transform, root.transform);
    }

    public static void ApplyTimeline(Timeline t)
    {
        int i = (int)t;
        for (int k = 0; k < 3; k++)
            if (timelineGroups[k] != null) timelineGroups[k].SetActive(k == i);
        if (groundRenderer != null) groundRenderer.material = groundMats[i];
        if (Camera.main != null) Camera.main.backgroundColor = skyColors[i];
        RenderSettings.fogColor = skyColors[i];
        RenderSettings.ambientLight = ambientColors[i];
        if (sun != null) { sun.color = sunColors[i]; sun.intensity = sunIntensity[i]; }
    }

    // ---------------- materials ----------------
    static Shader FindShader()
    {
        var s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        return s;
    }

    public static Material MakeMat(Color c, float alpha = 1f)
    {
        var m = new Material(FindShader());
        c.a = alpha;
        m.color = c;
        if (alpha < 1f)
        {
            // URP Lit transparency: _Surface = 1 (transparent), _Blend = 0 (alpha).
            // Unknown property names are ignored by the built-in pipeline.
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        return m;
    }

    public static Material MakeGlow(Color c, float intensity = 2f)
    {
        var m = MakeMat(c);
        if (m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * intensity);
        }
        return m;
    }

    // ---------------- primitive helpers ----------------
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
        go.transform.localPosition = pos;
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
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(radius * 2f, height / 2f, radius * 2f);
        go.GetComponent<Renderer>().material = mat;
        return go;
    }

    static GameObject Cone(string name, Vector3 pos, float radius, float height, Material mat, Transform parent)
    {
        // Unity has no cone primitive: use a cylinder pinched at the top.
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var mf = go.AddComponent<MeshFilter>();
        mf.mesh = ConeMesh(radius, height);
        go.AddComponent<MeshRenderer>().material = mat;
        return go;
    }

    static Mesh ConeMesh(float radius, float height)
    {
        var mesh = new Mesh();
        int seg = 10;
        var verts = new List<Vector3> { new Vector3(0f, height / 2f, 0f) };
        for (int i = 0; i <= seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            verts.Add(new Vector3(Mathf.Cos(a) * radius, -height / 2f, Mathf.Sin(a) * radius));
        }
        var tris = new List<int>();
        for (int i = 1; i <= seg; i++) { tris.Add(0); tris.Add(i); tris.Add(i + 1); }
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    static GameObject Ball(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        NoColliders(go);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().material = mat;
        return go;
    }

    // ---------------- terrain ribbon ----------------
    static Mesh BuildRibbon()
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        float halfW = 4f, skirt = 4f;
        float[][] ranges = { new float[] { -20f, GapStart }, new float[] { GapEnd, 250f } };
        foreach (var r in ranges)
        {
            int baseIndex = verts.Count;
            int n = 0;
            float lastY = 0f;
            for (float x = r[0]; x <= r[1] + 0.01f; x += 1f, n++)
            {
                float y = GetGroundY(x);
                if (float.IsNaN(y)) y = lastY; // hold the edge height at the gap lip
                else lastY = y;
                verts.Add(new Vector3(x, y, -halfW));
                verts.Add(new Vector3(x, y, halfW));
                verts.Add(new Vector3(x, y - skirt, -halfW));
                verts.Add(new Vector3(x, y - skirt, halfW));
                if (n > 0)
                {
                    int p = baseIndex + (n - 1) * 4, c = baseIndex + n * 4;
                    // top
                    tris.Add(p); tris.Add(c); tris.Add(p + 1);
                    tris.Add(p + 1); tris.Add(c); tris.Add(c + 1);
                    // front skirt (z+)
                    tris.Add(p + 1); tris.Add(c + 1); tris.Add(p + 3);
                    tris.Add(p + 3); tris.Add(c + 1); tris.Add(c + 3);
                    // back skirt (z-)
                    tris.Add(p + 2); tris.Add(p); tris.Add(c + 2);
                    tris.Add(c + 2); tris.Add(p); tris.Add(c);
                }
            }
        }
        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // Thin grass skin hugging the 1999 hill.
    static Mesh BuildHillSkin()
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        float halfW = 4.05f;
        int n = 0;
        float lastY = 0f;
        for (float x = HillStart; x <= GapStart + 0.01f; x += 1f, n++)
        {
            float y = GetGroundY(x) + 0.06f;
            if (float.IsNaN(y)) y = lastY; // hold the lip's edge height
            else lastY = y;
            verts.Add(new Vector3(x, y, -halfW));
            verts.Add(new Vector3(x, y, halfW));
            if (n > 0)
            {
                int p = (n - 1) * 2, c = n * 2;
                tris.Add(p); tris.Add(c); tris.Add(p + 1);
                tris.Add(p + 1); tris.Add(c); tris.Add(c + 1);
            }
        }
        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    // ---------------- shared props ----------------
    static void BuildFinishArch(Transform parent)
    {
        var mat = MakeMat(Color.white);
        var dark = MakeMat(new Color(0.15f, 0.15f, 0.18f));
        Box("FinPostL", new Vector3(FinishX, 2f, -3f), new Vector3(0.5f, 4f, 0.5f), dark, parent);
        Box("FinPostR", new Vector3(FinishX, 2f, 3f), new Vector3(0.5f, 4f, 0.5f), dark, parent);
        Box("FinTop", new Vector3(FinishX, 4.2f, 0f), new Vector3(0.6f, 1f, 6.6f), mat, parent);
        // checkers
        for (int i = 0; i < 8; i++)
            Box("Chk" + i, new Vector3(FinishX, 4.2f, -2.8f + i * 0.8f),
                new Vector3(0.65f, 0.5f, 0.8f), MakeMat(i % 2 == 0 ? Color.black : Color.white), parent);
        var label = UIUtil.MakeWorldLabel("FINISH", 0.03f, Color.black);
        label.transform.SetParent(parent, false);
        label.transform.position = new Vector3(FinishX, 4.2f, -3.6f);
    }

    static void BuildJumpSign(Transform parent)
    {
        var wood = MakeMat(new Color(0.5f, 0.35f, 0.2f));
        Cyl("SignPost", new Vector3(54f, 1f, 3f), 0.12f, 2f, wood, parent);
        Box("SignBoard", new Vector3(54f, 2.4f, 3f), new Vector3(7f, 1.4f, 0.15f), MakeMat(new Color(0.95f, 0.8f, 0.2f)), parent);
        var label = UIUtil.MakeWorldLabel("JUMP >", 0.03f, Color.black, 6.2f);
        label.transform.SetParent(parent, false);
        label.transform.position = new Vector3(54f, 2.4f, 2.9f);
    }

    // ---------------- PRESENT 1999 ----------------
    static void BuildPresent(Transform p)
    {
        var roadMat = MakeMat(new Color(0.16f, 0.16f, 0.18f));
        var lineMat = MakeMat(Color.white);
        // Freeway below the gap.
        Box("Freeway", new Vector3(100f, -6f, 0f), new Vector3(46f, 0.5f, 9f), roadMat, p);
        for (float x = 82f; x < 120f; x += 4f)
            Box("Lane", new Vector3(x, -5.7f, 0f), new Vector3(1.6f, 0.06f, 0.25f), lineMat, p);
        // Traffic.
        var carCols = new Color[] { new Color(0.8f, 0.2f, 0.2f), new Color(0.2f, 0.4f, 0.9f), new Color(0.9f, 0.8f, 0.2f), new Color(0.3f, 0.3f, 0.35f) };
        for (int i = 0; i < 4; i++)
        {
            var car = new GameObject("Car" + i);
            car.transform.SetParent(p, false);
            car.transform.position = new Vector3(88f + i * 9f, -5.1f, (i % 2 == 0 ? -2f : 2f));
            var cm = MakeMat(carCols[i]);
            Box("Body", Vector3.zero, new Vector3(2.4f, 0.7f, 1.5f), cm, car.transform);
            Box("Top", new Vector3(-0.2f, 0.55f, 0f), new Vector3(1.2f, 0.5f, 1.3f), cm, car.transform);
            var mover = car.AddComponent<PropMover>();
            mover.axis = Vector3.right;
            mover.range = 14f;
            mover.speed = (i % 2 == 0 ? 10f : -13f) * (1f + i * 0.12f);
        }
        // Town silhouettes.
        var houseCols = new Color[] { new Color(0.85f, 0.7f, 0.6f), new Color(0.7f, 0.8f, 0.85f), new Color(0.9f, 0.85f, 0.7f), new Color(0.75f, 0.65f, 0.7f) };
        for (int i = 0; i < 5; i++)
        {
            float x = -8f + i * 13f;
            Box("House" + i, new Vector3(x, 2f, 8.5f), new Vector3(7f, 4f, 5f), MakeMat(houseCols[i % 4]), p);
            Box("Roof" + i, new Vector3(x, 4.4f, 8.5f), new Vector3(7.8f, 0.7f, 5.8f), MakeMat(new Color(0.5f, 0.3f, 0.25f)), p);
        }
        // Lackluster Video.
        Box("Store", new Vector3(48f, 2.5f, 9f), new Vector3(10f, 5f, 6f), MakeMat(new Color(0.25f, 0.3f, 0.65f)), p);
        Box("StoreSign", new Vector3(48f, 6f, 9f), new Vector3(12.5f, 1.8f, 0.4f), MakeMat(new Color(0.95f, 0.85f, 0.2f)), p);
        var sl = UIUtil.MakeWorldLabel("LACKLUSTER VIDEO", 0.022f, new Color(0.1f, 0.1f, 0.5f), 11.5f);
        sl.transform.SetParent(p, false);
        sl.transform.position = new Vector3(48f, 6f, 8.7f);
        // Grass tufts on the hill.
        var tuft = MakeMat(new Color(0.25f, 0.55f, 0.25f));
        for (int i = 0; i < 10; i++)
        {
            float x = 60f + i * 2f;
            Cone("Tuft" + i, new Vector3(x, GetGroundY(x) + 0.4f, Random.Range(-3f, 3f)), 0.35f, 0.9f, tuft, p);
        }
        BuildSky(p,
            Ball("SunP", new Vector3(150f, 42f, 46f), new Vector3(14f, 14f, 2f), MakeMat(new Color(1f, 0.95f, 0.8f)), p),
            new Color(0.45f, 0.52f, 0.72f), new Color(0.38f, 0.62f, 0.38f), 4);
    }

    // ---------------- MEDIEVAL 999 ----------------
    static void BuildMedieval(Transform p)
    {
        var rock = MakeMat(new Color(0.42f, 0.36f, 0.32f));
        var darkRock = MakeMat(new Color(0.25f, 0.21f, 0.19f));
        // Ravine pit.
        Box("Pit", new Vector3(100f, -14f, 0f), new Vector3(46f, 0.5f, 12f), MakeMat(new Color(0.08f, 0.07f, 0.08f)), p);
        // Rock walls at the gap edges.
        var wl = Box("WallL", new Vector3(79f, -7f, 0f), new Vector3(3f, 15f, 10f), rock, p);
        wl.transform.rotation = Quaternion.Euler(0f, 0f, 18f);
        var wr = Box("WallR", new Vector3(121f, -7f, 0f), new Vector3(3f, 15f, 10f), rock, p);
        wr.transform.rotation = Quaternion.Euler(0f, 0f, -18f);
        // Spikes in the pit.
        for (int i = 0; i < 7; i++)
            Cone("Spike" + i, new Vector3(84f + i * 5.5f, -11.5f, Random.Range(-3f, 3f)), 1.3f, 5f, darkRock, p);
        // Huts.
        var thatch = MakeMat(new Color(0.72f, 0.58f, 0.35f));
        var hutMat = MakeMat(new Color(0.55f, 0.42f, 0.3f));
        for (int i = 0; i < 4; i++)
        {
            float x = -6f + i * 14f;
            Cyl("Hut" + i, new Vector3(x, 1.2f, 9f), 2f, 2.4f, hutMat, p);
            Cone("HutRoof" + i, new Vector3(x, 3.4f, 9f), 2.8f, 2.2f, thatch, p);
        }
        // Castle tower in the distance.
        Cyl("Castle", new Vector3(40f, 8f, 26f), 6f, 16f, MakeMat(new Color(0.5f, 0.46f, 0.42f)), p);
        Cone("CastleTop", new Vector3(40f, 18f, 26f), 7f, 5f, MakeMat(new Color(0.4f, 0.3f, 0.28f)), p);
        // Rocks on the hill.
        for (int i = 0; i < 8; i++)
        {
            float x = 61f + i * 2.4f;
            Ball("Rock" + i, new Vector3(x, GetGroundY(x) + 0.3f, Random.Range(-3f, 3f)),
                new Vector3(1.2f, 0.8f, 1f), rock, p);
        }
        BuildSky(p,
            Ball("SunM", new Vector3(150f, 36f, 46f), new Vector3(18f, 18f, 2f), MakeMat(new Color(1f, 0.85f, 0.6f)), p),
            new Color(0.58f, 0.48f, 0.4f), new Color(0.52f, 0.55f, 0.34f), 3);
    }

    // ---------------- FUTURE 2999 ----------------
    static void BuildFuture(Transform p)
    {
        var metal = MakeMat(new Color(0.20f, 0.23f, 0.30f));
        var glowCyan = MakeGlow(new Color(0.2f, 0.9f, 1f), 2.5f);
        var glowMag = MakeGlow(new Color(1f, 0.3f, 0.8f), 2f);
        // Collapsed pit.
        Box("PitF", new Vector3(100f, -14f, 0f), new Vector3(46f, 0.5f, 12f), MakeMat(new Color(0.03f, 0.04f, 0.07f)), p);
        // Tilted girders across the gap (visual).
        for (int i = 0; i < 3; i++)
        {
            var g = Box("Girder" + i, new Vector3(88f + i * 12f, 5f + i * 2f, -2f + i * 2f),
                new Vector3(16f, 0.8f, 0.8f), metal, p);
            g.transform.rotation = Quaternion.Euler(8f * i, 0f, 14f - i * 9f);
        }
        // Broken deck slabs at the edges.
        var s1 = Box("SlabL", new Vector3(78f, 1.5f, 0f), new Vector3(5f, 0.7f, 8f), metal, p);
        s1.transform.rotation = Quaternion.Euler(0f, 0f, -12f);
        var s2 = Box("SlabR", new Vector3(122f, 1.5f, 0f), new Vector3(5f, 0.7f, 8f), metal, p);
        s2.transform.rotation = Quaternion.Euler(0f, 0f, 12f);
        // Glow strips marking the jump line.
        for (float x = 74f; x <= 80f; x += 2f)
        {
            float gy = GetGroundY(x);
            if (float.IsNaN(gy)) continue; // x == 80 is the gap edge
            Box("Strip", new Vector3(x, gy + 0.15f, -3.6f), new Vector3(1.2f, 0.12f, 0.3f), glowCyan, p);
        }
        for (float x = 120f; x <= 130f; x += 2f)
            Box("StripL", new Vector3(x, 0.15f, -3.6f), new Vector3(1.2f, 0.12f, 0.3f), glowMag, p);
        // Towers.
        for (int i = 0; i < 5; i++)
        {
            float x = -10f + i * 16f, h = 16f + (i * 7f) % 14f;
            Box("Tower" + i, new Vector3(x, h / 2f, 10f + (i % 2) * 4f), new Vector3(4f, h, 4f), metal, p);
            for (int w = 0; w < 4; w++)
                Box("Win" + i + "_" + w, new Vector3(x, 4f + w * 4f, 8f + (i % 2) * 4f),
                    new Vector3(3f, 0.35f, 0.2f), glowCyan, p);
        }
        // Holo billboards.
        var holo = MakeMat(new Color(0.3f, 0.9f, 1f), 0.35f);
        Box("Holo1", new Vector3(60f, 10f, 20f), new Vector3(10f, 5f, 0.2f), holo, p);
        Box("Holo2", new Vector3(150f, 14f, 24f), new Vector3(14f, 6f, 0.2f), holo, p);
        BuildSky(p,
            Ball("MoonF", new Vector3(170f, 48f, 46f), new Vector3(12f, 12f, 2f), MakeMat(new Color(0.8f, 0.9f, 1f)), p),
            new Color(0.14f, 0.20f, 0.36f), new Color(0.10f, 0.18f, 0.28f), 2);
    }

    // Shared sky builder: sun/moon already placed by caller; adds mountains,
    // hills, drifting clouds at three parallax depths.
    static void BuildSky(Transform p, GameObject sunBall, Color farCol, Color midCol, int cloudCount)
    {
        var farMat = MakeMat(farCol);
        var midMat = MakeMat(midCol);
        // Far mountains (z=46).
        for (int i = 0; i < 7; i++)
        {
            float x = -30f + i * 42f + (i % 3) * 8f;
            Cone("Mt" + i, new Vector3(x, 6f, 46f), 20f + (i % 3) * 8f, 34f + (i % 4) * 7f, farMat, p);
        }
        // Mid hills (z=26).
        for (int i = 0; i < 6; i++)
        {
            float x = -20f + i * 48f;
            Ball("Hill" + i, new Vector3(x, -4f, 26f), new Vector3(55f, 16f, 6f), midMat, p);
        }
        // Clouds: small flat puffs, high and far behind the action (z=40) with a
        // short slow drift, so they read as distant sky and can never cross
        // the gameplay view no matter the camera framing.
        var cloudMat = MakeMat(new Color(1f, 1f, 1f), 0.7f);
        for (int i = 0; i < cloudCount; i++)
        {
            var cl = new GameObject("Cloud" + i);
            cl.transform.SetParent(p, false);
            cl.transform.position = new Vector3(i * 80f - 30f, 38f + (i % 3) * 5f, 40f);
            for (int b = 0; b < 3; b++)
                Ball("Puff", new Vector3(b * 3f - 3f, (b % 2) * 0.7f, 0f), new Vector3(5f, 1.7f, 1.7f), cloudMat, cl.transform);
            var mover = cl.AddComponent<PropMover>();
            mover.axis = Vector3.right;
            mover.range = 10f;
            mover.speed = 0.4f + i * 0.15f;
        }
    }
}

// Moves props back and forth (traffic, clouds). Top-level class: Unity cannot
// attach nested MonoBehaviours.
public class PropMover : MonoBehaviour
{
    public Vector3 axis = Vector3.right;
    public float range = 20f;
    public float speed = 8f;
    Vector3 basePos;
    float phase;

    void Awake()
    {
        basePos = transform.position;
        phase = Random.value * 20f;
    }

    void Update()
    {
        if (GameManager.State != GameState.Playing) return;
        float t = Mathf.PingPong(Time.time * Mathf.Abs(speed) + phase, range * 2f) - range;
        if (speed < 0f) t = -t;
        transform.position = basePos + axis * t;
    }
}
