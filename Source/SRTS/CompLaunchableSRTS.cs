using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SRTS;

[StaticConstructorOnStartup]
public class CompLaunchableSRTS : ThingComp
{
    public static readonly Texture2D TargeterMouseAttachment =
        ContentFinder<Texture2D>.Get("UI/Overlays/LaunchableMouseAttachment");

    private static readonly Texture2D LaunchCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip");

    private readonly List<Thing> thingsInsideShip = [];
    private readonly List<Pawn> tmpAllowedPawns = [];
    private Caravan carr;

    public float BaseFuelPerTile => SRTSMod.GetStatFor<float>(parent.def.defName, StatName.fuelPerTile);

    public CompProperties_LaunchableSRTS SRTSProps => (CompProperties_LaunchableSRTS)props;
    public Building FuelingPortSource => (Building)parent;
    public bool ConnectedToFuelingPort => FuelingPortSource != null;

    public bool FuelingPortSourceHasAnyFuel =>
        ConnectedToFuelingPort && FuelingPortSource.GetComp<CompRefuelable>().HasFuel;

    public bool LoadingInProgressOrReadyToLaunch => Transporter.LoadingInProgressOrReadyToLaunch;

    public bool AnythingLeftToLoad => Transporter.AnythingLeftToLoad;

    public Thing FirstThingLeftToLoad => Transporter.FirstThingLeftToLoad;

    public List<CompTransporter> TransportersInGroup => [parent.TryGetComp<CompTransporter>()];

    public bool AnyInGroupHasAnythingLeftToLoad => Transporter.AnyInGroupHasAnythingLeftToLoad;

    public Thing FirstThingLeftToLoadInGroup => Transporter.FirstThingLeftToLoadInGroup;

