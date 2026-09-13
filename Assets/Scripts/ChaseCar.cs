using UnityEngine;

// Time-cop cruiser: pressures the player until the gap. It cannot follow
// across, so the jump is the escape. Touch the bike => BUSTED.
public class ChaseCar : MonoBehaviour
{
    public BikeController bike;

    GameObject lightR, lightB;
    float x = -25f;
    float speed;
    bool on;
    float flashT;

    public bool ChaseOn => on;

    public float Heat
    {
        get
        {
            if (!on || bike == null) return 0f;
            return Mathf.Clamp01(1f - Mathf.Abs(bike.transform.position.x - x) / 22f);
        }
    }

    void Awake()
    {
        Build();
        gameObject.SetActive(false);
    }

    static GameObject Part(PrimitiveType type, string name, Vector3 lp, Vector3 ls, Material mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        foreach (var c in go.GetComponents<Collider>()) Object.Destroy(c);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = lp;
        go.transform.localScale = ls;
        go.GetComponent<Renderer>().material = mat;
        return go;
    }

    void Build()
    {
        var navy = WorldBuilder.MakeMat(new Color(0.08f, 0.12f, 0.35f));
        var glass = WorldBuilder.MakeMat(new Color(0.60f, 0.80f, 0.95f));
        var tire = WorldBuilder.MakeMat(new Color(0.08f, 0.08f, 0.09f));
        var t = transform;
        Part(PrimitiveType.Cube, "Body", new Vector3(0f, 0.75f, 0f), new Vector3(2.6f, 0.7f, 1.5f), navy, t);
        Part(PrimitiveType.Cube, "Cabin", new Vector3(-0.2f, 1.30f, 0f), new Vector3(1.3f, 0.55f, 1.3f), glass, t);
        Part(PrimitiveType.Cube, "BarBase", new Vector3(-0.2f, 1.65f, 0f), new Vector3(1.0f, 0.12f, 0.4f), tire, t);
        lightR = Part(PrimitiveType.Cube, "LightR", new Vector3(-0.45f, 1.82f, 0f), new Vector3(0.4f, 0.2f, 0.42f),
            WorldBuilder.MakeGlow(Color.red, 3f), t);
        lightB = Part(PrimitiveType.Cube, "LightB", new Vector3(0.05f, 1.82f, 0f), new Vector3(0.4f, 0.2f, 0.42f),
            WorldBuilder.MakeGlow(Color.blue, 3f), t);
        var wheels = new Vector3[]
        {
            new Vector3(0.85f, 0.35f, 0.78f), new Vector3(0.85f, 0.35f, -0.78f),
            new Vector3(-0.85f, 0.35f, 0.78f), new Vector3(-0.85f, 0.35f, -0.78f),
        };
        for (int i = 0; i < wheels.Length; i++)
        {
            var w = Part(PrimitiveType.Cylinder, "Wheel" + i, wheels[i], new Vector3(0.7f, 0.2f, 0.7f), tire, t);
            w.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    public void BeginChase()
    {
        x = -25f;
        speed = 0f;
        on = true;
        gameObject.SetActive(true);
        AudioDirector.SetSiren(true);
    }

    public void EndChase()
    {
        on = false;
        gameObject.SetActive(false);
        AudioDirector.SetSiren(false);
    }

    void Update()
    {
        if (!on || GameManager.State != GameState.Playing || bike == null) return;
        float bikeX = bike.transform.position.x;
        // It will not follow across the gap: the jump is the escape.
        float targetX = Mathf.Min(bikeX - 4f, 73f);
        float want = Mathf.Clamp((targetX - x) * 1.6f, 0f, 29.5f);
        speed = Mathf.MoveTowards(speed, want, 22f * Time.deltaTime);
        x += speed * Time.deltaTime;
        float gy = WorldBuilder.GetGroundY(x);
        transform.position = new Vector3(x, (float.IsNaN(gy) ? 0f : gy) + 0.35f, -2.4f);

        flashT += Time.deltaTime * 7f;
        bool phase = (flashT % 1f) < 0.5f;
        lightR.SetActive(phase);
        lightB.SetActive(!phase);

        if (bikeX < 80f && Mathf.Abs(bikeX - x) < 2.6f)
        {
            GameManager.Instance.Busted();
            return;
        }
        if (bikeX > 82f) EndChase(); // lost him at the jump
    }
}
