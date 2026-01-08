using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SRTS;

public class CompBombFlyer : ThingComp
{
    public BombingType bombType;
    public Building SRTS_Launcher => parent as Building;
    public CompLaunchableSRTS CompLauncher => SRTS_Launcher.GetComp<CompLaunchableSRTS>();
    public CompProperties_BombsAway Props => (CompProperties_BombsAway)props;

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (SRTS_Launcher.GetComp<CompLaunchableSRTS>().LoadingInProgressOrReadyToLaunch)
        {
            yield return new Command_Action
            {
                defaultLabel = "BombTarget".Translate(),
                defaultDesc = "BombTargetDesc".Translate(),
                icon = TexCommand.Attack,
                action = delegate
                {
                    var num = 0;
                    foreach (var t in CompLauncher.Transporter.innerContainer)
                    {
                        if (t is Pawn { IsColonist: true })
                        {
                            num++;
                        }
                    }

                    if (SRTSMod.mod.settings.passengerLimits)
                    {
                        if (num < SRTSMod.GetStatFor<int>(parent.def.defName, StatName.minPassengers))
                        {
                            Messages.Message("NotEnoughPilots".Translate(), MessageTypeDefOf.RejectInput, false);
                            return;
                        }

                        if (num > SRTSMod.GetStatFor<int>(parent.def.defName, StatName.maxPassengers))
                        {
                            Messages.Message("TooManyPilots".Translate(), MessageTypeDefOf.RejectInput, false);
                            return;
                        }
                    }

                    var carpetBombing = new FloatMenuOption("CarpetBombing".Translate(), delegate
                    {
                        bombType = BombingType.carpet;
                        if (CompLauncher.AnyInGroupHasAnythingLeftToLoad)
                        {
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                "ConfirmSendNotCompletelyLoadedPods".Translate(
                                    CompLauncher.FirstThingLeftToLoadInGroup.LabelCapNoCount,
                                    CompLauncher.FirstThingLeftToLoadInGroup), StartChoosingDestinationBomb));
                        }

                        StartChoosingDestinationBomb();
                    });
                    var preciseBombing = new FloatMenuOption("PreciseBombing".Translate(), delegate
                    {
                        bombType = BombingType.precise;
                        if (CompLauncher.AnyInGroupHasAnythingLeftToLoad)
                        {
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                "ConfirmSendNotCompletelyLoadedPods".Translate(
                                    CompLauncher.FirstThingLeftToLoadInGroup.LabelCapNoCount,
                                    CompLauncher.FirstThingLeftToLoadInGroup), StartChoosingDestinationBomb));
                        }

                        StartChoosingDestinationBomb();
                    });
                    Find.WindowStack.Add(new FloatMenuGizmo([carpetBombing, preciseBombing],
                        parent, parent.LabelCap, UI.MouseMapPosition()));
                }
            };
        }
    }

    private void StartChoosingDestinationBomb()
    {
        CameraJumper.TryJump(CameraJumper.GetWorldTarget(parent));
        Find.WorldSelector.ClearSelection();
        var tile = parent.Map.Tile;

        Find.WorldTargeter.BeginTargeting(ChoseWorldTargetToBomb, false, Tex2D.LauncherTargeting, true,
            delegate { GenDraw.DrawWorldRadiusRing(tile, CompLauncher.MaxLaunchDistance); },
            delegate(GlobalTargetInfo target)
            {
                if (!target.IsValid)
                {
                    return null;
                }

                var num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile);
                if (num > CompLauncher.MaxLaunchDistance)
                {
                    GUI.color = Color.red;
                    return num > CompLauncher.MaxLaunchDistanceEverPossible
                        ? "TransportPodDestinationBeyondMaximumRange".Translate()
                        : "TransportPodNotEnoughFuel".Translate();
                }

                var transportPodsFloatMenuOptionsAt = CompLauncher.GetTransportPodsFloatMenuOptionsAt(target.Tile);
                if (!transportPodsFloatMenuOptionsAt.Any())
                {
                    return string.Empty;
                }

                if (transportPodsFloatMenuOptionsAt.Count() == 1)
                {
                    if (transportPodsFloatMenuOptionsAt.First().Disabled)
                    {
                        GUI.color = Color.red;
                    }

                    return transportPodsFloatMenuOptionsAt.First().Label;
                }

                if (target.WorldObject is MapParent mapParent)
                {
                    return "ClickToSeeAvailableOrders_WorldObject".Translate(mapParent.LabelCap); //change
                }

                return "ClickToSeeAvailableOrders_Empty".Translate(); //change here
            });
    }

    private bool ChoseWorldTargetToBomb(GlobalTargetInfo target)
    {
        if (!target.IsValid)
        {
            Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput,
                false);
            return false;
        }

        var num = Find.WorldGrid.TraversalDistanceBetween(parent.Map.Tile, target.Tile);
        if (num > CompLauncher.MaxLaunchDistance)
        {
            Messages.Message(
                "MessageTransportPodsDestinationIsTooFar".Translate(CompLaunchableSRTS
                    .FuelNeededToLaunchAtDist(num, parent.GetComp<CompLaunchableSRTS>().BaseFuelPerTile)
                    .ToString("0.#")), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        if (Find.WorldObjects.AnyMapParentAt(target.Tile))
        {
            var targetMapParent = Find.WorldObjects.MapParentAt(target.Tile);
            if (SRTSArrivalActionBombRun.CanBombSpecificCell(null, targetMapParent))
            {
                var targetMap = targetMapParent.Map;
                Current.Game.CurrentMap = targetMap;
                CameraJumper.TryHideWorld();

                var bombingTargetingParams = new TargetingParameters
                {
                    canTargetLocations = true,
                    canTargetSelf = false,
                    canTargetPawns = false,
                    canTargetFires = true,
                    canTargetBuildings = true,
                    canTargetItems = true,
                    validator = x =>
                        x.Cell.InBounds(targetMap) && (!x.Cell.GetRoof(targetMap)?.isThickRoof ?? true)
                };

                SRTSHelper.targeter.BeginTargeting(bombingTargetingParams,
                    delegate(IEnumerable<IntVec3> cells, Pair<IntVec3, IntVec3> targetPoints)
                    {
                        TryLaunchBombRun(target.Tile, targetPoints, cells, targetMapParent);
                    }, parent.def, bombType, targetMap, null, delegate
                    {
                        if (Find.Maps.Contains(parent.Map))
                        {
                            Current.Game.CurrentMap = parent.Map;
                        }
                    }, Tex2D.LauncherTargeting);
                return true;
            }
        }

        Messages.Message("CannotBombMap".Translate(), MessageTypeDefOf.RejectInput, false);
        return false;
    }

    private void TryLaunchBombRun(PlanetTile destTile, Pair<IntVec3, IntVec3> targetPoints,
        IEnumerable<IntVec3> bombCells,
        MapParent mapParent)
    {
        if (!parent.Spawned)
        {
            Log.Error($"Tried to launch {parent}, but it's unspawned.");
            return;
        }

        if (!CompLauncher.LoadingInProgressOrReadyToLaunch || !CompLauncher.AllInGroupConnectedToFuelingPort ||
            !CompLauncher.AllFuelingPortSourcesInGroupHaveAnyFuel)
        {
            return;
        }

        var map = parent.Map;
        var num = Find.WorldGrid.TraversalDistanceBetween(map.Tile, destTile);
        if (num > CompLauncher.MaxLaunchDistance)
        {
            return;
        }

        CompLauncher.Transporter.TryRemoveLord(map);
        var groupID = CompLauncher.Transporter.groupID;
        var amount =
            Mathf.Max(
                CompLaunchableSRTS.FuelNeededToLaunchAtDist(num, parent.GetComp<CompLaunchableSRTS>().BaseFuelPerTile),
                1f);
        var comp1 = CompLauncher.FuelingPortSource.TryGetComp<CompTransporter>();
        var fuelPortSource = CompLauncher.FuelingPortSource;
        fuelPortSource?.TryGetComp<CompRefuelable>().ConsumeFuel(amount);

        var directlyHeldThings = comp1.GetDirectlyHeldThings();

        var thing = ThingMaker.MakeThing(ThingDef.Named(parent.def.defName));
        thing.SetFactionDirect(Faction.OfPlayer);
        thing.Rotation = CompLauncher.FuelingPortSource.Rotation;
        var comp2 = thing.TryGetComp<CompRefuelable>();
        comp2.GetType().GetField("fuel", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(comp2, fuelPortSource.TryGetComp<CompRefuelable>().Fuel);
        comp2.TargetFuelLevel = fuelPortSource.TryGetComp<CompRefuelable>().TargetFuelLevel;
        thing.stackCount = 1;
        directlyHeldThings.TryAddOrTransfer(thing);

        var ActiveTransporter = (ActiveTransporter)ThingMaker.MakeThing(ThingDef.Named($"{parent.def.defName}_Active"));
        ActiveTransporter.Contents = new ActiveTransporterInfo();
        ActiveTransporter.Contents.innerContainer.TryAddRangeOrTransfer(directlyHeldThings, true, true);

        var srtsLeaving = (SRTSLeaving)SkyfallerMaker.MakeSkyfaller(SkyfallerLeavingDefByRot(), ActiveTransporter);
        srtsLeaving.rotation = CompLauncher.FuelingPortSource.Rotation;
        srtsLeaving.groupID = groupID;
        srtsLeaving.destinationTile = destTile;
        srtsLeaving.arrivalAction = new SRTSArrivalActionBombRun(mapParent, targetPoints, bombCells, bombType, map,
            CompLauncher.FuelingPortSource.Position);

        comp1.CleanUpLoadingVars(map);
        if (fuelPortSource != null)
        {
            var position = fuelPortSource.Position;
            SRTSStatic.SRTSDestroy(fuelPortSource);
            GenSpawn.Spawn(srtsLeaving, position, map);
        }

        CameraJumper.TryHideWorld();
    }

    private ThingDef SkyfallerLeavingDefByRot()
    {
        if (parent.Rotation == Rot4.East)
        {
            return Props.eastSkyfaller;
        }

        return parent.Rotation == Rot4.West ? Props.westSkyfaller : Props.eastSkyfaller;
    }
}