    public bool AnyInGroupIsUnderRoof
    {
        get
        {
            var transportersInGroup = TransportersInGroup;
            foreach (var compTransporter in transportersInGroup)
            {
                if (compTransporter.parent.Position.Roofed(parent.Map))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public CompTransporter Transporter
    {
        get
        {
            field ??= parent.GetComp<CompTransporter>();

            return field;
        }
    }

    public float FuelingPortSourceFuel => !ConnectedToFuelingPort ? 0.0f : parent.GetComp<CompRefuelable>().Fuel;

    public bool AllInGroupConnectedToFuelingPort => true;

    public bool AllFuelingPortSourcesInGroupHaveAnyFuel => true;

    private float FuelInLeastFueledFuelingPortSource
    {
        get
        {
            var fuelingPortSourceFuel = FuelingPortSourceFuel;

            return fuelingPortSourceFuel;
        }
    }

    public int MaxLaunchDistance
    {
        get
        {
            if (parent.Spawned && !LoadingInProgressOrReadyToLaunch)
            {
                return 0;
            }

            return MaxLaunchDistanceAtFuelLevel(FuelInLeastFueledFuelingPortSource, BaseFuelPerTile);
        }
    }

    public int MaxLaunchDistanceEverPossible
    {
        get
        {
            if (!LoadingInProgressOrReadyToLaunch)
            {
                return 0;
            }

            var num = 0.0f;
            var fuelingPortSource = FuelingPortSource;
            if (fuelingPortSource != null)
            {
                num = Mathf.Max(num, fuelingPortSource.GetComp<CompRefuelable>().Props.fuelCapacity);
            }

            return MaxLaunchDistanceAtFuelLevel(num, BaseFuelPerTile);
        }
    }

    private bool PodsHaveAnyPotentialCaravanOwner
    {
        get
        {
            var transportersInGroup = TransportersInGroup;
            foreach (var compTransporter in transportersInGroup)
            {
                var innerContainer = compTransporter.innerContainer;
                foreach (var thing in innerContainer)
                {
                    if (thing is Pawn pawn && CaravanUtility.IsOwner(pawn, Faction.OfPlayer))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    private Designator_AddToCarryall AddToCarryallDesignator => field ??= new Designator_AddToCarryall(this);

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (parent.Rotation == Rot4.North)
        {
            parent.Rotation = Rot4.East;
        }

        if (parent.Rotation == Rot4.South)
        {
            parent.Rotation = Rot4.West;
        }

        //
        parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Buildings | MapMeshFlagDefOf.Things);
    }

    public void AddThingsToSRTS(Thing thing)
    {
        thingsInsideShip.Add(thing);
    }

    /*
    private void TryLaunch()
    {
        if (SRTSProps.needsConfirmation)
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("CA_ConfirmationScreen".Translate(), DoLaunchAction));
        }
        else
        {
            DoLaunchAction();
        }
    }
    */

    private void DoLaunchAction()
    {
        var num = 0;
        foreach (var t in Transporter.innerContainer)
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

        if (AnyInGroupHasAnythingLeftToLoad)
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "ConfirmSendNotCompletelyLoadedPods".Translate(FirstThingLeftToLoadInGroup.LabelCapNoCount),
                StartChoosingDestination));
        }
        else
        {
            StartChoosingDestination();
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo;
        }

        yield return AddToCarryallDesignator;

        yield return new Command_Action
        {
            defaultLabel = "CA_Rotate".Translate(),
            defaultDesc = "CA_RotateDesc".Translate(),
            icon = ContentFinder<Texture2D>.Get("Misc/Rotate"),
            action = delegate
            {
                parent.Rotation = parent.Rotation.Opposite;
                parent.Map.mapDrawer.MapMeshDirty(parent.Position,
                    MapMeshFlagDefOf.Buildings | MapMeshFlagDefOf.Things);
            }
        };

        if (SRTSProps.hasSelfDestruct)
        {
            yield return new Command_Action
            {
                defaultLabel = "CA_SelfDestruct".Translate(),
                defaultDesc = "CA_SelfDestructDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("Misc/Detonate"),
                action = delegate
                {
                    var exp = parent.TryGetComp<CompExplosive>();
                    if (exp == null)
                    {
                        return;
                    }

                    FleckMaker.ThrowMicroSparks(parent.DrawPos, parent.Map);
                    FireUtility.TryStartFireIn(parent.Position, parent.Map, 2, null);
                    parent.TryAttachFire(100, null);
                }
            };
        }

        if (!LoadingInProgressOrReadyToLaunch)
        {
            yield break;
        }

        var launch = new Command_Action
        {
            defaultLabel = "CommandLaunchGroup".Translate(),
            defaultDesc = "CommandLaunchGroupDesc".Translate(),
            icon = LaunchCommandTex,
            alsoClickIfOtherInGroupClicked = false,
            action = DoLaunchAction
        };
        if (!AllInGroupConnectedToFuelingPort)
        {
            launch.Disable("CommandLaunchGroupFailNotConnectedToFuelingPort".Translate());
        }
        else if (!AllFuelingPortSourcesInGroupHaveAnyFuel)
        {
            launch.Disable("CommandLaunchGroupFailNoFuel".Translate());
        }
        else if (AnyInGroupIsUnderRoof &&
                 !parent.Position.GetThingList(parent.Map).Any(x => x.def.defName == "ShipShuttleBay"))
        {
            launch.Disable("CommandLaunchGroupFailUnderRoof".Translate());
        }

        yield return launch;
    }

    private bool CanAcceptNewPawnToLoad(Pawn pawn)
    {
        var boardingPawnCount = Transporter.leftToLoad?.Sum(t => t.things?.Count(thing => thing is Pawn)) ?? 0;
        var currentlyLoadedPawnCount = Transporter.innerContainer?.Count(t => t is Pawn) ?? 0;

        //Weight condition
        if (pawn == null)
        {
            return boardingPawnCount + currentlyLoadedPawnCount < SRTSProps.maxPassengers;
        }

        var leftLoadList = Transporter.leftToLoad;
        var innerContainerList = Transporter.innerContainer;
        float currentMass = 0;
        float extraMass = 0;
        if (!leftLoadList.NullOrEmpty())
        {
            currentMass = CollectionsMassCalculator.MassUsage(leftLoadList.SelectMany(t => t.things).ToList(),
                IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, true);
        }

        if (!innerContainerList.NullOrEmpty())
        {
            extraMass = CollectionsMassCalculator.MassUsage(innerContainerList.ToList(),
                IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, true);
        }

        if (currentMass + extraMass + pawn.def.BaseMass > Transporter?.MassCapacity)
        {
            return false;
        }

        return boardingPawnCount + currentlyLoadedPawnCount < SRTSProps.maxPassengers;
    }

    private void TryInitPawnLoadingCommand(Pawn pawn)
    {
        if (!LoadingInProgressOrReadyToLaunch)
        {
            TransporterUtility.InitiateLoading(Gen.YieldSingle(Transporter));
        }

        Transporter.AddToTheToLoadList(new TransferableOneWay
        {
            things = [pawn]
        }, 1);
    }

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn pawn)
    {
        if (pawn.Dead || pawn.InMentalState || pawn.Faction != Faction.OfPlayerSilentFail)
        {
            yield break;
        }

        if (!CanAcceptNewPawnToLoad(pawn))
        {
            yield break;
        }

        if (pawn.CanReach(parent, PathEndMode.Touch, Danger.Deadly))
        {
            yield return new FloatMenuOption("BoardSRTS".Translate(parent.Label), delegate
            {
                TryInitPawnLoadingCommand(pawn);
                var job = new Job(JobDefOf.EnterTransporter, parent);
                pawn.jobs.TryTakeOrderedJob(job);
            });
        }
    }


    public override IEnumerable<FloatMenuOption> CompMultiSelectFloatMenuOptions(IEnumerable<Pawn> selPawns)
    {
        foreach (var pawn in selPawns)
        {
            if (pawn.CanReach(parent, PathEndMode.Touch, Danger.Deadly))
            {
                tmpAllowedPawns.Add(pawn);
            }
        }

        if (!tmpAllowedPawns.Any())
        {
            yield return new FloatMenuOption("BoardSRTS".Translate(parent.Label) + " (" + "NoPath".Translate() + ")",
                null);
            yield break;
        }

        if (!CanAcceptNewPawnToLoad(null))
        {
            yield break;
        }

        yield return new FloatMenuOption("BoardSRTS".Translate(parent.Label), delegate
        {
            foreach (var pawn1 in tmpAllowedPawns)
            {
                if (!pawn1.CanReach(parent, PathEndMode.Touch, Danger.Deadly) || pawn1.Downed || pawn1.Dead ||
                    !pawn1.Spawned)
                {
                    continue;
                }

                if (!CanAcceptNewPawnToLoad(pawn1))
                {
                    Messages.Message("CA_WarningSeatsFull".Translate(pawn1.NameFullColored),
                        MessageTypeDefOf.RejectInput, false);
                    continue;
                }

                TryInitPawnLoadingCommand(pawn1);

                var job = JobMaker.MakeJob(JobDefOf.EnterTransporter, parent);
                pawn1.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
        });
    }


    public override string CompInspectStringExtra()
    {
        var sb = new StringBuilder();
        if (!LoadingInProgressOrReadyToLaunch)
        {
        }
        else if (!AllInGroupConnectedToFuelingPort)
        {
            sb.AppendLine("NotReadyForLaunch".Translate() + ": " +
                          "NotAllInGroupConnectedToFuelingPort".Translate() + ".");
        }
        else if (!AllFuelingPortSourcesInGroupHaveAnyFuel)
        {
            sb.AppendLine("NotReadyForLaunch".Translate() + ": " +
                          "NotAllFuelingPortSourcesInGroupHaveAnyFuel".Translate() + ".");
        }
        else if (AnyInGroupHasAnythingLeftToLoad)
        {
            sb.AppendLine("NotReadyForLaunch".Translate() + ": " +
                          "TransportPodInGroupHasSomethingLeftToLoad".Translate() + ".");
        }

        sb.AppendLine("ReadyForLaunch".Translate());
        var container = Transporter.innerContainer.ToList();
        var pawnCount = container.Count(t => t is Pawn);
        var val = CollectionsMassCalculator.MassUsage(container, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload,
            true);
        sb.AppendLine("CA_PassengerCapacity".Translate($"{pawnCount}/{SRTSProps.maxPassengers}"));
        sb.AppendLine("CA_Storage".Translate($"{Math.Round(val, 1)}/{Transporter.MassCapacity}"));
        return sb.ToString().TrimEndNewlines();
    }

    private void StartChoosingDestination()
    {
        CameraJumper.TryJump(CameraJumper.GetWorldTarget(parent));
        Find.WorldSelector.ClearSelection();
        var tile = parent.Map.Tile;
        carr = null;

        /* SOS2 Compatibility Section */
        if (SRTSHelper.SOS2ModLoaded)
        {
            if (parent.Map.Parent.def.defName == "ShipOrbiting")
            {
                Find.WorldTargeter.BeginTargeting(ChoseWorldTarget, true, TargeterMouseAttachment, true, null,
                    delegate(GlobalTargetInfo target)
                    {
                        if (!target.IsValid || parent.TryGetComp<CompRefuelable>() == null ||
                            parent.TryGetComp<CompRefuelable>().FuelPercentOfMax == 1.0f)
                        {
                            return null;
                        }

                        if (target.WorldObject != null &&
                            target.WorldObject.GetType().IsAssignableFrom(SRTSHelper.SpaceSiteType))
                        {
                            /*if (this.parent.TryGetComp<CompRefuelable>().FuelPercentOfMax >= ((SRTSHelper.SpaceSite.worldObjectClass)target.WorldObject).fuelCost / 100f)
                                return null;
                            return "MessageShuttleNeedsMoreFuel".Translate(((SpaceSite)target.WorldObject).fuelCost);*/
                            return null;
                        }

                        return "MessageShuttleMustBeFullyFueled".Translate();
                    });
            }
            else if (parent.Map.Parent.GetType().IsAssignableFrom(SRTSHelper.SpaceSiteType))
            {
                Find.WorldTargeter.BeginTargeting(ChoseWorldTarget, true, TargeterMouseAttachment, true, null,
                    delegate(GlobalTargetInfo target)
                    {
                        if (target.WorldObject == null || target.WorldObject.def != SRTSHelper.SpaceSite &&
                            target.WorldObject.def.defName != "ShipOrbiting")
                        {
                            return "MessageOnlyOtherSpaceSites".Translate();
                        }

                        return null;
                        /*if (this.parent.TryGetComp<CompRefuelable>().FuelPercentOfMax >= ((SpaceSite)this.parent.Map.Parent).fuelCost / 100f)
                            return null;
                        return "MessageShuttleNeedsMoreFuel".Translate(((SpaceSite)this.parent.Map.Parent).fuelCost);*/
                    });
            }
        }

        /* -------------------------- */
        Find.WorldTargeter.BeginTargeting(ChoseWorldTarget, true, TargeterMouseAttachment, true,
            () => GenDraw.DrawWorldRadiusRing(tile, MaxLaunchDistance), target =>
            {
                if (!target.IsValid)
                {
                    return null;
                }

                var num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile);
                if (num > MaxLaunchDistance)
                {
                    GUI.color = Color.red;
                    return num > MaxLaunchDistanceEverPossible
                        ? "TransportPodDestinationBeyondMaximumRange".Translate()
                        : "TransportPodNotEnoughFuel".Translate();
                }

                if (target.WorldObject?.def?.defName == "ShipOrbiting" ||
                    (target.WorldObject?.GetType().IsAssignableFrom(SRTSHelper.SpaceSiteType) ?? false))
                {
                    return null;
                }

                var floatMenuOptionsAt = GetTransportPodsFloatMenuOptionsAt(target.Tile);
                if (!floatMenuOptionsAt.Any())
                {
                    if (Find.WorldGrid[target.Tile].PrimaryBiome.impassable || Find.World.Impassable(target.Tile))
                    {
                        return "MessageTransportPodsDestinationIsInvalid".Translate();
                    }

                    return string.Empty;
                }

                if (floatMenuOptionsAt.Count() != 1)
                {
                    return target.WorldObject is not MapParent worldObject
                        ? "ClickToSeeAvailableOrders_Empty".Translate()
                        : "ClickToSeeAvailableOrders_WorldObject".Translate(worldObject.LabelCap);
                }

                if (floatMenuOptionsAt.First().Disabled)
                {
                    GUI.color = Color.red;
                }

                return floatMenuOptionsAt.First().Label;
            });
    }

    public void WorldStartChoosingDestination(Caravan car)
    {
        CameraJumper.TryJump(CameraJumper.GetWorldTarget((GlobalTargetInfo)(WorldObject)car));
        Find.WorldSelector.ClearSelection();
        var tile = car.Tile;
        carr = car;
        Find.WorldTargeter.BeginTargeting(ChoseWorldTarget, true, TargeterMouseAttachment, false,
            (Action)(() => GenDraw.DrawWorldRadiusRing(car.Tile, MaxLaunchDistance)), target =>
            {
                if (!target.IsValid)
                {
                    return null;
                }

                var num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile);
                if (num > MaxLaunchDistance)
                {
                    GUI.color = Color.red;
                    return num > MaxLaunchDistanceEverPossible
                        ? "TransportPodDestinationBeyondMaximumRange".Translate()
                        : "TransportPodNotEnoughFuel".Translate();
                }

                var floatMenuOptionsAt = GetTransportPodsFloatMenuOptionsAt(target.Tile, car);
                if (!floatMenuOptionsAt.Any())
                {
                    if (Find.WorldGrid[target.Tile].PrimaryBiome.impassable || Find.World.Impassable(target.Tile))
                    {
                        return "MessageTransportPodsDestinationIsInvalid".Translate();
                    }

                    return string.Empty;
                }

                if (floatMenuOptionsAt.Count() != 1)
                {
                    return target.WorldObject is not MapParent worldObject
                        ? "ClickToSeeAvailableOrders_Empty".Translate()
                        : "ClickToSeeAvailableOrders_WorldObject".Translate(worldObject.LabelCap);
                }

                if (floatMenuOptionsAt.First().Disabled)
                {
                    GUI.color = Color.red;
                }

                return floatMenuOptionsAt.First().Label;
            });
    }

