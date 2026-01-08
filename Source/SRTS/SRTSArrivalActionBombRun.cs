using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SRTS;

public class SRTSArrivalActionBombRun : TransportersArrivalAction
{
    private List<IntVec3> bombCells;

    private BombingType bombType;

    private MapParent mapParent;

    private IntVec3 originalLandingSpot;

    private Map originalMap;

    private IntVec3 targetCellA;

    private IntVec3 targetCellB;

    public SRTSArrivalActionBombRun()
    {
    }

    public SRTSArrivalActionBombRun(MapParent mapParent, Pair<IntVec3, IntVec3> targetCells,
        IEnumerable<IntVec3> bombCells, BombingType bombType, Map originalMap, IntVec3 originalLandingSpot)
    {
        this.mapParent = mapParent;
        targetCellA = targetCells.First;
        targetCellB = targetCells.Second;
        this.bombCells = bombCells.ToList();
        this.originalMap = originalMap;
        this.originalLandingSpot = originalLandingSpot;
        this.bombType = bombType;
    }

    public override bool GeneratesMap { get; }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref mapParent, "mapParent");
        Scribe_References.Look(ref originalMap, "originalMap");
        Scribe_Values.Look(ref targetCellA, "targetCellA");
        Scribe_Values.Look(ref targetCellB, "targetCellB");
        Scribe_Values.Look(ref bombType, "bombType");
        Scribe_Collections.Look(ref bombCells, "bombCells");
        Scribe_Values.Look(ref originalLandingSpot, "originalLandingSpot");
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

        return CanBombSpecificCell(pods, mapParent);
    }

    public override void Arrived(List<ActiveTransporterInfo> pods, PlanetTile tile)
    {
        var lookTarget = TransportersArrivalActionUtility.GetLookTarget(pods);
        BombRunArrivalUtility.BombWithSRTS(pods, targetCellA, targetCellB, bombCells, bombType, mapParent.Map,
            originalMap, originalLandingSpot);
        Messages.Message("BombRunStarted".Translate(), lookTarget, MessageTypeDefOf.CautionInput);
    }

    public static bool CanBombSpecificCell(IEnumerable<IThingHolder> pods, MapParent mapParent)
    {
        return mapParent is { Spawned: true, HasMap: true };
    }
}