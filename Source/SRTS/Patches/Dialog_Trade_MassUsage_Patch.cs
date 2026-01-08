using System.Collections.Generic;
using HarmonyLib;
using RimWorld;

namespace SRTS.Patches;

[HarmonyPatch(typeof(Dialog_Trade), "MassUsage", MethodType.Getter)]
public static class Dialog_Trade_MassUsage_Patch
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, System.Reflection.Emit.ILGenerator ilg)
    {
        return StartUp.SRTSMassUsageCaravanTranspiler(instructions, ilg);
    }
}
