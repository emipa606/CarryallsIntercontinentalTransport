using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SRTS.Patches;

[HarmonyPatch(typeof(CompTransporter), nameof(CompTransporter.CompGetGizmosExtra))]
public static class CompTransporter_CompGetGizmosExtra_Patch
{
    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<Gizmo> __result, CompTransporter __instance)
    {
        StartUp.NoLaunchGroupForSRTS(ref __result, __instance);
    }
}
