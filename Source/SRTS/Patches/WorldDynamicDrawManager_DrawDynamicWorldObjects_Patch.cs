using System.Collections.Generic;
using HarmonyLib;
using RimWorld.Planet;

namespace SRTS.Patches;

[HarmonyPatch(typeof(WorldDynamicDrawManager), nameof(WorldDynamicDrawManager.DrawDynamicWorldObjects))]
public static class WorldDynamicDrawManager_DrawDynamicWorldObjects_Patch
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, System.Reflection.Emit.ILGenerator ilg)
    {
        return StartUp.DrawDynamicSRTSObjectsTranspiler(instructions, ilg);
    }
}
