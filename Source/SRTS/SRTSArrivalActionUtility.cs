using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SRTS;

public static class SRTSArrivalActionUtility
{
    public static IEnumerable<FloatMenuOption> GetFloatMenuOptions<T>(
        Func<FloatMenuAcceptanceReport> acceptanceReportGetter, Func<T> arrivalActionGetter, string label,
        CompLaunchableSRTS representative, PlanetTile destinationTile, Caravan car) where T : TransportersArrivalAction
    {
        var rep = acceptanceReportGetter();
        if (!rep.Accepted && rep.FailReason.NullOrEmpty() && rep.FailMessage.NullOrEmpty())
        {
            yield break;
        }

        if (!rep.FailReason.NullOrEmpty())
        {
            yield return new FloatMenuOption($"{label} ({rep.FailReason})", null);
        }
        else
        {
            yield return new FloatMenuOption(label, (Action)(() =>
            {
                var acceptanceReport = acceptanceReportGetter();
                if (acceptanceReport.Accepted)
                {
                    representative.TryLaunch(destinationTile, arrivalActionGetter(), car);
                }
                else
                {
                    if (acceptanceReport.FailMessage.NullOrEmpty())
                    {
                        return;
                    }

                    Messages.Message(acceptanceReport.FailMessage, new GlobalTargetInfo(destinationTile),
                        MessageTypeDefOf.RejectInput, false);
                }
            }));
        }
    }

    public static IEnumerable<FloatMenuOption> GetATKFloatMenuOptions(CompLaunchableSRTS representative,
        IEnumerable<IThingHolder> pods, Settlement settlement, Caravan car)
    {
        var acceptanceReportGetter1 = (Func<FloatMenuAcceptanceReport>)(() =>
            TransportersArrivalAction_AttackSettlement.CanAttack(pods, settlement));
        var arrivalActionGetter1 = (Func<TransportersArrivalAction_AttackSettlement>)(() =>
            new TransportersArrivalAction_AttackSettlement(settlement, PawnsArrivalModeDefOf.EdgeDrop));
        foreach (var floatMenuOption in GetFloatMenuOptions(acceptanceReportGetter1, arrivalActionGetter1,
                     "AttackAndDropAtEdge".Translate(settlement.Label), representative, settlement.Tile, car))
        {
            yield return floatMenuOption;
        }

        var acceptanceReportGetter2 = (Func<FloatMenuAcceptanceReport>)(() =>
            TransportersArrivalAction_AttackSettlement.CanAttack(pods, settlement));
        var arrivalActionGetter2 = (Func<TransportersArrivalAction_AttackSettlement>)(() =>
            new TransportersArrivalAction_AttackSettlement(settlement, PawnsArrivalModeDefOf.CenterDrop));
        foreach (var floatMenuOption in GetFloatMenuOptions(acceptanceReportGetter2, arrivalActionGetter2,
                     "AttackAndDropInCenter".Translate(settlement.Label), representative, settlement.Tile, car))
        {
            yield return floatMenuOption;
        }
    }

    public static IEnumerable<FloatMenuOption> GetGIFTFloatMenuOptions(CompLaunchableSRTS representative,
        IEnumerable<IThingHolder> pods, Settlement settlement, Caravan car)
    {
        if (settlement.Faction == Faction.OfPlayer)
        {
            return [];
        }

        return GetFloatMenuOptions(
            (Func<FloatMenuAcceptanceReport>)(() => TransportersArrivalAction_GiveGift.CanGiveGiftTo(pods, settlement)),
            (Func<TransportersArrivalAction_GiveGift>)(() => new TransportersArrivalAction_GiveGift(settlement)),
            "GiveGiftViaTransportPods".Translate(settlement.Faction.Name,
                FactionGiftUtility.GetGoodwillChange(pods, settlement).ToStringWithSign()), representative,
            settlement.Tile, car);
    }

    public static IEnumerable<FloatMenuOption> GetVisitFloatMenuOptions(CompLaunchableSRTS representative,
        IEnumerable<IThingHolder> pods, Settlement settlement, Caravan car)
    {
        return GetFloatMenuOptions(
            (Func<FloatMenuAcceptanceReport>)(() =>
                TransportersArrivalAction_VisitSettlement.CanVisit(pods, settlement)),
            (Func<TransportersArrivalAction_VisitSettlement>)(() =>
                new TransportersArrivalAction_VisitSettlement(settlement, "VisitSettlement")),
            "VisitSettlement".Translate(settlement.Label), representative, settlement.Tile, car);
    }
}