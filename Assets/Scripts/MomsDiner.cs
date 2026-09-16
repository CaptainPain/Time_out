using UnityEngine;

// MOM'S — the drama home, a fixed point across all three timelines.
// Boxy diner at x≈30 on the town strip: large street windows with warm
// light, counter + stools + coffee machine inside, Jack and Mom visible
// through the glass. ApplyTimeline swaps the skin: 1999 warm diner /
// medieval tavern hall / future noodle bar. A pendant-shaped hint hangs
// on the wall behind the counter in every timeline — unexplained.
public static class MomsDiner
{
    public static Vector3 DinerPos { get; private set; }

    static GameObject root;
    static GameObject[] skins = new GameObject[3];

    public static void Build(Transform parent)
    {
        root = new GameObject("MomsDiner");
        root.transform.SetParent(parent, false);
        DinerPos = new Vector3(31f, 0f, 0f);

        var wallMat = WorldBuilder.MakeMat(new Color(0.88f, 0.84f, 0.78f));
        var roofMat = WorldBuilder.MakeMat(new Color(0.55f, 0.15f, 0.15f));
        var frameMat = WorldBuilder.MakeMat(new Color(0.25f, 0.25f, 0.28f));
        var counterMat = WorldBuilder.MakeMat(new Color(0.60f, 0.42f, 0.28f));
        var chromeMat = WorldBuilder.MakeMat(new Color(0.75f, 0.78f, 0.82f));

        // Cutaway shell: back box + side walls + roof; street face holds the
        // window frames so the interior reads through them from the camera.
        Box("DinerBack", DinerPos + new Vector3(0f, 2.5f, 1.5f), new Vector3(14f, 5f, 3f), wallMat);
        Box("DinerSideL", new Vector3(24f, 2.5f, -1.5f), new Vector3(0.3f, 5f, 3f), wallMat);
        Box("DinerSideR", new Vector3(38f, 2.5f, -1.5f), new Vector3(0.3f, 5f, 3f), wallMat);
        Box("DinerFloor", DinerPos + new Vector3(0f, -0.05f, -0.5f), new Vector3(14f, 0.1f, 5f), wallMat);
        Box("DinerRoof", DinerPos + new Vector3(0f, 5.3f, 0f), new Vector3(15f, 0.6f, 7f), roofMat);

        // Window band on the street face: four frames, faint glass.
        var glassMat = WorldBuilder.MakeMat(new Color(0.80f, 0.90f, 1f), 0.12f);
        for (int i = 0; i < 4; i++)
        {
            float x = DinerPos.x - 5.25f + i * 3.5f;
            Box("WinFrame" + i, new Vector3(x, 2.4f, -3.02f), new Vector3(3f, 2.6f, 0.12f), frameMat);
            Box("WinGlass" + i, new Vector3(x, 2.4f, -3.02f), new Vector3(2.7f, 2.3f, 0.06f), glassMat);
        }
        // Door on the left face.
        Box("DinerDoor", new Vector3(23.95f, 1.5f, 1f), new Vector3(0.12f, 3f, 1.6f), frameMat);

        // Shared interior: long counter, stools, coffee machine.
        Box("Counter", DinerPos + new Vector3(0f, 0.55f, 0.8f), new Vector3(8f, 1.1f, 1f), counterMat);
        for (int i = 0; i < 4; i++)
        {
            float x = DinerPos.x - 3f + i * 2f;
            var seat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            seat.name = "StoolSeat" + i;
            NoColliders(seat);
            seat.transform.SetParent(root.transform, false);
            seat.transform.localPosition = new Vector3(x, 0.65f, -0.6f);
            seat.transform.localScale = new Vector3(0.5f, 0.12f, 0.5f);
            seat.GetComponent<Renderer>().material = chromeMat;
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "StoolPole" + i;
            NoColliders(pole);
            pole.transform.SetParent(root.transform, false);
            pole.transform.localPosition = new Vector3(x, 0.32f, -0.6f);
            pole.transform.localScale = new Vector3(0.08f, 0.6f, 0.08f);
            pole.GetComponent<Renderer>().material = frameMat;
        }
        Box("CoffeeMachine", DinerPos + new Vector3(2.5f, 1.6f, 1.9f), new Vector3(1f, 1.2f, 0.8f), frameMat);
        Box("CoffeeGlow", DinerPos + new Vector3(2.5f, 1.7f, 1.48f), new Vector3(0.7f, 0.15f, 0.06f),
            WorldBuilder.MakeGlow(new Color(1f, 0.6f, 0.2f), 1.8f));

        // The window shot: Jack seated at the counter, Mom behind it.
        BuildPerson(DinerPos + new Vector3(-1f, 0f, -0.6f), true,
            new Color(0.72f, 0.22f, 0.14f), new Color(0.95f, 0.76f, 0.60f), "JackSeated");
        BuildPerson(DinerPos + new Vector3(0.5f, 0f, 1.9f), false,
            new Color(0.35f, 0.45f, 0.65f), new Color(0.95f, 0.78f, 0.62f), "Mom");

        // The three timeline skins.
        skins[0] = BuildPresentSkin();
        skins[1] = BuildMedievalSkin();
        skins[2] = BuildFutureSkin();
        ApplyTimeline(Timeline.Present);

        // Re-skin automatically whenever Jack shifts.
        TimelineManager.OnShift += ApplyTimeline;
    }

