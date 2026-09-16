using UnityEngine;

// LACKLUSTER VIDEO — the comedy home base. Storefront on the town strip at
// x≈-20 (before the ride start) plus an enterable interior: VHS shelves,
// counter with register, movie posters, a glowing TV, employee door.
// When on-foot Jack approaches the counter, the script's BTTF bit plays;
// afterwards Jack's shift ends and the ride begins.
public static class LacklusterVideo
{
    public static Vector3 InteriorSpawn { get; private set; }
    public static Vector3 StorefrontPos { get; private set; }

    static GameObject root;
    static GameObject frontGlass;
    static Material signMat;
    static Vector3 counterPos;
    static bool interiorMode;

    public static void Build(Transform parent)
    {
        root = new GameObject("LacklusterVideo");
        root.transform.SetParent(parent, false);

        InteriorSpawn = new Vector3(-26f, 0f, 0f);
        StorefrontPos = new Vector3(-20f, 0f, -4.5f);
        counterPos = new Vector3(-20f, 0f, 1.2f);
        interiorMode = false;

        var wallMat = WorldBuilder.MakeMat(new Color(0.85f, 0.82f, 0.75f));
        var trimMat = WorldBuilder.MakeMat(new Color(0.95f, 0.75f, 0.10f));
        var blueMat = WorldBuilder.MakeMat(new Color(0.10f, 0.25f, 0.70f));
        var floorMat = WorldBuilder.MakeMat(new Color(0.55f, 0.55f, 0.58f));
        var shelfMat = WorldBuilder.MakeMat(new Color(0.45f, 0.30f, 0.18f));
        var darkMat = WorldBuilder.MakeMat(new Color(0.12f, 0.12f, 0.14f));

        // Shell: floor, back + side walls, ceiling. Front stays open toward
        // the camera; the storefront facade + glass sit on the street face.
        Box("Floor", new Vector3(-20f, -0.10f, 0f), new Vector3(16f, 0.2f, 7f), floorMat);
        Box("BackWall", new Vector3(-20f, 3f, 3.4f), new Vector3(16f, 6f, 0.3f), wallMat);
        Box("WallL", new Vector3(-28f, 3f, 0f), new Vector3(0.3f, 6f, 7f), wallMat);
        Box("WallR", new Vector3(-12f, 3f, 0f), new Vector3(0.3f, 6f, 7f), wallMat);
        Box("Ceiling", new Vector3(-20f, 6f, 0f), new Vector3(16f, 0.3f, 7f), wallMat);
        // Yellow trim stripe along the facade.
        Box("Trim", new Vector3(-20f, 3.9f, -3.55f), new Vector3(16f, 0.25f, 0.15f), trimMat);

        // Facade + big buzzing sign (yellow on blue).
        Box("Facade", new Vector3(-20f, 5f, -3.4f), new Vector3(16f, 2.2f, 0.4f), blueMat);
        Box("SignBack", new Vector3(-20f, 5f, -3.62f), new Vector3(13f, 1.7f, 0.1f), blueMat);
        signMat = WorldBuilder.MakeGlow(new Color(1f, 0.85f, 0.15f), 1.6f);
        var sign = UIUtil.MakeWorldLabel("LACKLUSTER VIDEO", 0.32f, new Color(1f, 0.85f, 0.15f), 12f);
        sign.transform.SetParent(root.transform, false);
        sign.transform.localPosition = new Vector3(-20f, 5f, -3.70f);
        sign.GetComponent<Renderer>().material = signMat;

        // Glass storefront (semi-transparent); hidden while Jack is inside.
        var glassMat = WorldBuilder.MakeMat(new Color(0.70f, 0.85f, 0.95f), 0.25f);
        frontGlass = Box("GlassFront", new Vector3(-20f, 2f, -3.4f), new Vector3(15f, 4f, 0.1f), glassMat);
        Box("Doorway", new Vector3(-13.5f, 1.5f, -3.42f), new Vector3(2f, 3f, 0.12f), darkMat);

        // Interior: three shelf rows of VHS tapes.
        for (int r = 0; r < 3; r++)
            BuildShelf(new Vector3(-22f, 0f, -1.5f + r * 1.6f), shelfMat);

        // Counter + cash register with glowing screen.
        Box("Counter", new Vector3(-20f, 0.55f, 1.6f), new Vector3(4f, 1.1f, 1f), shelfMat);
        Box("Register", new Vector3(-20f, 1.28f, 1.6f), new Vector3(0.7f, 0.35f, 0.6f), darkMat);
        Box("RegScreen", new Vector3(-20f, 1.38f, 1.28f), new Vector3(0.5f, 0.3f, 0.05f),
            WorldBuilder.MakeGlow(new Color(0.4f, 1f, 0.5f), 1.5f));

        // Movie posters on the back wall.
        Poster("BACK TO THE FUTURE", new Vector3(-24f, 3.4f, 3.22f));
        Poster("JURASSIC PARK", new Vector3(-20f, 3.4f, 3.22f));
        Poster("CLERKS", new Vector3(-16f, 3.4f, 3.22f));

        // TV on a stand, glowing.
        Box("TVStand", new Vector3(-14.5f, 0.4f, 2.6f), new Vector3(1.2f, 0.8f, 0.6f), darkMat);
        Box("TV", new Vector3(-14.5f, 1.3f, 2.6f), new Vector3(1.6f, 1.1f, 0.5f), darkMat);
        Box("TVScreen", new Vector3(-14.5f, 1.3f, 2.32f), new Vector3(1.3f, 0.85f, 0.06f),
            WorldBuilder.MakeGlow(new Color(0.5f, 0.8f, 1f), 2f));

        // Employee door.
        Box("EmpDoor", new Vector3(-27.8f, 1.5f, 2.2f), new Vector3(0.15f, 3f, 1.4f), darkMat);
        var empLabel = UIUtil.MakeWorldLabel("EMPLOYEES ONLY", 0.12f, Color.white, 1.2f);
        empLabel.transform.SetParent(root.transform, false);
        empLabel.transform.localPosition = new Vector3(-27.68f, 2.4f, 2.2f);
        empLabel.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

        // Customer waiting at the counter.
        BuildCustomer(new Vector3(-20f, 0f, 0.1f));

        // Director: buzzing sign flicker + BTTF bit trigger + interior mode.
        var dir = root.AddComponent<VideoStoreDirector>();
        dir.signMat = signMat;
        dir.counterPos = counterPos;
    }