    private bool ChoseWorldTarget(GlobalTargetInfo target)
    {
        if (carr == null && !LoadingInProgressOrReadyToLaunch)
        {
            return true;
        }

        if (!target.IsValid)
        {
            Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput,
                false);
            return false;
        }

        var num = Find.WorldGrid.TraversalDistanceBetween(carr?.Tile ?? parent.Map.Tile, target.Tile);
        if (num > MaxLaunchDistance)
        {
            Messages.Message(
                "MessageTransportPodsDestinationIsTooFar".Translate(FuelNeededToLaunchAtDist(num, BaseFuelPerTile)
                    .ToString("0.#")), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        if ((Find.WorldGrid[target.Tile].PrimaryBiome.impassable || Find.World.Impassable(target.Tile)) &&
            (!SRTSHelper.SOS2ModLoaded || target.WorldObject?.def?.defName != "ShipOrbiting"))
        {
            Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput,
                false);
            return false;
        }

        if (SRTSHelper.SOS2ModLoaded && target.WorldObject?.def?.defName == "ShipOrbiting")
        {
            if (!SRTSMod.GetStatFor<bool>(parent.def.defName, StatName.spaceFaring))
            {
                Messages.Message("NonSpaceFaringSRTS".Translate(parent.def.defName), MessageTypeDefOf.RejectInput,
                    false);
                return false;
            }

            if (SRTSMod.GetStatFor<bool>(parent.def.defName, StatName.shuttleBayLanding))
            {
                var shuttleBayPos = (IntVec3)AccessTools.Method(SRTSHelper.SOS2LaunchableType, "FirstShuttleBayOpen")
                    .Invoke(null, [(target.WorldObject as MapParent)?.Map]);
                if (shuttleBayPos == IntVec3.Zero)
                {
                    Messages.Message("NeedOpenShuttleBay".Translate(), MessageTypeDefOf.RejectInput);
                    return false;
                }

                ConfirmLaunch(target.Tile,
                    new TransportersArrivalAction_LandInSpecificCell((target.WorldObject as MapParent)?.Map.Parent,
                        shuttleBayPos));
                return true;
            }
        }

        var floatMenuOptionsAt = GetTransportPodsFloatMenuOptionsAt(target.Tile, carr);
        if (!floatMenuOptionsAt.Any())
        {
            if (Find.WorldGrid[target.Tile].PrimaryBiome.impassable || Find.World.Impassable(target.Tile))
            {
                Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput,
                    false);
                return false;
            }

            ConfirmLaunch(target.Tile, null);
            return true;
        }

