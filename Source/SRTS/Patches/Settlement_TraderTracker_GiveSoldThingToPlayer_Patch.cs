using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace SRTS.Patches;

[HarmonyPatch]
public static class Settlement_TraderTracker_GiveSoldThingToPlayer_Patch
{
    [HarmonyTargetMethod]
    public static MethodBase Target()
    {
        var type = AccessTools.TypeByName("RimWorld.Planet.Settlement_TraderTracker");
        return AccessTools.Method(type, "GiveSoldThingToPlayer");
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, System.Reflection.Emit.ILGenerator ilg)
    {
        return StartUp.GiveSoldThingsToSRTSTranspiler(instructions, ilg);
    }
}
