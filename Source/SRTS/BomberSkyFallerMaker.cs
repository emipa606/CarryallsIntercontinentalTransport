using System.Collections.Generic;
using SPExtended;
using Verse;

namespace SRTS;

public static class BomberSkyfallerMaker
{
    public static BomberSkyfaller MakeSkyfaller(ThingDef skyfaller)
    {
        return (BomberSkyfaller)ThingMaker.MakeThing(skyfaller);
    }

    public static BomberSkyfaller MakeSkyfaller(ThingDef skyfaller, ThingDef innerThing)
    {
        var innerThing2 = ThingMaker.MakeThing(innerThing);
        return MakeSkyfaller(skyfaller, innerThing2);
    }

    public static BomberSkyfaller MakeSkyfaller(ThingDef skyfaller, Thing innerThing)
    {
        var skyfaller2 = MakeSkyfaller(skyfaller);
        if (innerThing == null || skyfaller2.innerContainer.TryAdd(innerThing))
        {
            return skyfaller2;
        }

        Log.Error($"Could not add {innerThing.ToStringSafe()} to a skyfaller.");
        innerThing.Destroy();

        return skyfaller2;
    }

    public static BomberSkyfaller MakeSkyfaller(ThingDef skyfaller, IEnumerable<Thing> things)
    {
        var skyfaller2 = MakeSkyfaller(skyfaller);
        if (things != null)
        {
            skyfaller2.innerContainer.TryAddRangeOrTransfer(things, false, true);
        }

        return skyfaller2;
    }

    public static BomberSkyfaller SpawnSkyfaller(ThingDef skyfaller, IntVec3 pos, Map map)
    {
        var thing = MakeSkyfaller(skyfaller);
        return (BomberSkyfaller)GenSpawn.Spawn(thing, pos, map);
    }

    public static BomberSkyfaller SpawnSkyfaller(ThingDef skyfaller, ThingDef innerThing, IntVec3 pos, Map map)
    {
        var thing = MakeSkyfaller(skyfaller, innerThing);
        return (BomberSkyfaller)GenSpawn.Spawn(thing, pos, map);
    }

    public static void SpawnSkyfaller(ThingDef skyfaller, Thing innerThing, IntVec3 start, IntVec3 end,
        List<IntVec3> bombCells, BombingType bombType, Map map, int idNumber, Thing original, Map originalMap,
        IntVec3 landingSpot)
    {
        var thing = MakeSkyfaller(skyfaller, innerThing);
        thing.originalMap = originalMap;
        thing.sourceLandingSpot = landingSpot;
        thing.numberOfBombs = SRTSMod.GetStatFor<int>(original.def.defName, StatName.numberBombs);
        thing.precisionBombingNumBombs =
            SRTSMod.GetStatFor<int>(original.def.defName, StatName.precisionBombingNumBombs);
        thing.speed = SRTSMod.GetStatFor<float>(original.def.defName, StatName.bombingSpeed);
        thing.radius = SRTSMod.GetStatFor<int>(original.def.defName, StatName.radiusDrop);
        thing.sound = original.TryGetComp<CompBombFlyer>().Props.soundFlyBy;
        thing.bombType = bombType;

        var angle = start.AngleToPointRelative(end);
        thing.angle = (float)(angle + 90) * -1;
        var exitPoint = SPTrig.ExitPointCustom(angle, start, map);

        var bomber = (BomberSkyfaller)GenSpawn.Spawn(thing, exitPoint, map);
        bomber.bombCells = bombCells;
    }

    public static BomberSkyfaller SpawnSkyfaller(ThingDef skyfaller, IEnumerable<Thing> things, IntVec3 pos, Map map)
    {
        var thing = MakeSkyfaller(skyfaller, things);
        return (BomberSkyfaller)GenSpawn.Spawn(thing, pos, map);
    }
}