using UnityEngine;

// Slow, deliberate on-foot Jack for interiors (Lackluster Video).
// A/D or arrow keys walk left/right (~3 u/s with deliberate acceleration),
// W / Up / Space for a small hop. No bike physics. Only responds while
// GameManager.State == GameState.OnFoot.
public class OnFootController : MonoBehaviour
{
    public Vector3 vel;

    const float MaxSpeed = 3f;
    const float Accel = 14f;
    const float HopSpeed = 5.2f;
    const float Gravity = -20f;

    float floorY;
    bool grounded = true;
    Transform figure;
    float bobT;

    // Teleport to a position (used when entering the video store).
    public void Place(Vector3 pos)
    {
        transform.position = pos;
        floorY = pos.y;
        vel = Vector3.zero;
        grounded = true;
    }

    void Awake()
    {
        BuildFigure();
    }

    static void NoColliders(GameObject go)
    {
        foreach (var c in go.GetComponents<Collider>()) Object.Destroy(c);
    }

    static GameObject Part(PrimitiveType type, string name, Vector3 localPos, Vector3 localScale, Material mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        NoColliders(go);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        go.GetComponent<Renderer>().material = mat;
        return go;
    }

    // Minimal standing Jack: capsule torso, sphere head + helmet, simple limbs.
    void BuildFigure()
    {
        figure = new GameObject("Figure").transform;
        figure.SetParent(transform, false);

        var jacketMat = WorldBuilder.MakeMat(new Color(0.72f, 0.22f, 0.14f));
        var denimMat = WorldBuilder.MakeMat(new Color(0.20f, 0.30f, 0.58f));
        var skinMat = WorldBuilder.MakeMat(new Color(0.95f, 0.76f, 0.60f));
        var helmetMat = WorldBuilder.MakeMat(new Color(0.92f, 0.92f, 0.95f));

        Part(PrimitiveType.Cylinder, "LegL", new Vector3(0f, 0.25f, 0.08f), new Vector3(0.14f, 0.50f, 0.14f), denimMat, figure);
        Part(PrimitiveType.Cylinder, "LegR", new Vector3(0f, 0.25f, -0.08f), new Vector3(0.14f, 0.50f, 0.14f), denimMat, figure);
        Part(PrimitiveType.Capsule, "Torso", new Vector3(0f, 0.85f, 0f), new Vector3(0.34f, 0.70f, 0.30f), jacketMat, figure);
        Part(PrimitiveType.Cylinder, "ArmL", new Vector3(0f, 0.95f, 0.22f), new Vector3(0.11f, 0.55f, 0.11f), jacketMat, figure);
        Part(PrimitiveType.Cylinder, "ArmR", new Vector3(0f, 0.95f, -0.22f), new Vector3(0.11f, 0.55f, 0.11f), jacketMat, figure);
        Part(PrimitiveType.Sphere, "Head", new Vector3(0f, 1.45f, 0f), new Vector3(0.28f, 0.32f, 0.28f), skinMat, figure);
        Part(PrimitiveType.Sphere, "Helmet", new Vector3(0f, 1.49f, 0f), new Vector3(0.34f, 0.32f, 0.34f), helmetMat, figure);
    }

    void Update()
    {
        if (GameManager.State != GameState.OnFoot) return;
        float dt = Time.deltaTime;

        float dir = 0f;
        if (KeyPoll.Held(KeyCode.A) || KeyPoll.Held(KeyCode.LeftArrow)) dir -= 1f;
        if (KeyPoll.Held(KeyCode.D) || KeyPoll.Held(KeyCode.RightArrow)) dir += 1f;

        // Deliberate acceleration toward the target walk speed.
        vel.x = Mathf.MoveTowards(vel.x, dir * MaxSpeed, Accel * dt);

        // Face the travel direction.
        if (Mathf.Abs(vel.x) > 0.1f)
            figure.localScale = new Vector3(Mathf.Sign(vel.x), 1f, 1f);

        // Small hop.
        if (grounded && (KeyPoll.Down(KeyCode.W) || KeyPoll.Down(KeyCode.UpArrow) || KeyPoll.Down(KeyCode.Space)))
        {
            vel.y = HopSpeed;
            grounded = false;
        }
        vel.y += Gravity * dt;

        Vector3 p = transform.position + vel * dt;
        if (p.y <= floorY)
        {
            p.y = floorY;
            vel.y = 0f;
            grounded = true;
        }
        transform.position = p;

        // Subtle walk bob.
        if (grounded && Mathf.Abs(vel.x) > 0.2f)
        {
            bobT += dt * 10f;
            figure.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(bobT)) * 0.05f, 0f);
        }
        else
        {
            figure.localPosition = Vector3.zero;
        }
    }
}
