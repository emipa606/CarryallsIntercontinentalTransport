using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace SRTS;

public class Designator_AddToCarryall : Designator
{
    private readonly CompLaunchableSRTS launchable;

    public Designator_AddToCarryall(CompLaunchableSRTS launchable)
    {
        this.launchable = launchable;
        defaultLabel = "CA_AddToCarryall".Translate();
        icon = ContentFinder<Texture2D>.Get("Misc/AddToCarryall");
        soundDragSustain = SoundDefOf.Designate_DragStandard;
        soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
        useMouseIcon = true;
        soundSucceeded = SoundDefOf.Checkbox_TurnedOff;
        hasDesignateAllFloatMenuOption = true;
    }

    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.Orders;

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        if (!c.InBounds(Map) || c.Fogged(Map))
        {
            return false;
        }

        return c.GetThingList(Map).Any(t => CanDesignateThing(t).Accepted);
    }

    public override AcceptanceReport CanDesignateThing(Thing t)
    {
        if (t is Pawn pawn)
        {
            if (pawn.health?.Downed ?? false)
            {
                return true;
            }

            if (pawn.IsColonist)
            {
                return true;
            }

            if (pawn.IsColonyMechPlayerControlled)
            {
                return true;
            }

            if (pawn.def.race.Animal && pawn.Faction == Faction.OfPlayer && !pawn.InAggroMentalState)
            {
                return true;
            }
        }

        if (t.def.category != ThingCategory.Item)
        {
            return false;
        }

        return !launchable.Transporter.LeftToLoadContains(t);
    }

    public override void DesignateSingleCell(IntVec3 c)
    {
        var thingList = c.GetThingList(Map);
        foreach (var thing in thingList)
        {
            if (CanDesignateThing(thing).Accepted)
            {
                DesignateThing(thing);
            }
        }
    }

    public override void DesignateThing(Thing t)
    {
        if (!launchable.Transporter.LoadingInProgressOrReadyToLaunch)
        {
            TransporterUtility.InitiateLoading(new List<CompTransporter> { launchable.Transporter });
        }

        var leftLoadList = launchable.Transporter.leftToLoad;
        var innerContainerList = launchable.Transporter.innerContainer;
        float currentMass = 0;
        float extraMass = 0;
        if (!leftLoadList.NullOrEmpty())
        {
            currentMass = CollectionsMassCalculator.MassUsage(
                launchable?.Transporter?.leftToLoad?.SelectMany(transferableOneWay => transferableOneWay.things)
                    .ToList(),
                IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, true);
        }

        if (!innerContainerList.NullOrEmpty())
        {
            extraMass = CollectionsMassCalculator.MassUsage(launchable?.Transporter?.innerContainer?.ToList(),
                IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, true);
        }
        //launchable?.Transporter?.innerContainer?.Sum(t => t.def.BaseMass);

        //Log.Message($"CurMass: {currentMass} + {extraMass} + {t.def.BaseMass} = ({currentMass + extraMass + t.def.BaseMass}/{launchable?.Transporter?.MassCapacity})");
        if (currentMass + extraMass + t.def.BaseMass > launchable?.Transporter?.MassCapacity)
        {
            Messages.Message("TooBigTransportersMassUsage".Translate(), MessageTypeDefOf.RejectInput, false);
            return;
        }

        var fobiddable = t.TryGetComp<CompForbiddable>();
        if (fobiddable is { Forbidden: true })
        {
            fobiddable.Forbidden = false;
        }

        launchable?.Transporter?.AddToTheToLoadList(new TransferableOneWay
        {
            things = [t]
        }, t.stackCount);
    }
}