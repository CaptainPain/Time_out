using UnityEngine;

// Custom 2D bike physics on the shared terrain heightfield. No rigidbodies:
// full control over launch, air rotation, and landing rules (the skills the
// whole game is built on).
[RequireComponent(typeof(BikeVisual))]
public class BikeController : MonoBehaviour
{
    public Vector2 vel;
    public float pitch;       // radians; 0 = level, + = nose up
    public bool grounded = true;
    public bool wheelie;
    public Vector3 checkpoint;

    const float RideHeight = 0.55f;
    const float EngineAccel = 18f;
    const float BrakeDecel = 30f;
    const float MaxSpeed = 33f;
    const float MaxReverse = 7f;
    const float Gravity = 24f;
    const float AirDrag = 0.03f;
    const float RotateSpeed = 2.8f;   // rad/s of air pitch control
    const float MaxLandFall = 27f;    // fastest survivable vertical landing
    const float MaxLandTilt = 0.9f;   // radians of pitch error at landing

    BikeVisual visual;
    float checkpointTimer;
    float wheelieBonus;

    void Awake()
    {
        visual = GetComponent<BikeVisual>();
        checkpoint = new Vector3(WorldBuilder.StartX, 1f, 0f);
    }

    public void Respawn(Vector3 pos)
    {
        transform.position = pos;
        transform.rotation = Quaternion.identity;
        vel = Vector2.zero;
        pitch = 0f;
        grounded = true;
        wheelie = false;
        checkpoint = pos;
        checkpointTimer = 0f;
    }

    public float SpeedKmh => vel.magnitude * 3.6f;

    static float WrapAngle(float a)
    {
        while (a > Mathf.PI) a -= Mathf.PI * 2f;
        while (a < -Mathf.PI) a += Mathf.PI * 2f;
        return a;
    }

    void Update()
    {
        if (GameManager.State != GameState.Playing) return;
        float dt = Time.deltaTime;

        bool gas = KeyPoll.Held(KeyCode.W) || KeyPoll.Held(KeyCode.UpArrow) || InputState.gas;
        bool brake = KeyPoll.Held(KeyCode.S) || KeyPoll.Held(KeyCode.DownArrow) || InputState.brake;
        bool leanB = KeyPoll.Held(KeyCode.A) || KeyPoll.Held(KeyCode.LeftArrow) || InputState.leanBack;
        bool leanF = KeyPoll.Held(KeyCode.D) || KeyPoll.Held(KeyCode.RightArrow) || InputState.leanFwd;
        if (KeyPoll.Down(KeyCode.Space) && !TimelineManager.TryShift()) AudioDirector.PlayDenied();

        Vector3 p = transform.position;

        if (grounded)
        {
            float gy = WorldBuilder.GetGroundY(p.x);
            if (float.IsNaN(gy))
            {
                grounded = false; // rode off an edge
            }
            else
            {
                float slope = WorldBuilder.GetSlope(p.x);
                float slopeAng = Mathf.Atan(slope);
                float n = Mathf.Sqrt(1f + slope * slope);
                float dirX = 1f / n, dirY = slope / n;

                float accel = -Gravity * dirY; // gravity along the slope
                if (gas) accel += EngineAccel + wheelieBonus;
                if (brake) accel -= (vel.x > 0.5f ? BrakeDecel : EngineAccel * 0.7f);
                if (!gas && !brake) accel -= vel.x * 0.35f; // rolling drag

                vel.x = Mathf.Clamp(vel.x + dirX * accel * dt, -MaxReverse, MaxSpeed);
                vel.y = vel.x * slope;

                // Wheelie: lean back at speed.
                wheelie = leanB && vel.x > 6f;
                wheelieBonus = wheelie ? 4f : 0f;
                float targetPitch = wheelie ? 0.45f : slopeAng + (leanB ? 0.12f : 0f) - (leanF ? 0.12f : 0f);
                pitch = Mathf.LerpAngle(pitch, targetPitch, 1f - Mathf.Exp(-8f * dt));

                p.x += vel.x * dt;
                float gy2 = WorldBuilder.GetGroundY(p.x);
                if (float.IsNaN(gy2))
                {
                    grounded = false; // launched off the crest
                    // GetSlope goes blind within 0.75 of an edge, so sample
                    // behind it to preserve the launch lip's upward kick.
                    float sBack = WorldBuilder.GetSlope(p.x - 1.5f);
                    if (sBack > 0.01f) vel.y = vel.x * sBack;
                }
                else
                {
                    p.y = gy2 + RideHeight;
                    // Crest liftoff: not enough gravity to hold the curve.
                    float h = 0.6f;
                    float y0 = gy2;
                    float yp = WorldBuilder.GetGroundY(p.x + h);
                    float ym = WorldBuilder.GetGroundY(p.x - h);
                    if (!float.IsNaN(yp) && !float.IsNaN(ym))
                    {
                        float curve = (yp - 2f * y0 + ym) / (h * h); // y''
                        if (vel.x * vel.x * (-curve) > Gravity) grounded = false;
                    }
                    // Face-plant into a sudden wall.
                    float ahead = WorldBuilder.GetGroundY(p.x + 1.2f);
                    if (!float.IsNaN(ahead) && ahead - gy2 > 1.3f && vel.x > 4f) { Crash(); return; }
                }
                p.x = Mathf.Max(p.x, -8f);
                transform.position = p;
                transform.rotation = Quaternion.Euler(0f, 0f, pitch * Mathf.Rad2Deg);

                // Checkpoint every 1.5 s on safe ground.
                checkpointTimer += dt;
                if (checkpointTimer > 1.5f) { checkpointTimer = 0f; checkpoint = p; }
            }
        }

        if (!grounded)
        {
            float lean = (leanB ? 1f : 0f) - (leanF ? 1f : 0f);
            pitch = WrapAngle(pitch + lean * RotateSpeed * dt);
            vel.y -= Gravity * dt;
            vel.x *= (1f - AirDrag * dt);
            // Timeline wind: the past fights you, the future pulls you forward.
            vel.x += TimelineManager.WindX(TimelineManager.Active) * dt;
            vel.x = Mathf.Min(vel.x, MaxSpeed + 4f);
            p.x += vel.x * dt;
            p.y += vel.y * dt;

            float gy = WorldBuilder.GetGroundY(p.x);
            if (float.IsNaN(gy))
            {
                // Over the gap: 1999 has traffic below, other eras a deep pit.
                float ky = (TimelineManager.Active == Timeline.Present) ? -5.2f : WorldBuilder.KillY;
                if (p.y < ky) { Crash(); return; }
            }
            else if (p.y <= gy + RideHeight)
            {
                float slopeAng = Mathf.Atan(WorldBuilder.GetSlope(p.x));
                float err = Mathf.Abs(WrapAngle(pitch - slopeAng));
                bool wallSlam = p.y < gy - 0.2f && vel.x > 5f;
                if (wallSlam || err > MaxLandTilt || vel.y < -MaxLandFall) { Crash(); return; }
                // Landed.
                float fall = -vel.y;
                p.y = gy + RideHeight;
                vel.y = 0f;
                vel.x *= 0.85f;
                grounded = true;
                wheelie = false;
                transform.position = p;
                visual.KickSuspension(fall);
                AudioDirector.PlayLand();
                if (GameManager.Instance != null && GameManager.Instance.cameraRig != null)
                    GameManager.Instance.cameraRig.AddShake(Mathf.Clamp01(fall / 20f) * 0.6f);
            }

            p.x = Mathf.Max(p.x, -8f);
            transform.position = p;
            transform.rotation = Quaternion.Euler(0f, 0f, pitch * Mathf.Rad2Deg);
        }

        // Rings, coins, and tower hazards for the active timeline.
        CheckWorld(transform.position);
        GameData.topSpeed = Mathf.Max(GameData.topSpeed, SpeedKmh);

        // Finish.
        if (transform.position.x >= WorldBuilder.FinishX && GameManager.State == GameState.Playing)
            GameManager.Instance.Complete();

        float leanVis = (leanB ? 1f : 0f) - (leanF ? 1f : 0f);
        visual.SetFrame(leanVis, wheelie, grounded, vel.magnitude, dt);
    }

