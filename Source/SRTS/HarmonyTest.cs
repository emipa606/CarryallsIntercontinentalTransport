using HarmonyLib;
using RimWorld;
using Verse;

namespace SRTS;

/* Akreedz original patch */
[HarmonyPatch(typeof(DropPodUtility), "MakeDropPodAt")]
public static class HarmonyTest
{
    public static bool Prefix(IntVec3 c, Map map, ActiveTransporterInfo info)
    {
        foreach (var thing in info.innerContainer)
        {
            var comp = thing.TryGetComp<CompLaunchableSRTS>();
            ActiveTransporter ActiveTransporter;
            if (comp != null)
            {
                ActiveTransporter = (ActiveTransporter)ThingMaker.MakeThing(SRTSStatic.SkyfallerActiveDefByRot(comp));
                ActiveTransporter.Contents = info;

                EnsureInBoundsSRTS(ref c, thing.def, map);
                var incomingSRTS = (SRTSIncoming)SkyfallerMaker.SpawnSkyfaller(
                    SkyfallerIncomingDefByRot(thing.TryGetComp<CompLaunchableSRTS>()),
                    ActiveTransporter, c, map);
                incomingSRTS.SRTSRotation = thing.Rotation;
                return false;
            }

            if (DefDatabase<ThingDef>.GetNamedSilentFail(thing.def.defName.Split('_')[0])
                    ?.GetCompProperties<CompProperties_BombsAway>() == null)
            {
                continue;
            }

            DefDatabase<ThingDef>.GetNamed(thing.def.defName.Split('_')[0]);
            ActiveTransporter = (ActiveTransporter)ThingMaker.MakeThing(thing.def);
            ActiveTransporter.Contents = info;
            return false;
        }

        return true;
    }

    private static ThingDef SkyfallerIncomingDefByRot(CompLaunchableSRTS comp)
    {
        if (comp.parent.Rotation == Rot4.East)
        {
            return comp.SRTSProps.eastSkyfallerIncoming;
        }

        return comp.parent.Rotation == Rot4.West ? comp.SRTSProps.westSkyfallerIncoming : comp.SRTSProps.eastSkyfaller;
    }

    private static void EnsureInBoundsSRTS(ref IntVec3 c, ThingDef shipDef, Map map)
    {
        var x = (int)shipDef.graphicData.drawSize.x;
        var y = (int)shipDef.graphicData.drawSize.y;
        var offset = x > y ? x : y;

        if (c.x < offset)
        {
            c.x = offset;
        }
        else if (c.x >= map.Size.x - offset)
        {
            c.x = map.Size.x - offset;
        }

        if (c.z < offset)
        {
            c.z = offset;
        }
        else if (c.z > map.Size.z - offset)
        {
            c.z = map.Size.z - offset;
        }
    }
}