    // Hides the glass front while Jack is inside so the interior reads clearly.
    public static void SetInteriorMode(bool inside)
    {
        interiorMode = inside;
        if (frontGlass != null) frontGlass.SetActive(!inside);
    }

    // ---- builders ----

    static void NoColliders(GameObject go)
    {
        foreach (var c in go.GetComponents<Collider>()) Object.Destroy(c);
    }

    static GameObject Box(string name, Vector3 pos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        NoColliders(go);
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().material = mat;
        return go;
    }

    static void BuildShelf(Vector3 basePos, Material shelfMat)
    {
        Box("ShelfPostL", basePos + new Vector3(-3f, 1.2f, 0f), new Vector3(0.12f, 2.4f, 0.8f), shelfMat);
        Box("ShelfPostR", basePos + new Vector3(3f, 1.2f, 0f), new Vector3(0.12f, 2.4f, 0.8f), shelfMat);
        for (int lvl = 0; lvl < 3; lvl++)
        {
            float y = 0.6f + lvl * 0.9f;
            Box("ShelfPlank", basePos + new Vector3(0f, y, 0f), new Vector3(6f, 0.08f, 0.8f), shelfMat);
            for (int i = 0; i < 14; i++)
            {
                float x = basePos.x - 2.8f + i * 0.42f;
                var c = new Color(Random.value, Random.value * 0.9f, Random.value * 0.9f);
                Box("VHS", new Vector3(x, y + 0.33f, basePos.z), new Vector3(0.32f, 0.55f, 0.5f), WorldBuilder.MakeMat(c));
            }
        }
    }

    static void Poster(string title, Vector3 pos)
    {
        var paper = WorldBuilder.MakeMat(new Color(0.92f, 0.90f, 0.85f));
        Box("Poster_" + title, pos, new Vector3(2.6f, 1.8f, 0.06f), paper);
        var label = UIUtil.MakeWorldLabel(title, 0.14f, new Color(0.15f, 0.15f, 0.20f), 2.4f);
        label.transform.SetParent(root.transform, false);
        label.transform.localPosition = pos + new Vector3(0f, 0f, -0.05f);
    }

    static void BuildCustomer(Vector3 pos)
    {
        var coatMat = WorldBuilder.MakeMat(new Color(0.30f, 0.35f, 0.45f));
        var skinMat = WorldBuilder.MakeMat(new Color(0.90f, 0.70f, 0.55f));
        var g = new GameObject("Customer").transform;
        g.SetParent(root.transform, false);
        g.localPosition = pos;
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "CustomerBody";
        NoColliders(body);
        body.transform.SetParent(g, false);
        body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        body.transform.localScale = new Vector3(0.4f, 0.8f, 0.35f);
        body.GetComponent<Renderer>().material = coatMat;
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "CustomerHead";
        NoColliders(head);
        head.transform.SetParent(g, false);
        head.transform.localPosition = new Vector3(0f, 1.65f, 0f);
        head.transform.localScale = new Vector3(0.3f, 0.34f, 0.3f);
        head.GetComponent<Renderer>().material = skinMat;
    }

    // Per-frame director: sign buzz/flicker, interior-mode toggle, and the
    // BTTF bit trigger when Jack reaches the counter.
    class VideoStoreDirector : MonoBehaviour
    {
        public Material signMat;
        public Vector3 counterPos;
        float flickerT;
        bool bitPlayed;
        bool lastInteriorMode;

        void Update()
        {
            // Buzzing fluorescent sign: mostly steady, occasional dropouts.
            if (signMat != null)
            {
                flickerT += Time.deltaTime;
                float f = 0.85f + 0.15f * Mathf.Sin(flickerT * 30f) * Mathf.Sin(flickerT * 7.3f);
                if (Random.value < 0.02f) f *= 0.35f;
                var baseCol = new Color(1f, 0.85f, 0.15f);
                signMat.SetColor("_EmissionColor", baseCol * 1.6f * f);
                signMat.color = baseCol * (0.7f + 0.3f * f);
            }

            // Interior mode follows the game state automatically.
            bool wantInterior = GameManager.State == GameState.OnFoot;
            if (wantInterior != lastInteriorMode)
            {
                lastInteriorMode = wantInterior;
                SetInteriorMode(wantInterior);
            }

            // BTTF bit: customer at the counter, Jack walks up.
            if (!bitPlayed && GameManager.State == GameState.OnFoot
                && GameManager.Instance != null && GameManager.Instance.onFoot != null)
            {
                Vector3 p = GameManager.Instance.onFoot.transform.position;
                Vector3 d = p - counterPos;
                d.y = 0f;
                if (d.magnitude < 2.5f)
                {
                    bitPlayed = true;
                    CutsceneManager.Instance.Play("bttf_bit", OnBitDone);
                }
            }
        }

        void OnBitDone()
        {
            // Shift's over. Closing time, then the ride.
            if (GameManager.Instance != null) GameManager.Instance.EndShift();
        }
    }
}