    // Swaps the visible skin: 1999 warm diner / medieval tavern / future noodle bar.
    public static void ApplyTimeline(Timeline t)
    {
        for (int i = 0; i < 3; i++)
            if (skins[i] != null) skins[i].SetActive(i == (int)t);
    }

    // The script's CINEMATIC 02 window shot: camera stays outside, Jack and
    // Mom visible through the glass, soft clean guitar, no audible dialogue.
    public static void PlayWindowScene()
    {
        if (CutsceneManager.Instance != null)
            CutsceneManager.Instance.Play("mom_window", null);
    }

    // ---- shared builders ----

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

    static GameObject SkinBox(string name, Vector3 pos, Vector3 size, Material mat, GameObject skin)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        NoColliders(go);
        go.transform.SetParent(skin.transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().material = mat;
        return go;
    }

    static void SkinLabel(GameObject skin, string text, Vector3 pos, float charSize, Color color, float maxWidth)
    {
        var label = UIUtil.MakeWorldLabel(text, charSize, color, maxWidth);
        label.transform.SetParent(skin.transform, false);
        label.transform.localPosition = pos;
    }

    // Simple standing/seated figure for the window shot.
    static void BuildPerson(Vector3 pos, bool seated, Color clothes, Color skin, string name)
    {
        var g = new GameObject(name).transform;
        g.SetParent(root.transform, false);
        g.localPosition = pos;
        var clothesMat = WorldBuilder.MakeMat(clothes);
        var skinMat = WorldBuilder.MakeMat(skin);
        float hipY = seated ? 0.72f : 0.85f;
        if (!seated)
        {
            foreach (float z in new float[] { 0.09f, -0.09f })
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = name + "Leg";
                NoColliders(leg);
                leg.transform.SetParent(g, false);
                leg.transform.localPosition = new Vector3(0f, 0.4f, z);
                leg.transform.localScale = new Vector3(0.13f, 0.8f, 0.13f);
                leg.GetComponent<Renderer>().material = clothesMat;
            }
        }
        var torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        torso.name = name + "Torso";
        NoColliders(torso);
        torso.transform.SetParent(g, false);
        torso.transform.localPosition = new Vector3(0f, hipY + 0.35f, 0f);
        torso.transform.localScale = new Vector3(0.36f, 0.7f, 0.32f);
        torso.GetComponent<Renderer>().material = clothesMat;
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = name + "Head";
        NoColliders(head);
        head.transform.SetParent(g, false);
        head.transform.localPosition = new Vector3(0f, hipY + 0.95f, 0f);
        head.transform.localScale = new Vector3(0.3f, 0.34f, 0.3f);
        head.GetComponent<Renderer>().material = skinMat;
    }

    // ---- timeline skins ----

    static GameObject NewSkin(string name)
    {
        var s = new GameObject(name);
        s.transform.SetParent(root.transform, false);
        return s;
    }

    // Back-wall light wash so the interior glows through the windows.
    static void Backlight(GameObject skin, Color c, float intensity)
    {
        SkinBox("Backlight", DinerPos + new Vector3(0f, 2.5f, 2.55f), new Vector3(11f, 3.6f, 0.1f),
            WorldBuilder.MakeGlow(c, intensity), skin);
    }

    static GameObject BuildPresentSkin()
    {
        var s = NewSkin("Skin_Present");
        Backlight(s, new Color(1f, 0.72f, 0.42f), 1.4f);
        // Neon sign.
        SkinBox("SignBack", DinerPos + new Vector3(0f, 6.4f, -3.25f), new Vector3(7f, 1.6f, 0.15f),
            WorldBuilder.MakeMat(new Color(0.12f, 0.10f, 0.14f)), s);
        var signMat = WorldBuilder.MakeGlow(new Color(1f, 0.30f, 0.45f), 2f);
        var label = UIUtil.MakeWorldLabel("MOM'S", 0.55f, new Color(1f, 0.45f, 0.60f), 6f);
        label.transform.SetParent(s.transform, false);
        label.transform.localPosition = DinerPos + new Vector3(0f, 6.4f, -3.36f);
        label.GetComponent<Renderer>().material = signMat;
        // Hanging pendant lamps.
        foreach (float x in new float[] { -3f, 0f, 3f })
        {
            var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = "Lamp";
            NoColliders(lamp);
            lamp.transform.SetParent(s.transform, false);
            lamp.transform.localPosition = DinerPos + new Vector3(x, 4f, 0f);
            lamp.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            lamp.GetComponent<Renderer>().material = WorldBuilder.MakeGlow(new Color(1f, 0.8f, 0.5f), 2f);
        }
        // Pendant hint: framed photo, a small pendant glowing faintly inside.
        Vector3 spot = DinerPos + new Vector3(0.5f, 3.3f, 2.45f);
        SkinBox("PhotoFrame", spot, new Vector3(0.9f, 1.1f, 0.08f),
            WorldBuilder.MakeMat(new Color(0.30f, 0.20f, 0.12f)), s);
        var pendant = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pendant.name = "PendantHint";
        NoColliders(pendant);
        pendant.transform.SetParent(s.transform, false);
        pendant.transform.localPosition = spot + new Vector3(0f, -0.05f, -0.06f);
        pendant.transform.localScale = new Vector3(0.16f, 0.24f, 0.06f);
        pendant.GetComponent<Renderer>().material = WorldBuilder.MakeGlow(new Color(0.95f, 0.75f, 0.35f), 1.2f);
        return s;
    }

    static GameObject BuildMedievalSkin()
    {
        var s = NewSkin("Skin_Medieval");
        Backlight(s, new Color(1f, 0.52f, 0.20f), 1.3f);
        // Timber beams + thatch roof over the diner shell.
        var timberMat = WorldBuilder.MakeMat(new Color(0.25f, 0.16f, 0.10f));
        var thatchMat = WorldBuilder.MakeMat(new Color(0.72f, 0.58f, 0.32f));
        SkinBox("Thatch", DinerPos + new Vector3(0f, 5.85f, 0f), new Vector3(15.5f, 0.5f, 7.5f), thatchMat, s);
        foreach (float x in new float[] { -4.5f, 0f, 4.5f })
            SkinBox("BeamV" + x, DinerPos + new Vector3(x, 2.5f, -3.12f), new Vector3(0.35f, 5f, 0.1f), timberMat, s);
        SkinBox("BeamH", DinerPos + new Vector3(0f, 4.6f, -3.12f), new Vector3(14f, 0.35f, 0.1f), timberMat, s);
        // Wooden tavern sign.
        SkinBox("SignBack", DinerPos + new Vector3(0f, 6.4f, -3.25f), new Vector3(9f, 1.6f, 0.15f), timberMat, s);
        SkinLabel(s, "MOMM'S TAVERNE", DinerPos + new Vector3(0f, 6.4f, -3.36f), 0.32f,
            new Color(1f, 0.85f, 0.45f), 8.4f);
        // Candles on the counter.
        foreach (float x in new float[] { -2f, 1f })
        {
            SkinBox("Candle" + x, DinerPos + new Vector3(x, 1.35f, 0.8f), new Vector3(0.12f, 0.35f, 0.12f),
                WorldBuilder.MakeMat(new Color(0.92f, 0.88f, 0.78f)), s);
            var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "Flame" + x;
            NoColliders(flame);
            flame.transform.SetParent(s.transform, false);
            flame.transform.localPosition = DinerPos + new Vector3(x, 1.6f, 0.8f);
            flame.transform.localScale = new Vector3(0.1f, 0.16f, 0.1f);
            flame.GetComponent<Renderer>().material = WorldBuilder.MakeGlow(new Color(1f, 0.6f, 0.15f), 2.5f);
        }
        // Pendant hint: a carved wooden pendant in a rough frame — same spot.
        Vector3 spot = DinerPos + new Vector3(0.5f, 3.3f, 2.45f);
        SkinBox("CarveFrame", spot, new Vector3(0.9f, 1.1f, 0.08f), timberMat, s);
        var carve = GameObject.CreatePrimitive(PrimitiveType.Cube);
        carve.name = "PendantHint";
        NoColliders(carve);
        carve.transform.SetParent(s.transform, false);
        carve.transform.localPosition = spot + new Vector3(0f, -0.05f, -0.06f);
        carve.transform.localScale = new Vector3(0.18f, 0.26f, 0.06f);
        carve.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        carve.GetComponent<Renderer>().material = WorldBuilder.MakeMat(new Color(0.35f, 0.22f, 0.12f));
        return s;
    }

    static GameObject BuildFutureSkin()
    {
        var s = NewSkin("Skin_Future");
        Backlight(s, new Color(0.35f, 0.75f, 1f), 1.3f);
        // Chrome panels on the counter front + neon strips.
        var chromeMat = WorldBuilder.MakeMat(new Color(0.80f, 0.83f, 0.88f));
        SkinBox("ChromePanel", DinerPos + new Vector3(0f, 0.55f, 0.26f), new Vector3(8f, 0.9f, 0.08f), chromeMat, s);
        SkinBox("NeonPink", DinerPos + new Vector3(0f, 5.0f, -3.55f), new Vector3(14f, 0.12f, 0.12f),
            WorldBuilder.MakeGlow(new Color(1f, 0.25f, 0.65f), 2.5f), s);
        SkinBox("NeonCyan", DinerPos + new Vector3(0f, 0.12f, -3.55f), new Vector3(14f, 0.12f, 0.12f),
            WorldBuilder.MakeGlow(new Color(0.25f, 0.85f, 1f), 2.5f), s);
        // Holographic menu board.
        SkinBox("HoloMenu", DinerPos + new Vector3(-3f, 3.4f, 2.45f), new Vector3(2.4f, 1.4f, 0.06f),
            WorldBuilder.MakeGlow(new Color(0.3f, 0.8f, 1f), 1.1f), s);
        // Sign.
        SkinBox("SignBack", DinerPos + new Vector3(0f, 6.4f, -3.25f), new Vector3(11f, 1.6f, 0.15f),
            WorldBuilder.MakeMat(new Color(0.08f, 0.10f, 0.16f)), s);
        var signMat = WorldBuilder.MakeGlow(new Color(0.35f, 0.9f, 1f), 2f);
        var label = UIUtil.MakeWorldLabel("MOM'S NOODLE BAR", 0.34f, new Color(0.5f, 0.95f, 1f), 10f);
        label.transform.SetParent(s.transform, false);
        label.transform.localPosition = DinerPos + new Vector3(0f, 6.4f, -3.36f);
        label.GetComponent<Renderer>().material = signMat;
        // Pendant hint: holographic pendant, floating — same spot.
        Vector3 spot = DinerPos + new Vector3(0.5f, 3.45f, 2.45f);
        SkinBox("HoloBack", spot, new Vector3(0.9f, 1.1f, 0.06f),
            WorldBuilder.MakeGlow(new Color(0.2f, 0.5f, 0.8f), 0.7f), s);
        var holo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        holo.name = "PendantHint";
        NoColliders(holo);
        holo.transform.SetParent(s.transform, false);
        holo.transform.localPosition = spot + new Vector3(0f, -0.05f, -0.08f);
        holo.transform.localScale = new Vector3(0.18f, 0.26f, 0.06f);
        holo.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        holo.GetComponent<Renderer>().material = WorldBuilder.MakeGlow(new Color(0.4f, 0.95f, 1f), 2f);
        return s;
    }
}
