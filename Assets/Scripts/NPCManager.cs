using UnityEngine;

// Ambient townsfolk with comedy bits. Pure decoration: NPCs never have
// colliders, never block the bike, they just live their little lives while
// Jack tears past. Two scripted bits (the arguers, the documentarian) plus
// generic townsfolk who step back, stare, and shout when the bike gets close.
public class NPCManager : MonoBehaviour
{
    static GameObject root;
    static NPC[] npcs;
    static float time;
    static int arguerLine;      // which line of the argument is showing
    static float arguerTimer;
    static int docuLine;        // which documentary line is showing
    static float docuTimer;

    const float ReactDist = 8f;
    const float StareDist = 14f;

    class NPC
    {
        public GameObject go;
        public Transform pivot;      // yaw-rotated to stare; bubble is NOT a child of this
        public Renderer bodyRenderer;
        public Renderer headRenderer;
        public GameObject cameraProp; // cameraman only
        public TextMesh bubble;
        public float bubbleTimer;
        public float baseX, baseZ, baseYaw;
        public float pacePhase, paceRange, paceSpeed;
        public bool pacer;
        public string role;          // "arguerA", "arguerB", "camera", "town"
        public int skinVariant;
        public float shoutCooldown;
        public float hopT;           // 0 = home, 1 = hopped back
        public float bobPhase;
    }

    static readonly string[] Shouts = {
        "MY INSURANCE!", "Is that legal?!", "Mom said don't stare...",
        "WITNESS ME!", "He's doing the thing again!"
    };

    static readonly string[] ArguerScript = {
        "He's gonna do it.", "He's not gonna do it.",
        "He's DOING it—", "...oh. He's just parking."
    };

    static readonly string[] DocuScript = {
        "Here we see the wild Jack...",
        "...in his natural habitat.",
        "Notice the pendant. Magnificent.",
        "He appears to be... showing off again.",
        "The townsfolk pretend not to watch."
    };

    // ---------------- build ----------------

