using UnityEngine;
using System;

// The three timelines share one map geography. Time travel keeps Jack's
// position and velocity: the pendant can fire mid-air, mid-jump, anytime.
// Shifts cost a pendant charge; threading shift rings earns them back.
public enum Timeline { Present, Medieval, Future }

public static class TimelineManager
{
    public static Timeline Active { get; private set; } = Timeline.Present;
    public static int Charges { get; private set; } = 3;
    public const int MaxCharges = 5;
    public static event Action<Timeline> OnShift;

    public static void Reset()
    {
        Active = Timeline.Present;
        Charges = 3;
        OnShift = null;
    }

    public static void ResetRun()
    {
        Active = Timeline.Present;
        Charges = 3;
    }

    public static bool CanShift => Charges > 0;

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

    public static void AddCharge()
    {
        Charges = Mathf.Min(MaxCharges, Charges + 1);
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
