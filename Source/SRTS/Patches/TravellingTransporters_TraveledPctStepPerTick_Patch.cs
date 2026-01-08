using HarmonyLib;
using RimWorld.Planet;
using System.Collections.Generic;

namespace SRTS.Patches;

[HarmonyPatch(typeof(TravellingTransporters), "TraveledPctStepPerTick", MethodType.Getter)]
public static class TravellingTransporters_TraveledPctStepPerTick_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(PlanetTile ___initialTile, PlanetTile ___destinationTile, List<RimWorld.ActiveTransporterInfo> ___transporters, ref float __result)
    {
        return StartUp.CustomTravelSpeedSRTS(___initialTile, ___destinationTile, ___transporters, ref __result);
    }
}
