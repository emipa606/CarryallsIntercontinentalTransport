using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;

namespace SRTS.Patches;

[HarmonyPatch]
public static class Dialog_LoadTransporters_AddItemsToTransferables_Patch
{
    [HarmonyTargetMethod]
    public static MethodBase Target()
    {
        return AccessTools.Method(typeof(Dialog_LoadTransporters), "AddItemsToTransferables");
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
    {
        return StartUp.AddItemsEntireMapNonHomeTranspiler(instructions, ilg);
    }
}