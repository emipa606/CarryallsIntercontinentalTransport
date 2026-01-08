using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SRTS.ArrivalActions;

public class CarryallArrivalAction_LandInSpecificCell : TransportersArrivalAction_LandInSpecificCell
{
    private IntVec3 cell;
    private MapParent mapParent;

    public CarryallArrivalAction_LandInSpecificCell(MapParent mapParent, PlanetTile shuttleBayPos)
    {
        this.mapParent = mapParent;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref mapParent, "mapParent");
        Scribe_Values.Look(ref cell, "cell");
    }

    public override FloatMenuAcceptanceReport StillValid(IEnumerable<IThingHolder> pods, PlanetTile destinationTile)
    {
        var floatMenuAcceptanceReport = base.StillValid(pods, destinationTile);
        if (!floatMenuAcceptanceReport)
        {
            return floatMenuAcceptanceReport;
        }

        if (mapParent != null && mapParent.Tile != destinationTile)
        {
            return false;
        }

        return CanLandInSpecificCell(pods, mapParent);
    }

    public override void Arrived(List<ActiveTransporterInfo> pods, PlanetTile tile)
    {
        var lookTarget = TransportersArrivalActionUtility.GetLookTarget(pods);
        Messages.Message("MessageTransportPodsArrived".Translate(), lookTarget, MessageTypeDefOf.TaskCompletion);

        //
        DropCarryall(pods, cell, mapParent.Map);
    }

    private static void DropCarryall(List<ActiveTransporterInfo> dropPods, IntVec3 near, Map map)
    {
        TransportersArrivalActionUtility.RemovePawnsFromWorldPawns(dropPods);
        foreach (var transporterInfo in dropPods)
        {
            if (!near.IsValid)
            {
                near = DropCellFinder.GetBestShuttleLandingSpot(map, Faction.OfPlayer);
            }

            DropPodUtility.MakeDropPodAt(near, map, transporterInfo);
        }
    }
}