        if (floatMenuOptionsAt.Count() == 1)
        {
            if (!floatMenuOptionsAt.First().Disabled)
            {
                floatMenuOptionsAt.First().action();
            }

            return false;
        }

        Find.WindowStack.Add(new FloatMenu(floatMenuOptionsAt.ToList()));
        return false;
    }

    private void ConfirmLaunch(int destinationTile, TransportersArrivalAction arrivalAction, Caravan cafr = null)
    {
        var map = parent?.MapHeld;
        if (map != null)
        {
            var dist = Find.WorldGrid.TraversalDistanceBetween(map.Tile, destinationTile);
            if (dist >= SRTSMod.mod.settings.confirmDistance)
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("CA_ConfirmationScreen".Translate(),
                    () => TryLaunch(destinationTile, arrivalAction, cafr)));
                return;
            }
        }

        //
        TryLaunch(destinationTile, arrivalAction, cafr);
    }

    public void TryLaunch(int destinationTile, TransportersArrivalAction arrivalAction, Caravan cafr = null)
    {
        if (cafr == null && !parent.Spawned)
        {
            Log.Error($"Tried to launch {parent}, but it's unspawned.");
        }
        else
        {
            if (parent.Spawned && !LoadingInProgressOrReadyToLaunch || !AllInGroupConnectedToFuelingPort ||
                !AllFuelingPortSourcesInGroupHaveAnyFuel)
            {
                return;
            }

            if (cafr == null)
            {
                var map = parent.Map;
                var num = Find.WorldGrid.TraversalDistanceBetween(map.Tile, destinationTile);
                if (num > MaxLaunchDistance)
                {
                    return;
                }

                Transporter.TryRemoveLord(map);
                var groupId = Transporter.groupID;
                var amount = Mathf.Max(FuelNeededToLaunchAtDist(num, BaseFuelPerTile), 1f);
                var comp1 = FuelingPortSource.TryGetComp<CompTransporter>();
                var fuelingPortSource = FuelingPortSource;
                fuelingPortSource?.TryGetComp<CompRefuelable>().ConsumeFuel(amount);

                var directlyHeldThings = comp1.GetDirectlyHeldThings();

                // Neceros Edit
                var thing = ThingMaker.MakeThing(ThingDef.Named(parent.def.defName));
                thing.SetFactionDirect(Faction.OfPlayer);
                thing.Rotation = FuelingPortSource.Rotation;
                var comp2 = thing.TryGetComp<CompRefuelable>();
                comp2.GetType().GetField("fuel", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(comp2, fuelingPortSource.TryGetComp<CompRefuelable>().Fuel);
                comp2.TargetFuelLevel = fuelingPortSource.TryGetComp<CompRefuelable>().TargetFuelLevel;
                thing.stackCount = 1;
                thing.HitPoints = parent.HitPoints;
                directlyHeldThings.TryAddOrTransfer(thing);

                // Neceros Edit
                var ActiveTransporter =
                    (ActiveTransporter)ThingMaker.MakeThing(SRTSStatic.SkyfallerActiveDefByRot(this));
                ActiveTransporter.HitPoints = parent.HitPoints;
                ActiveTransporter.Contents = new ActiveTransporterInfo();
                ActiveTransporter.Contents.innerContainer.TryAddRangeOrTransfer(directlyHeldThings, true, true);

                // Neceros Edit
                var srtsLeaving =
                    (SRTSLeaving)SkyfallerMaker.MakeSkyfaller(SkyfallerLeavingDefByRot(), ActiveTransporter);
                srtsLeaving.rotation = FuelingPortSource.Rotation;
                srtsLeaving.groupID = groupId;
                srtsLeaving.destinationTile = destinationTile;
                srtsLeaving.arrivalAction = arrivalAction;
                comp1.CleanUpLoadingVars(map);
                if (fuelingPortSource != null)
                {
                    var position = fuelingPortSource.Position;
                    SRTSStatic.SRTSDestroy(fuelingPortSource);
                    GenSpawn.Spawn(srtsLeaving, position, map);
                }

                CameraJumper.TryHideWorld();
            }
            else
            {
                var num = Find.WorldGrid.TraversalDistanceBetween(carr.Tile, destinationTile);
                if (num > MaxLaunchDistance)
                {
                    return;
                }

                var amount = Mathf.Max(FuelNeededToLaunchAtDist(num, BaseFuelPerTile), 1f);
                FuelingPortSource?.TryGetComp<CompRefuelable>().ConsumeFuel(amount);

                var directlyHeldThings = (ThingOwner<Pawn>)cafr.GetDirectlyHeldThings();
                Thing thing = null;
                foreach (var pawn in directlyHeldThings.InnerListForReading)
                {
                    var inventory = pawn.inventory;
                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (var index = 0; index < inventory.innerContainer.Count; index++)
                    {
                        var item = inventory.innerContainer[index];
                        // Neceros Edit
                        if (item.TryGetComp<CompLaunchableSRTS>() == null)
                        {
                            continue;
                        }

                        thing = item;
                        item.holdingOwner.Remove(item);
                        break;
                    }
                }

                /*Add caravan items to SRTS - SmashPhil */
                foreach (var p in directlyHeldThings.InnerListForReading)
                {
                    p.inventory.innerContainer.InnerListForReading.ForEach(AddThingsToSRTS);
                    p.inventory.innerContainer.Clear();
                }

                var thingOwner = new ThingOwner<Thing>();
                foreach (var pawn in directlyHeldThings.AsEnumerable<Pawn>().ToList())
                {
                    thingOwner.TryAddOrTransfer(pawn);
                }

                if (thing is { holdingOwner: null })
                {
                    thingOwner.TryAddOrTransfer(thing, false);
                }

                // Neceros Edit
                var ActiveTransporter =
                    (ActiveTransporter)ThingMaker.MakeThing(SRTSStatic.SkyfallerActiveDefByRot(this));
                ActiveTransporter.Contents = new ActiveTransporterInfo();
                ActiveTransporter.Contents.innerContainer.TryAddRangeOrTransfer(thingOwner, true, true);
                ActiveTransporter.Contents.innerContainer.TryAddRangeOrTransfer(thingsInsideShip, true, true);
                thingsInsideShip.Clear();

                cafr.RemoveAllPawns();
                if (!cafr.Destroyed)
                {
                    cafr.Destroy();
                }

                var travelingTransportPods =
                    (TravelingSRTS)WorldObjectMaker.MakeWorldObject(StaticDefOf.TravelingSRTS_Carryall);
                travelingTransportPods.Tile = cafr.Tile;
                travelingTransportPods.SetFaction(Faction.OfPlayer);
                travelingTransportPods.destinationTile = destinationTile;
                travelingTransportPods.arrivalAction = arrivalAction;
                travelingTransportPods.flyingThing = parent;
                Find.WorldObjects.Add(travelingTransportPods);
                travelingTransportPods.AddTransporter(ActiveTransporter.Contents, true);
                ActiveTransporter.Contents = null;
                ActiveTransporter.Destroy();
                Find.WorldTargeter.StopTargeting();
            }
        }
    }

    private ThingDef SkyfallerLeavingDefByRot()
    {
        if (parent.Rotation == Rot4.East)
        {
            return SRTSProps.eastSkyfaller;
        }

        return parent.Rotation == Rot4.West ? SRTSProps.westSkyfaller : SRTSProps.eastSkyfaller;
    }

    public void Notify_FuelingPortSourceDeSpawned()
    {
        if (!Transporter.CancelLoad())
        {
            return;
        }

        Messages.Message("MessageTransportersLoadCanceled_FuelingPortGiverDeSpawned".Translate(), (Thing)parent,
            MessageTypeDefOf.NegativeEvent);
    }

    public static int MaxLaunchDistanceAtFuelLevel(float fuelLevel, float costPerTile)
    {
        return Mathf.FloorToInt(fuelLevel / costPerTile);
    }

    public static float FuelNeededToLaunchAtDist(float dist, float cost)
    {
        return cost * dist;
    }

    public IEnumerable<FloatMenuOption> GetTransportPodsFloatMenuOptionsAt(int tile, Caravan car = null)
    {
        var anything = false;
        var pods = TransportersInGroup.Cast<IThingHolder>();
        if (car != null)
        {
            var rliss = new List<Caravan> { car };
            pods = rliss;
        }

        if (car == null)
        {
            if (TransportersArrivalAction_FormCaravan.CanFormCaravanAt(pods, tile) &&
                !Find.WorldObjects.AnySettlementBaseAt(tile) && !Find.WorldObjects.AnySiteAt(tile))
            {
                anything = true;
                yield return new FloatMenuOption("FormCaravanHere".Translate(),
                    () => ConfirmLaunch(tile, new TransportersArrivalAction_FormCaravan()));
            }
        }
        else if (!Find.WorldObjects.AnySettlementBaseAt(tile) && !Find.WorldObjects.AnySiteAt(tile) &&
                 !Find.World.Impassable(tile))
        {
            anything = true;
            yield return new FloatMenuOption("FormCaravanHere".Translate(),
                () => ConfirmLaunch(tile, new TransportersArrivalAction_FormCaravan(), car));
        }

        var worldObjects = Find.WorldObjects.AllWorldObjects;
        foreach (var worldObject in worldObjects)
        {
            if (worldObject.Tile != tile)
            {
                continue;
            }

            var nowre = SRTSStatic.getFM(worldObject, pods, this, car);
            if (nowre.ToList().Count < 1)
            {
                yield return new FloatMenuOption("FormCaravanHere".Translate(),
                    () => ConfirmLaunch(tile,
                        new TransportersArrivalAction_FormCaravan(), car));
            }
            else
            {
                foreach (var floatMenuOption in nowre)
                {
                    anything = true;
                    yield return floatMenuOption;
                }
            }
        }

        if (!anything && !Find.World.Impassable(tile))
        {
            yield return new FloatMenuOption("TransportPodsContentsWillBeLost".Translate(),
                () => ConfirmLaunch(tile, null));
        }
    }
}