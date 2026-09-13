using UnityEngine;
using System.Collections.Generic;

// Shared registries for shift rings, coins, and hazard towers, plus
// per-run stats. World props register here at build time; the bike
// queries them every frame.
public struct RingDef
{
    public int timeline;
    public Vector3 pos;
    public float radius;
    public bool taken;
}

public struct CoinDef
{
    public Vector3 pos;
    public bool taken;
    public Transform node;
}

public struct TowerDef
{
    public int timeline;
    public float x;
    public float topY;
    public float halfW;
}

public static class GameData
{
    public static List<RingDef> rings = new List<RingDef>();
    public static List<CoinDef> coins = new List<CoinDef>();
    public static List<TowerDef> towers = new List<TowerDef>();

    public static int shiftsUsed;
    public static int coinsGot;
    public static int ringsHit;
    public static float topSpeed;
    public static float runTime;
    public static bool quipShown;

    public static void ClearWorld()
    {
        rings.Clear();
        coins.Clear();
        towers.Clear();
    }

    public static void ResetRun()
    {
        shiftsUsed = 0;
        coinsGot = 0;
        ringsHit = 0;
        topSpeed = 0f;
        runTime = 0f;
        for (int i = 0; i < rings.Count; i++)
        {
            var r = rings[i];
            r.taken = false;
            rings[i] = r;
        }
        for (int i = 0; i < coins.Count; i++)
        {
            var c = coins[i];
            c.taken = false;
            coins[i] = c;
            if (c.node != null) c.node.gameObject.SetActive(true);
        }
    }
}
