using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SRTS;

/* Akreedz original patch */
[HarmonyPatch(typeof(TransportersArrivalAction_LandInSpecificCell), "Arrived", typeof(List<ActiveTransporterInfo>),
    typeof(PlanetTile))]
public static class HarmonyTest_AJ
{
    public static bool Prefix(TransportersArrivalAction_LandInSpecificCell __instance,
        List<ActiveTransporterInfo> transporters)
    {
        foreach (var pod in transporters)
        {
            foreach (var thing in pod.innerContainer)
            {
                if (thing.TryGetComp<CompLaunchableSRTS>() == null && DefDatabase<ThingDef>
                        .GetNamed(thing?.def?.defName?.Split('_')[0], false)
                        ?.GetCompProperties<CompProperties_LaunchableSRTS>() == null)
                {
                    continue;
                }

                var lookTarget = TransportersArrivalActionUtility.GetLookTarget(transporters);
                var traverse = Traverse.Create(__instance);
                var c = traverse.Field("cell").GetValue<IntVec3>();
                var map = traverse.Field("mapParent").GetValue<MapParent>().Map;
                TransportersArrivalActionUtility.RemovePawnsFromWorldPawns(transporters);
                foreach (var activeTransporterInfo in transporters)
                {
                    activeTransporterInfo.openDelay = 0;
                    DropPodUtility.MakeDropPodAt(c, map, activeTransporterInfo);
                }

                Messages.Message("MessageTransportPodsArrived".Translate(), lookTarget,
                    MessageTypeDefOf.TaskCompletion);
                return false;
            }
        }

        return true;
    }
}