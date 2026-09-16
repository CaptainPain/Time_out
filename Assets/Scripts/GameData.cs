using UnityEngine;
using System.Collections.Generic;

// Shared registries for hazard towers plus per-run stats. World props
// register here at build time; the bike queries them every frame.
public struct TowerDef
{
    public int timeline;
    public float x;
    public float topY;
    public float halfW;
}

public static class GameData
{
    public static List<TowerDef> towers = new List<TowerDef>();

    public static int shiftsUsed;
    public static float topSpeed;
    public static float runTime;
    public static bool quipShown;

    public static void ClearWorld()
    {
        towers.Clear();
    }

    public static void ResetRun()
    {
        shiftsUsed = 0;
        topSpeed = 0f;
        runTime = 0f;
    }
}
