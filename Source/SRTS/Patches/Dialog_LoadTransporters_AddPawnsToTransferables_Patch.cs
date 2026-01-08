using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SRTS.Patches;

[HarmonyPatch]
public static class Dialog_LoadTransporters_AddPawnsToTransferables_Patch
{
    [HarmonyTargetMethod]
    public static MethodBase Target()
    {
        return AccessTools.Method(typeof(Dialog_LoadTransporters), "AddPawnsToTransferables");
    }

    [HarmonyPrefix]
    public static bool Prefix(List<CompTransporter> ___transporters, Map ___map, Dialog_LoadTransporters __instance)
    {
        if (ModLister.HasActiveModWithName("Save Our Ship 2"))
        {
            return StartUp.CustomOptionsPawnsToTransportOverride(___transporters, ___map, __instance);
        }

        return true;
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
    {
        if (ModLister.HasActiveModWithName("Save Our Ship 2"))
        {
            return instructions;
        }

        return StartUp.CustomOptionsPawnsToTransportTranspiler(instructions, ilg);
    }
}