    public static void Build(Transform parent)
    {
        if (root != null) Object.Destroy(root);
        root = new GameObject("NPCs");
        root.transform.SetParent(parent, false);
        time = 0f;
        arguerLine = 0; arguerTimer = 1.5f;
        docuLine = 0; docuTimer = 2f;

        npcs = new NPC[6];
        // Bit 1: the arguers, posted up near the jump approach, facing each other.
        npcs[0] = MakeNPC("ArguerA", new Vector3(50f, 0f, -3.4f), 0, "arguerA", false);
        npcs[1] = MakeNPC("ArguerB", new Vector3(52f, 0f, -3.4f), 1, "arguerB", false);
        // Bit 2: the documentarian with his camera.
        npcs[2] = MakeNPC("Cameraman", new Vector3(38f, 0f, 3.6f), 2, "camera", false);
        GiveCamera(npcs[2]);
        // Generic townsfolk: pacers on the strip.
        npcs[3] = MakeNPC("Townie0", new Vector3(14f, 0f, 4.2f), 3, "town", true);
        npcs[4] = MakeNPC("Townie1", new Vector3(26f, 0f, -4.0f), 4, "town", true);
        npcs[5] = MakeNPC("Townie2", new Vector3(44f, 0f, 4.4f), 5, "town", true);

        ApplyTimeline(TimelineManager.Active);
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

    static NPC MakeNPC(string name, Vector3 pos, int variant, string role, bool pacer)
    {
        var npc = new NPC();
        npc.role = role;
        npc.skinVariant = variant;
        npc.pacer = pacer;
        npc.baseX = pos.x; npc.baseZ = pos.z;
        npc.baseYaw = pos.z >= 0f ? 180f : 0f; // face the lane by default
        npc.pacePhase = variant * 1.7f;
        npc.paceRange = 2.5f;
        npc.paceSpeed = 0.5f + variant * 0.07f;
        npc.bobPhase = variant * 2.3f;

        npc.go = new GameObject(name);
        npc.go.transform.SetParent(root.transform, false);
        npc.go.transform.position = pos;

        npc.pivot = new GameObject("Pivot").transform;
        npc.pivot.SetParent(npc.go.transform, false);
        npc.pivot.rotation = Quaternion.Euler(0f, npc.baseYaw, 0f);

        var bodyGo = Part(PrimitiveType.Capsule, "Body", new Vector3(0f, 0.85f, 0f),
            new Vector3(0.55f, 1.1f, 0.55f), WorldBuilder.MakeMat(Color.gray), npc.pivot);
        npc.bodyRenderer = bodyGo.GetComponent<Renderer>();
        var headGo = Part(PrimitiveType.Sphere, "Head", new Vector3(0f, 1.62f, 0f),
            new Vector3(0.34f, 0.38f, 0.34f), WorldBuilder.MakeMat(new Color(0.95f, 0.76f, 0.60f)), npc.pivot);
        npc.headRenderer = headGo.GetComponent<Renderer>();

        // Speech bubble: child of the root (NOT the pivot) so it never
        // rotates away when the NPC turns to stare.
        npc.bubble = UIUtil.MakeWorldLabel("", 0.055f, new Color(1f, 0.95f, 0.6f), 4.5f);
        npc.bubble.transform.SetParent(npc.go.transform, false);
        npc.bubble.transform.localPosition = new Vector3(0f, 2.35f, 0f);
        npc.bubble.gameObject.SetActive(false);

        return npc;
    }

    static void GiveCamera(NPC npc)
    {
        // Chunky 1999 camcorder held up at chest height, pointing +z of the pivot.
        npc.cameraProp = Part(PrimitiveType.Cube, "Camera",
            new Vector3(0f, 1.15f, 0.42f), new Vector3(0.22f, 0.22f, 0.42f),
            WorldBuilder.MakeMat(new Color(0.12f, 0.12f, 0.14f)), npc.pivot);
        Part(PrimitiveType.Cylinder, "Lens",
            new Vector3(0f, 1.15f, 0.66f), new Vector3(0.12f, 0.10f, 0.12f),
            WorldBuilder.MakeGlow(new Color(0.2f, 0.5f, 1f), 1.2f), npc.pivot)
            .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    // ---------------- timeline skins ----------------

    public static void ApplyTimeline(Timeline t)
    {
        if (npcs == null) return;
        Color[] presentClothes = {
            new Color(0.20f, 0.30f, 0.58f), new Color(0.72f, 0.22f, 0.14f),
            new Color(0.25f, 0.55f, 0.30f), new Color(0.55f, 0.30f, 0.65f),
            new Color(0.85f, 0.55f, 0.15f), new Color(0.15f, 0.55f, 0.60f)
        };
        Color[] medievalClothes = {
            new Color(0.45f, 0.32f, 0.20f), new Color(0.30f, 0.38f, 0.22f),
            new Color(0.50f, 0.48f, 0.45f), new Color(0.35f, 0.25f, 0.18f),
            new Color(0.42f, 0.40f, 0.28f), new Color(0.28f, 0.32f, 0.28f)
        };
        Color[] futureClothes = {
            new Color(0.10f, 0.85f, 0.95f), new Color(0.95f, 0.15f, 0.75f),
            new Color(0.95f, 0.85f, 0.15f), new Color(0.45f, 0.15f, 0.95f),
            new Color(0.15f, 0.95f, 0.45f), new Color(0.95f, 0.35f, 0.15f)
        };
        foreach (var npc in npcs)
        {
            if (npc == null || npc.bodyRenderer == null) continue;
            Color c = t == Timeline.Present ? presentClothes[npc.skinVariant % presentClothes.Length]
                : t == Timeline.Medieval ? medievalClothes[npc.skinVariant % medievalClothes.Length]
                : futureClothes[npc.skinVariant % futureClothes.Length];
            npc.bodyRenderer.material = t == Timeline.Future
                ? WorldBuilder.MakeGlow(c, 1.1f)
                : WorldBuilder.MakeMat(c);
            // Camcorder becomes a sketchbook / a hover-drone lens per era.
            if (npc.cameraProp != null)
            {
                var cr = npc.cameraProp.GetComponent<Renderer>();
                if (t == Timeline.Medieval) cr.material = WorldBuilder.MakeMat(new Color(0.45f, 0.30f, 0.18f));
                else if (t == Timeline.Future) cr.material = WorldBuilder.MakeGlow(new Color(0.15f, 0.80f, 0.95f), 1.4f);
                else cr.material = WorldBuilder.MakeMat(new Color(0.12f, 0.12f, 0.14f));
            }
        }
    }

    // ---------------- per-frame ----------------

    static void ShowBubble(NPC npc, string text, float duration)
    {
        if (npc == null || npc.bubble == null) return;
        npc.bubble.text = text;
        npc.bubble.gameObject.SetActive(true);
        npc.bubbleTimer = duration;
    }

    public void Tick(Vector3 playerPos)
    {
        if (npcs == null) return;
        time += Time.deltaTime;

        // Bit 1: the argument loops forever.
        arguerTimer -= Time.deltaTime;
        if (arguerTimer <= 0f)
        {
            NPC speaker = npcs[arguerLine % 2 == 0 ? 0 : 1];
            ShowBubble(speaker, ArguerScript[arguerLine % ArguerScript.Length], 3.8f);
            arguerLine++;
            arguerTimer = arguerLine % ArguerScript.Length == 0 ? 6f : 4.2f;
        }

        // Bit 2: the documentary only narrates when Jack is in range.
        NPC cam = npcs[2];
        float camDist = Vector3.Distance(cam.go.transform.position, playerPos);
        if (camDist < 24f)
        {
            docuTimer -= Time.deltaTime;
            if (docuTimer <= 0f)
            {
                ShowBubble(cam, DocuScript[docuLine % DocuScript.Length], 4.4f);
                docuLine++;
                docuTimer = 5.2f;
            }
        }

        foreach (var npc in npcs)
        {
            if (npc == null) continue;
            Vector3 p = npc.go.transform.position;
            float dist = Vector3.Distance(p, playerPos);
            bool reacting = dist < ReactDist;

            // Bubble expiry.
            if (npc.bubbleTimer > 0f)
            {
                npc.bubbleTimer -= Time.deltaTime;
                if (npc.bubbleTimer <= 0f && npc.bubble != null)
                    npc.bubble.gameObject.SetActive(false);
            }

            // Hop back when the bike screams past; ease home when it leaves.
            float zSign = npc.baseZ >= 0f ? 1f : -1f;
            float targetHop = (reacting && Mathf.Abs(playerPos.z - npc.baseZ) < 3f) ? 1f : 0f;
            npc.hopT = Mathf.MoveTowards(npc.hopT, targetHop, Time.deltaTime * 3f);
            float bob = Mathf.Sin(time * 2.2f + npc.bobPhase) * 0.03f;

            if (npc.pacer && !reacting)
                p.x = npc.baseX + Mathf.Sin(time * npc.paceSpeed + npc.pacePhase) * npc.paceRange;
            p.z = npc.baseZ + zSign * npc.hopT * 1.3f;
            p.y = bob;
            npc.go.transform.position = p;

            // Stare at the bike when it's near; otherwise face the lane.
            float targetYaw = npc.baseYaw;
            if (dist < StareDist)
            {
                Vector3 toBike = playerPos - p;
                targetYaw = Mathf.Atan2(toBike.x, toBike.z) * Mathf.Rad2Deg;
            }
            float cur = npc.pivot.rotation.eulerAngles.y;
            float ny = Mathf.LerpAngle(cur > 180f ? cur - 360f : cur, targetYaw, 1f - Mathf.Exp(-6f * Time.deltaTime));
            npc.pivot.rotation = Quaternion.Euler(0f, ny, 0f);

            // Shout something when the bike gets close (cooldown per NPC).
            npc.shoutCooldown -= Time.deltaTime;
            if (reacting && npc.shoutCooldown <= 0f)
            {
                string line;
                if (npc.role == "arguerA") line = "SEE?! I TOLD YOU!";
                else if (npc.role == "arguerB") line = "THAT DOESN'T PROVE ANYTHING!";
                else line = Shouts[Random.Range(0, Shouts.Length)];
                ShowBubble(npc, line, 2.6f);
                npc.shoutCooldown = 9f + npc.skinVariant;
            }
        }
    }
}
