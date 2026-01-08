using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SRTS;

public class SRTSLeaving : Skyfaller, IActiveTransporter
{
    private const int TakeoffCountTicks = 300;
    private static readonly List<Thing> tmpActiveTransporters = [];
    private bool alreadyLeft;
    public TransportersArrivalAction arrivalAction;
    public PlanetTile destinationTile = -1;
    public int groupID = -1;

    private bool initiatingTakeoff;

    private Vector3 originalDrawPos = Vector3.zero;
    public Rot4 rotation;

    private bool soundPlayed;

    private int takeoffTicks;

    public ActiveTransporterInfo Contents
    {
        get => ((ActiveTransporter)innerContainer[0]).Contents;
        private set => ((ActiveTransporter)innerContainer[0]).Contents = value;
    }


    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        originalDrawPos = base.DrawPos;
        Rotation = Rot4.West; //rotation; going to be for directional lift off
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref groupID, "groupID");
        Scribe_Values.Look(ref destinationTile, "destinationTile");
        Scribe_Deep.Look(ref arrivalAction, "arrivalAction");
        Scribe_Values.Look(ref alreadyLeft, "alreadyLeft");
        Scribe_Values.Look(ref rotation, "rotation", Rot4.North);
    }

    protected override void LeaveMap()
    {
        if (alreadyLeft)
        {
            base.LeaveMap();
        }
        else if (groupID < 0)
        {
            Log.Error($"Drop pod left the map, but its group ID is {groupID}");
            Destroy();
        }
        else if (destinationTile < 0)
        {
            Log.Error($"Drop pod left the map, but its destination tile is {destinationTile}");
            Destroy();
        }
        else
        {
            var lord = TransporterUtility.FindLord(groupID, Map);
            if (lord != null)
            {
                Map.lordManager.RemoveLord(lord);
            }

            var travelingTransportPods =
                (TravelingSRTS)WorldObjectMaker.MakeWorldObject(StaticDefOf.TravelingSRTS_Carryall);
            travelingTransportPods.Tile = Map.Tile;
            travelingTransportPods.SetFaction(Faction.OfPlayer);
            travelingTransportPods.destinationTile = destinationTile;
            travelingTransportPods.arrivalAction = arrivalAction;

            Find.WorldObjects.Add(travelingTransportPods);
            tmpActiveTransporters.Clear();
            tmpActiveTransporters.AddRange(Map.listerThings.ThingsInGroup(ThingRequestGroup.ActiveTransporter));
            travelingTransportPods.flyingThing =
                tmpActiveTransporters.Find(x => (x as SRTSLeaving)?.groupID == groupID);
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var index = 0; index < tmpActiveTransporters.Count; index++)
            {
                var thing = tmpActiveTransporters[index];
                if (thing is not SRTSLeaving tmpActiveTransporter || tmpActiveTransporter.groupID != groupID)
                {
                    continue;
                }

                tmpActiveTransporter.alreadyLeft = true;
                travelingTransportPods.AddTransporter(tmpActiveTransporter.Contents, true);
                tmpActiveTransporter.Contents = null;
                tmpActiveTransporter.Destroy();
            }
        }
    }

    protected override void Tick()
    {
        takeoffTicks++;
        if (takeoffTicks >= TakeoffCountTicks && !initiatingTakeoff)
        {
            initiatingTakeoff = true;
            originalDrawPos = SkyfallerDrawPosUtilityExtended.DrawPos_TakeoffUpward(originalDrawPos, TakeoffCountTicks);
        }

        if (!initiatingTakeoff)
        {
            return;
        }

        ticksToImpact++;

        if (!soundPlayed && def.skyfaller.anticipationSound != null &&
            ticksToImpact > def.skyfaller.anticipationSoundTicks)
        {
            soundPlayed = true;
            def.skyfaller.anticipationSound.PlayOneShot(new TargetInfo(Position, Map));
        }

        if (ticksToImpact == 220)
        {
            LeaveMap();
        }
    }
}