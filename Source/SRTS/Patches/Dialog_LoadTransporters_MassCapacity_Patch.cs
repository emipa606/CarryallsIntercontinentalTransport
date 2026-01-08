using System.Collections.Generic;
using HarmonyLib;
using RimWorld;

namespace SRTS.Patches;

[HarmonyPatch(typeof(Dialog_LoadTransporters), "MassCapacity", MethodType.Getter)]
public static class Dialog_LoadTransporters_MassCapacity_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(ref float __result, List<CompTransporter> ___transporters)
    {
        return StartUp.CustomSRTSMassCapacity(ref __result, ___transporters);
    }
}