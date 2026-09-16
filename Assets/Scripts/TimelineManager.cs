using UnityEngine;
using System;

// The three timelines share one map geography. Time travel keeps Jack's
// position and velocity: the pendant can fire mid-air, mid-jump, anytime.
// Shifts cost a pendant charge; charges regenerate over time.
public enum Timeline { Present, Medieval, Future }

public static class TimelineManager
{
    public static Timeline Active { get; private set; } = Timeline.Present;
    public static int Charges { get; private set; } = 3;
    public const int MaxCharges = 5;
    const float RegenSecondsPerCharge = 8f;
    static float regenTimer;
    public static event Action<Timeline> OnShift;

    public static void Reset()
    {
        Active = Timeline.Present;
        Charges = 3;
        regenTimer = 0f;
        OnShift = null;
    }

    public static void ResetRun()
    {
        Active = Timeline.Present;
        Charges = 3;
        regenTimer = 0f;
    }

    public static bool CanShift => Charges > 0;

    // Called every frame from GameManager: refills the pendant over time.
    public static void Tick(float dt)
    {
        if (Charges >= MaxCharges) { regenTimer = 0f; return; }
        regenTimer += dt;
        if (regenTimer >= RegenSecondsPerCharge)
        {
            regenTimer = 0f;
            Charges = Mathf.Min(MaxCharges, Charges + 1);
        }
    }

    // Returns true if the shift fired.
    public static bool TryShift()
    {
        if (!CanShift) return false;
        if (GameManager.State != GameState.Playing) return false;
        Charges--;
        GameData.shiftsUsed++;
        Active = (Timeline)(((int)Active + 1) % 3);
        if (OnShift != null) OnShift(Active);
        if (!GameData.quipShown)
        {
            GameData.quipShown = true;
            if (GameManager.Instance != null) GameManager.Instance.Quip();
        }
        return true;
    }

    // The past fights you, the future pulls you forward.
    public static float WindX(Timeline t)
    {
        if (t == Timeline.Medieval) return -4.5f;
        if (t == Timeline.Future) return 2.5f;
        return 0f;
    }

    public static string Name(Timeline t)
    {
        return t == Timeline.Present ? "PRESENT" : t == Timeline.Medieval ? "MEDIEVAL" : "FUTURE";
    }

    public static string Year(Timeline t)
    {
        return t == Timeline.Present ? "1999" : t == Timeline.Medieval ? "999" : "2999";
    }
}
