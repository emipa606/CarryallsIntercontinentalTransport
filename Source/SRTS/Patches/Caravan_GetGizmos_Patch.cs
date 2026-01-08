using System.Collections.Generic;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace SRTS.Patches;

[HarmonyPatch(typeof(Caravan), nameof(Caravan.GetGizmos))]
public static class Caravan_GetGizmos_Patch
{
    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<Gizmo> __result, Caravan __instance)
    {
        __result = StartUp.LaunchAndBombGizmosPassthrough(__result, __instance);
    }
}