    void CheckWorld(Vector3 p)
    {
        int ti = (int)TimelineManager.Active;
        // Shift rings: thread one for a boost and a pendant charge.
        for (int i = 0; i < GameData.rings.Count; i++)
        {
            var r = GameData.rings[i];
            if (r.taken || r.timeline != ti) continue;
            if (Mathf.Abs(p.x - r.pos.x) < 1.5f)
            {
                float dy = p.y - r.pos.y, dz = p.z - r.pos.z;
                if (dy * dy + dz * dz < r.radius * r.radius)
                {
                    r.taken = true;
                    GameData.rings[i] = r;
                    GameData.ringsHit++;
                    TimelineManager.AddCharge();
                    vel *= 1.22f;
                    if (vel.x > MaxSpeed) vel.x = MaxSpeed;
                    AudioDirector.PlayRing();
                    if (GameManager.Instance != null && GameManager.Instance.pendantFx != null)
                        GameManager.Instance.pendantFx.RingBurst(p);
                }
            }
        }
        // Coins.
        for (int i = 0; i < GameData.coins.Count; i++)
        {
            var c = GameData.coins[i];
            if (c.taken) continue;
            float dx = p.x - c.pos.x, dy = p.y - c.pos.y, dz = p.z - c.pos.z;
            if (dx * dx + dy * dy + dz * dz < 2.9f)
            {
                c.taken = true;
                GameData.coins[i] = c;
                if (c.node != null) c.node.gameObject.SetActive(false);
                GameData.coinsGot++;
                AudioDirector.PlayCoin();
            }
        }
        // Watchtowers (medieval): clip one and you're wrecked.
        for (int i = 0; i < GameData.towers.Count; i++)
        {
            var t = GameData.towers[i];
            if (t.timeline != ti) continue;
            if (Mathf.Abs(p.x - t.x) < t.halfW + 0.7f && p.y - 0.6f < t.topY)
            {
                Crash();
                return;
            }
        }
    }

    void Crash()
    {
        if (GameManager.State != GameState.Playing) return;
        GameManager.Instance.OnCrash(transform.position);
    }
}
