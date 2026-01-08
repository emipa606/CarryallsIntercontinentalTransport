using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SRTS;

public static class SRTSStatic
{
    public static ThingDef SkyfallerActiveDefByRot(CompLaunchableSRTS comp)
    {
        if (comp.parent.Rotation == Rot4.East)
        {
            return comp.SRTSProps.eastSkyfallerActive;
        }

        return comp.parent.Rotation == Rot4.West ? comp.SRTSProps.westSkyfallerActive : comp.SRTSProps.eastSkyfaller;
    }

    public static IEnumerable<FloatMenuOption> getFM(
        WorldObject wobj,
        IEnumerable<IThingHolder> ih,
        CompLaunchableSRTS comp,
        Caravan car)
    {
        if (wobj is Caravan)
        {
            return [];
        }

        if (wobj is Site site)
        {
            return GetSite(site, ih, comp, car);
        }

        if (wobj is Settlement settlement)
        {
            return GetSettle(settlement, ih, comp, car);
        }

        return wobj is MapParent parent ? GetMapParent(parent, ih, comp, car) : [];
    }

    public static IEnumerable<FloatMenuOption> GetMapParent(
        MapParent mapparent,
        IEnumerable<IThingHolder> pods,
        CompLaunchableSRTS representative,
        Caravan car)
    {
        if (TransportersArrivalAction_LandInSpecificCell.CanLandInSpecificCell(pods, mapparent))
        {
            yield return new FloatMenuOption("LandInExistingMap".Translate(mapparent.Label), (Action)(() =>
            {
                var myMap = car != null ? null : representative.parent.Map;
                Current.Game.CurrentMap = mapparent.Map;
                CameraJumper.TryHideWorld();
                Find.Targeter.BeginTargeting(TargetingParameters.ForDropPodsDestination(),
                    x => representative.TryLaunch(mapparent.Tile,
                        new TransportersArrivalAction_LandInSpecificCell(mapparent, x.Cell), car), null, (Action)(() =>
                    {
                        if (myMap == null || !Find.Maps.Contains(myMap))
                        {
                            return;
                        }

                        Current.Game.CurrentMap = myMap;
                    }), CompLaunchable.TargeterMouseAttachment);
            }));
        }
    }

    public static IEnumerable<FloatMenuOption> GetSite(
        Site site,
        IEnumerable<IThingHolder> pods,
        CompLaunchableSRTS representative,
        Caravan car)
    {
        foreach (var floatMenuOption in GetMapParent(site, pods, representative, car))
        {
            yield return floatMenuOption;
        }

        foreach (var floatMenuOption in GetVisitSite(representative, pods, site, car))
        {
            yield return floatMenuOption;
        }
    }

    public static IEnumerable<FloatMenuOption> GetVisitSite(
        CompLaunchableSRTS representative,
        IEnumerable<IThingHolder> pods,
        Site site,
        Caravan car)
    {
        foreach (var floatMenuOption in SRTSArrivalActionUtility.GetFloatMenuOptions(
                     (Func<FloatMenuAcceptanceReport>)(() => TransportersArrivalAction_VisitSite.CanVisit(pods, site)),
                     (Func<TransportersArrivalAction_VisitSite>)(() =>
                         new TransportersArrivalAction_VisitSite(site, PawnsArrivalModeDefOf.EdgeDrop)),
                     "DropAtEdge".Translate(), representative, site.Tile, car))
        {
            yield return floatMenuOption;
        }

        foreach (var floatMenuOption in SRTSArrivalActionUtility.GetFloatMenuOptions(
                     (Func<FloatMenuAcceptanceReport>)(() => TransportersArrivalAction_VisitSite.CanVisit(pods, site)),
                     (Func<TransportersArrivalAction_VisitSite>)(() =>
                         new TransportersArrivalAction_VisitSite(site, PawnsArrivalModeDefOf.CenterDrop)),
                     "DropInCenter".Translate(), representative, site.Tile, car))
        {
            yield return floatMenuOption;
        }
    }

    public static IEnumerable<FloatMenuOption> GetSettle(
        Settlement bs,
        IEnumerable<IThingHolder> pods,
        CompLaunchableSRTS representative,
        Caravan car)
    {
        foreach (var floatMenuOption in GetMapParent(bs, pods, representative, car))
        {
            yield return floatMenuOption;
        }

        foreach (var visitFloatMenuOption in SRTSArrivalActionUtility.GetVisitFloatMenuOptions(representative, pods, bs,
                     car))
        {
            yield return visitFloatMenuOption;
        }

        /*Uncomment to allow gifting of Ship and contents to faction -SmashPhil*/
        /*foreach (FloatMenuOption giftFloatMenuOption in SRTSArrivalActionUtility.GetGIFTFloatMenuOptions(representative, pods, bs, car))
        {
          FloatMenuOption f2 = giftFloatMenuOption;
          yield return f2;
          f2 = (FloatMenuOption) null;
        }*/
        foreach (var atkFloatMenuOption in SRTSArrivalActionUtility.GetATKFloatMenuOptions(representative, pods, bs,
                     car))
        {
            yield return atkFloatMenuOption;
        }
    }

    public static void SRTSDestroy(Thing thing, DestroyMode mode = DestroyMode.Vanish)
    {
        if (!Thing.allowDestroyNonDestroyable && !thing.def.destroyable)
        {
            Log.Error($"Tried to destroy non-destroyable thing {thing}");
        }
        else if (thing.Destroyed)
        {
            Log.Error($"Tried to destroy already-destroyed thing {thing}");
        }
        else
        {
            _ = thing.Spawned;
            _ = thing.Map;
            if (thing.Spawned)
            {
                thing.DeSpawn(mode);
            }

            var type = typeof(Thing);
            var field = type.GetField("mapIndexOrState", BindingFlags.Instance | BindingFlags.NonPublic);
            sbyte num = -2;
            if (field != null)
            {
                field.SetValue(thing, num);
            }

            if (thing.def.DiscardOnDestroyed)
            {
                thing.Discard();
            }

            thing.holdingOwner?.Notify_ContainedItemDestroyed(thing);
            type.GetMethod("RemoveAllReservationsAndDesignationsOnThis", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(thing, null);
            if (thing is Pawn)
            {
                return;
            }

            thing.stackCount = 0;
        }
    }
}