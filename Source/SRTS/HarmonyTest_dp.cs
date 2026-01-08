using HarmonyLib;
using RimWorld;
using Verse;

namespace SRTS;

/* Akreedz original patch */
[HarmonyPatch(typeof(ActiveTransporter), "PodOpen", [])]
public static class HarmonyTest_dp
{
    public static void Prefix(ActiveTransporter __instance)
    {
        var ActiveTransporterInfo = Traverse.Create(__instance).Field("contents").GetValue<ActiveTransporterInfo>();
        for (var index = ActiveTransporterInfo.innerContainer.Count - 1; index >= 0; --index)
        {
            var thing = ActiveTransporterInfo.innerContainer[index];
            if (thing?.TryGetComp<CompLaunchableSRTS>() == null)
            {
                continue;
            }

            GenSpawn.Spawn(thing, __instance.Position, __instance.Map, thing.Rotation);
            break;
        }
    }
}