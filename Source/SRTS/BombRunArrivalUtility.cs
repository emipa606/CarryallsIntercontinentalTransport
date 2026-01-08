using System.Collections.Generic;
using RimWorld;
using Verse;

namespace SRTS;

public static class BombRunArrivalUtility
{
    public static void BombWithSRTS(List<ActiveTransporterInfo> srts, IntVec3 targetA, IntVec3 targetB,
        List<IntVec3> bombCells, BombingType bombType, Map map, Map originalMap, IntVec3 returnSpot)
    {
        if (srts.Count > 1)
        {
            Log.Error(
                "Initiating bomb run with more than 1 SRTS in Drop Pod Group. This should not happen. - Smash Phil");
        }

        foreach (var activeTransporterInfo in srts)
        {
            MakeSRTSBombingAt(targetA, targetB, bombCells, bombType, map, activeTransporterInfo, originalMap,
                returnSpot);
        }
    }

    private static void MakeSRTSBombingAt(IntVec3 c1, IntVec3 c2, List<IntVec3> bombCells, BombingType bombType,
        Map map,
        ActiveTransporterInfo info, Map originalMap, IntVec3 returnSpot)
    {
        foreach (var thing in info.innerContainer)
        {
            if (thing.TryGetComp<CompLaunchableSRTS>() == null)
            {
                continue;
            }

            var shipType = thing.def.defName;
            var srts = (ActiveTransporter)ThingMaker.MakeThing(ThingDef.Named($"{shipType}_Active"));
            srts.Contents = info;
            BomberSkyfallerMaker.SpawnSkyfaller(ThingDef.Named($"{shipType}_BomberRun"), srts, c1, c2, bombCells,
                bombType, map, thing.thingIDNumber, thing, originalMap, returnSpot);
        }
    }
}