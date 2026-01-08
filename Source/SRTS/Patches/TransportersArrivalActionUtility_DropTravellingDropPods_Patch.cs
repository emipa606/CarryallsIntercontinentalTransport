using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using System.Collections.Generic;

namespace SRTS.Patches;

[HarmonyPatch(typeof(TransportersArrivalActionUtility), nameof(TransportersArrivalActionUtility.DropTravellingDropPods))]
public static class TransportersArrivalActionUtility_DropTravellingDropPods_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(List<ActiveTransporterInfo> transporters, IntVec3 near, Map map)
    {
        return StartUp.DropSRTSExactSpot(transporters, near, map);
    }
}
