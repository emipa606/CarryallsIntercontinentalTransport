using System.Text;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SRTS.Patches;

[HarmonyPatch(typeof(CollectionsMassCalculator), nameof(CollectionsMassCalculator.CapacityLeftAfterTradeableTransfer))]
public static class CollectionsMassCalculator_CapacityLeftAfterTradeableTransfer_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(List<Thing> allCurrentThings, List<Tradeable> tradeables, StringBuilder explanation, ref float __result)
    {
        return StartUp.SRTSMassCapacityCaravan(allCurrentThings, tradeables, explanation, ref __result);
    }
}
