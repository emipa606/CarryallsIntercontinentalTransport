using System.Collections.Generic;
using HarmonyLib;
using RimWorld.Planet;

namespace SRTS.Patches;

[HarmonyPatch(typeof(ExpandableWorldObjectsUtility), nameof(ExpandableWorldObjectsUtility.ExpandableWorldObjectsOnGUI))]
public static class ExpandableWorldObjectsUtility_ExpandableWorldObjectsOnGUI_Patch
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, System.Reflection.Emit.ILGenerator ilg)
    {
        return StartUp.ExpandableIconDetourSRTSTranspiler(instructions, ilg);
    }
}
