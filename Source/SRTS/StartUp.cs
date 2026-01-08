using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace SRTS;

[StaticConstructorOnStartup]
public static class StartUp
{
    static StartUp()
    {
        var harmony = new Harmony("SRTSExpanded.smashphil.neceros");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        //Harmony.DEBUG = true;
    }

    /// <summary>
    ///     Insert all items on map (non minifiable) if map is not player home.
    /// </summary>
    /// <param name="instructions"></param>
    /// <param name="ilg"></param>
    /// <returns></returns>
    public static IEnumerable<CodeInstruction> AddItemsEntireMapNonHomeTranspiler(
        IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
    {
        var instructionList = instructions.ToList();

        foreach (var instruction in instructionList)
        {
            if (SRTSMod.mod.settings.displayHomeItems && instruction.opcode == OpCodes.Call &&
                (MethodInfo)instruction.operand == AccessTools.Method(typeof(CaravanFormingUtility),
                    nameof(CaravanFormingUtility.AllReachableColonyItems)))
            {
                var label = ilg.DefineLabel();

                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld,
                    AccessTools.Field(typeof(Dialog_LoadTransporters), "transporters"));
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(SRTSHelper), nameof(SRTSHelper.SRTSLauncherSelected)));
                yield return new CodeInstruction(OpCodes.Brfalse, label);

                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld,
                    AccessTools.Field(typeof(Dialog_LoadTransporters), "map"));
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(SRTSHelper), nameof(SRTSHelper.SRTSNonPlayerHomeMap)));
                yield return new CodeInstruction(OpCodes.Brfalse, label);

                yield return new CodeInstruction(OpCodes.Pop);
                yield return new CodeInstruction(OpCodes.Pop);
                yield return new CodeInstruction(OpCodes.Pop);

                yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                yield return new CodeInstruction(OpCodes.Ldc_I4_0);

                instruction.labels.Add(label);
            }

            yield return instruction;
        }
    }

    /// <summary>
    ///     Add purchased items to list of things contained within SRTS, to drop contents on landing rather than placing inside
    ///     pawn's inventory.
    /// </summary>
    /// <param name="instructions"></param>
    /// <param name="ilg"></param>
    /// <returns></returns>
    public static IEnumerable<CodeInstruction> GiveSoldThingsToSRTSTranspiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator ilg)
    {
        var instructionList = instructions.ToList();

        for (var i = 0; i < instructionList.Count; i++)
        {
            var instruction = instructionList[i];

            if (instruction.opcode == OpCodes.Stloc_2)
            {
                yield return instruction;
                instruction = instructionList[++i];

                var label = ilg.DefineLabel();
                yield return new CodeInstruction(OpCodes.Ldloc_0);
                yield return new CodeInstruction(OpCodes.Ldloc_1);
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(SRTSHelper), nameof(SRTSHelper.AddToSRTSFromCaravan)));
                instruction.labels.Add(label);
            }

            yield return instruction;
        }
    }

    /// <summary>
    ///     Draw SRTS textures dynamically to mimic both the flying SRTS texture and its rotation
    /// </summary>
    /// <param name="instructions"></param>
    /// <param name="ilg"></param>
    /// <returns></returns>
    public static IEnumerable<CodeInstruction> DrawDynamicSRTSObjectsTranspiler(
        IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
    {
        var instructionList = instructions.ToList();

        // TransitionPct is a method call (not a property getter)
        var transitionPctMethod = AccessTools.Method(typeof(ExpandableWorldObjectsUtility),
            nameof(ExpandableWorldObjectsUtility.TransitionPct), [typeof(WorldObject)]);

        for (var i = 0; i < instructionList.Count; i++)
        {
            var instruction = instructionList[i];

            if (transitionPctMethod != null && instruction.Calls(transitionPctMethod))
            {
                var label = ilg.DefineLabel();
                var brlabel = ilg.DefineLabel();

                yield return new CodeInstruction(OpCodes.Ldloc_1);
                yield return new CodeInstruction(OpCodes.Isinst, typeof(TravelingSRTS));
                yield return new CodeInstruction(OpCodes.Brfalse, label);

                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Property(typeof(SRTSHelper), nameof(SRTSHelper.DynamicTexturesEnabled)).GetGetMethod());
                yield return new CodeInstruction(OpCodes.Brfalse, label);

                yield return new CodeInstruction(OpCodes.Ldloc_1);
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(TravelingSRTS), nameof(TravelingSRTS.Draw)));
                yield return new CodeInstruction(OpCodes.Leave, brlabel);

                var j = i;
                while (j < instructionList.Count)
                {
                    if (instructionList[j].opcode == OpCodes.Ldloca_S)
                    {
                        instructionList[j].labels.Add(brlabel);
                        break;
                    }

                    j++;
                }

                instruction.labels.Add(label);
            }

            yield return instruction;
        }
    }

    /// <summary>
    ///     Expanding Icon dynamic drawer for SRTS dynamic textures
    /// </summary>
    /// <param name="instructions"></param>
    /// <param name="ilg"></param>
    /// <returns></returns>
    public static IEnumerable<CodeInstruction> ExpandableIconDetourSRTSTranspiler(
        IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
    {
        var instructionList = instructions.ToList();
        var jumpLabel = ilg.DefineLabel();

        for (var i = 0; i < instructionList.Count; i++)
        {
            var instruction = instructionList[i];

            if (instruction.opcode == OpCodes.Ldloc_2 && instructionList[i + 1].opcode == OpCodes.Ldc_I4_1)
            {
                instruction.labels.Add(jumpLabel);
            }

            if (instruction.Calls(AccessTools.Property(typeof(WorldObject), nameof(WorldObject.ExpandingIconColor))
                    .GetGetMethod()))
            {
                var label = ilg.DefineLabel();

                yield return new CodeInstruction(OpCodes.Ldloc_3);
                yield return new CodeInstruction(OpCodes.Isinst, typeof(TravelingSRTS));
                yield return new CodeInstruction(OpCodes.Brfalse, label);

                yield return new CodeInstruction(OpCodes.Pop);
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Property(typeof(SRTSHelper), nameof(SRTSHelper.DynamicTexturesEnabled)).GetGetMethod());
                yield return new CodeInstruction(OpCodes.Brfalse, label);

                yield return new CodeInstruction(OpCodes.Ldloc_3);
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(TravelingSRTS), nameof(TravelingSRTS.Draw)));
                yield return new CodeInstruction(OpCodes.Br, jumpLabel);

                instruction.labels.Add(label);
            }

            yield return instruction;
        }
    }

    public static void TravelingSRTSChangeDirection(List<WorldObject> ___selected)
    {
        /* placeholder */
    }

    public static bool CustomTravelSpeedSRTS(PlanetTile ___initialTile, PlanetTile ___destinationTile,
        List<ActiveTransporterInfo> ___transporters, ref float __result)
    {
        if (!___transporters.Any(x => x.innerContainer.Any(y => y.TryGetComp<CompLaunchableSRTS>() != null)))
        {
            return true;
        }

        var start = Find.WorldGrid.GetTileCenter(___initialTile);
        var end = Find.WorldGrid.GetTileCenter(___destinationTile);

        if (start == end)
        {
            __result = 1f;
            return false;
        }

        var num = GenMath.SphericalDistance(start.normalized, end.normalized) * 100000;
        if (num == 0f)
        {
            __result = 1f;
            return false;
        }

        var ship = ___transporters
            .Find(x => x.innerContainer.First(y => y.TryGetComp<CompLaunchableSRTS>() != null) != null)
            .innerContainer.First(z => z.TryGetComp<CompLaunchableSRTS>() != null);
        __result = SRTSMod.GetStatFor<float>(ship.def.defName, StatName.flightSpeed) / num;
        return false;
    }

    public static bool SRTSMassCapacityCaravan(List<Thing> allCurrentThings, List<Tradeable> tradeables,
        StringBuilder explanation, ref float __result)
    {
        if (!allCurrentThings.Any(x => x.TryGetComp<CompLaunchableSRTS>() != null))
        {
            return true;
        }

        var srts = allCurrentThings.First(x => x.TryGetComp<CompLaunchableSRTS>() != null);
        __result = SRTSMod.GetStatFor<float>(srts.def.defName, StatName.massCapacity);
        return false;
    }

    public static IEnumerable<CodeInstruction> SRTSMassUsageCaravanTranspiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator ilg)
    {
        var list = instructions.ToList();
        var target = AccessTools.Method(typeof(CollectionsMassCalculator),
            nameof(CollectionsMassCalculator.MassUsageLeftAfterTradeableTransfer));
        var getter = AccessTools.Property(typeof(SRTSHelper), nameof(SRTSHelper.SRTSInCaravan)).GetGetMethod();

        for (var i = 0; i < list.Count; i++)
        {
            var instruction = list[i];

            if (instruction.opcode == OpCodes.Ldc_I4_0 &&
                i + 3 < list.Count &&
                list[i + 1].opcode == OpCodes.Ldc_I4_0 &&
                list[i + 2].opcode == OpCodes.Ldc_I4_0 &&
                list[i + 3].Calls(target))
            {
                yield return list[i];
                yield return new CodeInstruction(OpCodes.Call, getter);
                yield return list[i + 2];
                i += 2;
                continue;
            }

            yield return instruction;
        }
    }

    public static void NoLaunchGroupForSRTS(ref IEnumerable<Gizmo> __result, CompTransporter __instance)
    {
        if (__instance.parent.def.GetCompProperties<CompProperties_LaunchableSRTS>() == null)
        {
            return;
        }

        var gizmos = __result.ToList();
        for (var i = gizmos.Count - 1; i >= 0; i--)
        {
            if ((gizmos[i] as Command_Action)?.defaultLabel == "CommandSelectPreviousTransporter".Translate() ||
                (gizmos[i] as Command_Action)?.defaultLabel == "CommandSelectAllTransporters".Translate() ||
                (gizmos[i] as Command_Action)?.defaultLabel == "CommandSelectNextTransporter".Translate())
            {
                gizmos.Remove(gizmos[i]);
            }
        }

        __result = gizmos;
    }

    public static bool DropSRTSExactSpot(List<ActiveTransporterInfo> transporters, IntVec3 near, Map map)
    {
        foreach (var pod in transporters)
        {
            foreach (var t in pod.innerContainer)
            {
                if (DefDatabase<ThingDef>.GetNamedSilentFail(t.def.defName.Split('_')[0])
                        ?.GetCompProperties<CompProperties_LaunchableSRTS>() == null)
                {
                    continue;
                }

                TransportersArrivalActionUtility.RemovePawnsFromWorldPawns(transporters);
                foreach (var pod2 in transporters)
                {
                    DropPodUtility.MakeDropPodAt(near, map, pod2);
                }

                return false;
            }
        }

        return true;
    }

    public static void DrawBombingTargeter()
    {
        SRTSHelper.targeter.TargeterOnGUI();
    }

    public static void ProcessBombingInputEvents()
    {
        SRTSHelper.targeter.ProcessInputEvents();
    }

    public static void BombTargeterUpdate()
    {
        SRTSHelper.targeter.TargeterUpdate();
    }

    public static bool CustomSRTSMassCapacity(ref float __result, List<CompTransporter> ___transporters)
    {
        if (!___transporters.Any(x => x.parent.TryGetComp<CompLaunchableSRTS>() != null))
        {
            return true;
        }

        var num = 0f;
        foreach (var comp in ___transporters)
        {
            num += SRTSMod.GetStatFor<float>(comp.parent.def.defName, StatName.massCapacity);
        }

        __result = num;
        return false;
    }

    public static bool ResearchCostApparent(ResearchProjectDef __instance, ref float __result)
    {
        if (SRTSHelper.srtsDefProjects.All(x => x.Value != __instance))
        {
            return true;
        }

        __result = SRTSHelper.GetResearchStat(__instance) * __instance.CostFactor(Faction.OfPlayer.def.techLevel);
        return false;
    }

    public static bool ResearchIsFinished(ResearchProjectDef __instance, ref bool __result)
    {
        if (SRTSHelper.srtsDefProjects.All(x => x.Value != __instance))
        {
            return true;
        }

        __result = __instance.ProgressReal >= SRTSHelper.GetResearchStat(__instance);
        return false;
    }

    public static bool ResearchProgressPercent(ResearchProjectDef __instance, ref float __result)
    {
        if (SRTSHelper.srtsDefProjects.All(x => x.Value != __instance))
        {
            return true;
        }

        __result = Find.ResearchManager.GetProgress(__instance) / SRTSHelper.GetResearchStat(__instance);
        return false;
    }

    public static IEnumerable<CodeInstruction> ResearchFinishProjectTranspiler(
        IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
    {
        var instructionList = instructions.ToList();

        var prerequisitesLabel = ilg.DefineLabel();
        yield return new CodeInstruction(OpCodes.Ldarg_1);
        yield return new CodeInstruction(OpCodes.Call,
            AccessTools.Method(typeof(SRTSHelper), nameof(SRTSHelper.ContainedInDefProjects)));
        yield return new CodeInstruction(OpCodes.Brfalse, prerequisitesLabel);

        yield return new CodeInstruction(OpCodes.Ldarg_1);
        yield return new CodeInstruction(OpCodes.Ldarg_0);
        yield return new CodeInstruction(OpCodes.Call,
            AccessTools.Method(typeof(SRTSHelper), nameof(SRTSHelper.FinishCustomPrerequisites)));

        yield return new CodeInstruction(OpCodes.Nop) { labels = [prerequisitesLabel] };

        for (var i = 0; i < instructionList.Count; i++)
        {
            var instruction = instructionList[i];

            if (instruction.opcode == OpCodes.Ldfld && (FieldInfo)instruction.operand ==
                AccessTools.Field(typeof(ResearchManager), "progress"))
            {
                yield return instruction;
                instruction = instructionList[++i];

                var label = ilg.DefineLabel();
                var brlabel = ilg.DefineLabel();

                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(SRTSHelper), nameof(SRTSHelper.ContainedInDefProjects)));
                yield return new CodeInstruction(OpCodes.Brfalse, label);

                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(SRTSHelper), nameof(SRTSHelper.GetResearchStat)));
                yield return new CodeInstruction(OpCodes.Br, brlabel);

                var x = i;
                while (x < instructionList.Count)
                {
                    if (instructionList[x].opcode == OpCodes.Callvirt)
                    {
                        instructionList[x].labels.Add(brlabel);
                        break;
                    }

                    x++;
                }

                instruction.labels.Add(label);
            }

            yield return instruction;
        }
    }

    public static IEnumerable<CodeInstruction> ResearchTranslatedCostTranspiler(
        IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
    {
        var instructionList = instructions.ToList();
        for (var i = 0; i < instructionList.Count; i++)
        {
            var instruction = instructionList[i];

            if (instruction.opcode == OpCodes.Ldflda && (FieldInfo)instruction.operand ==
                AccessTools.Field(typeof(ResearchProjectDef), "baseCost"))
            {
                var label = ilg.DefineLabel();
                var brlabel = ilg.DefineLabel();

                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld,
                    AccessTools.Field(typeof(MainTabWindow_Research), "selectedProject"));
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(SRTSHelper), nameof(SRTSHelper.ContainedInDefProjects)));
                yield return new CodeInstruction(OpCodes.Brfalse, label);

                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(SRTSHelper), nameof(SRTSHelper.GetResearchStatString)));
                yield return new CodeInstruction(OpCodes.Br, brlabel);

                instruction.labels.Add(label);
                instructionList[i + 4].labels.Add(brlabel);
            }

            yield return instruction;
        }
    }

    public static void ResearchFinishAllSRTS(ResearchManager __instance,
        ref Dictionary<ResearchProjectDef, float> ___progress)
    {
        foreach (var researchProjectDef in SRTSHelper.srtsDefProjects.Values)
        {
            ___progress[researchProjectDef] = SRTSHelper.GetResearchStat(researchProjectDef);
        }

        __instance.ReapplyAllMods();
    }

    public static void CustomPrerequisitesCompleted(ResearchProjectDef __instance, ref bool __result,
        List<ResearchProjectDef> ___prerequisites)
    {
        if (!SRTSHelper.ContainedInDefProjects(__instance) || ___prerequisites == null || __result is false)
        {
            return;
        }

        var projects = SRTSMod.mod.settings
            .defProperties[SRTSHelper.srtsDefProjects.FirstOrDefault(x => x.Value == __instance).Key.defName]
            .CustomResearch;
        foreach (var proj in projects)
        {
            if (!proj.IsFinished)
            {
                __result = false;
            }
        }
    }

    public static IEnumerable<CodeInstruction> DrawCustomResearchTranspiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator ilg)
    {
        var instructionList = instructions.ToList();
        var foundPlace = true;
        foreach (var instruction in instructionList)
        {
            if (instruction.Calls(AccessTools.Method(typeof(MainTabWindow_Research), "PosX")) && foundPlace)
            {
                foundPlace = false;
                var label = ilg.DefineLabel();

                yield return new CodeInstruction(OpCodes.Ldloc_S, 14);
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(SRTSHelper), nameof(SRTSHelper.ContainedInDefProjects)));
                yield return new CodeInstruction(OpCodes.Brfalse, label);

                yield return new CodeInstruction(OpCodes.Ldloc_S, 14);
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.PropertyGetter(typeof(MainTabWindow_Research), "CurTab"));
                yield return new CodeInstruction(OpCodes.Ldloc_S, 3);
                yield return new CodeInstruction(OpCodes.Ldloc_S, 4);
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld,
                    AccessTools.Field(typeof(MainTabWindow_Research), "selectedProject"));
                yield return new CodeInstruction(OpCodes.Ldloc_S, 13);
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(SRTSHelper), nameof(SRTSHelper.DrawLinesCustomPrerequisites)));


                instruction.labels.Add(label);
            }

            yield return instruction;
        }
    }

    public static void DrawCustomResearchPrereqs(ResearchProjectDef project, Rect rect, ref float __result)
    {
        if (!SRTSHelper.ContainedInDefProjects(project))
        {
            return;
        }

        var projects = SRTSMod.mod.settings
            .defProperties[SRTSHelper.srtsDefProjects.FirstOrDefault(x => x.Value == project).Key.defName]
            .CustomResearch;
        var yMin = rect.yMin;
        rect.yMin += rect.height;
        var oldResult = __result;
        foreach (var proj in projects)
        {
            if (!project.IsFinished)
            {
                GUI.color = proj.IsFinished ? Color.green : Color.red;
            }

            Widgets.LabelCacheHeight(ref rect, "  " + proj.LabelCap);
            rect.yMin += rect.height;
        }

        GUI.color = Color.white;
        __result = rect.yMin - yMin + oldResult;
    }

    public static IEnumerable<Gizmo> LaunchAndBombGizmosPassthrough(IEnumerable<Gizmo> __result, Caravan __instance)
    {
        var enumerator = __result.GetEnumerator();
        using var disposable = (IDisposable)enumerator;
        while (enumerator.MoveNext())
        {
            var element = enumerator.Current;
            yield return element;
            if ((element as Command_Action)?.defaultLabel != "CommandSettle".Translate() ||
                !__instance.PawnsListForReading.Any(x =>
                    x.inventory.innerContainer.Any(y => y.TryGetComp<CompLaunchableSRTS>() != null)))
            {
                continue;
            }

            var massUsage = 0f;
            Thing srts = null;
            foreach (var p in __instance.PawnsListForReading)
            {
                foreach (var t in p.inventory?.innerContainer!)
                {
                    if (t.TryGetComp<CompLaunchableSRTS>() != null)
                    {
                        srts = t;
                    }
                    else
                    {
                        massUsage += t.GetStatValue(StatDefOf.Mass) * t.stackCount;
                    }
                }

                massUsage += p.GetStatValue(StatDefOf.Mass);
                massUsage -= MassUtility.InventoryMass(p) * p.stackCount;
            }

            yield return new Command_Action
            {
                defaultLabel = "CommandLaunchGroup".Translate(),
                defaultDesc = "CommandLaunchGroupDesc".Translate(),
                icon = Tex2D.LaunchSRTS,
                alsoClickIfOtherInGroupClicked = false,
                action = delegate
                {
                    if (massUsage > SRTSMod.GetStatFor<float>(srts?.def.defName, StatName.massCapacity))
                    {
                        Messages.Message("TooBigTransportersMassUsage".Translate(), MessageTypeDefOf.RejectInput,
                            false);
                    }
                    else
                    {
                        srts.TryGetComp<CompLaunchableSRTS>().WorldStartChoosingDestination(__instance);
                    }
                }
            };
            var RefuelSRTS = new Command_Action
            {
                defaultLabel = "CommandAddFuelSRTS".Translate(srts.TryGetComp<CompRefuelable>().parent.Label),
                defaultDesc = "CommandAddFuelDescSRTS".Translate(),
                icon = Tex2D.FuelSRTS,
                alsoClickIfOtherInGroupClicked = false,
                action = delegate
                {
                    var foundRefuelable = false;
                    var count = 0;
                    var thingList = CaravanInventoryUtility.AllInventoryItems(__instance);
                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (var index = 0; index < thingList.Count; index++)
                    {
                        var item = thingList[index];
                        if (item.def != ThingDefOf.Chemfuel)
                        {
                            continue;
                        }

                        count = item.stackCount;
                        var ownerOf = CaravanInventoryUtility.GetOwnerOf(__instance, item);
                        var num = srts.TryGetComp<CompRefuelable>().Props.fuelCapacity -
                                  srts.TryGetComp<CompRefuelable>().Fuel;
                        if (num < 1.0 && num > 0.0)
                        {
                            count = 1;
                        }

                        if (count * 1.0 >= num)
                        {
                            count = (int)num;
                        }

                        if (item.stackCount * 1.0 <= count)
                        {
                            item.stackCount -= count;
                            ownerOf.inventory.innerContainer.Remove(item);
                            item.Destroy();
                        }
                        else if ((uint)count > 0U)
                        {
                            item.SplitOff(count).Destroy();
                        }

                        srts.TryGetComp<CompRefuelable>().GetType()
                            .GetField("fuel", BindingFlags.Instance | BindingFlags.NonPublic)
                            ?.SetValue(srts.TryGetComp<CompRefuelable>(),
                                (float)(srts.TryGetComp<CompRefuelable>().Fuel + (double)count));
                        foundRefuelable = true;
                        break;
                    }

                    if (foundRefuelable)
                    {
                        Messages.Message("AddFuelSRTSCaravan".Translate(count, srts?.LabelCap),
                            MessageTypeDefOf.PositiveEvent, false);
                    }
                    else
                    {
                        Messages.Message("NoFuelSRTSCaravan".Translate(), MessageTypeDefOf.RejectInput, false);
                    }
                }
            };
            if (srts.TryGetComp<CompRefuelable>().IsFull)
            {
                RefuelSRTS.Disable();
            }

            yield return RefuelSRTS;
            yield return new Gizmo_MapRefuelableFuelStatus
            {
                nowFuel = srts.TryGetComp<CompRefuelable>().Fuel,
                maxFuel = srts.TryGetComp<CompRefuelable>().Props.fuelCapacity,
                compLabel = srts.TryGetComp<CompRefuelable>().Props.FuelGizmoLabel
            };
        }
    }

    public static IEnumerable<CodeInstruction> CustomOptionsPawnsToTransportTranspiler(
        IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
    {
        var instructionList = instructions.ToList();

        foreach (var instruction in instructionList)
        {
            if (instruction.opcode == OpCodes.Call && (MethodInfo)instruction.operand ==
                AccessTools.Method(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.AllSendablePawns)))
            {
                var label = ilg.DefineLabel();

                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld,
                    AccessTools.Field(typeof(Dialog_LoadTransporters), "transporters"));
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(SRTSHelper), nameof(SRTSHelper.SRTSInTransporters)));
                yield return new CodeInstruction(OpCodes.Brfalse, label);

                yield return new CodeInstruction(OpCodes.Pop);
                yield return new CodeInstruction(OpCodes.Pop);
                yield return new CodeInstruction(OpCodes.Pop);
                yield return new CodeInstruction(OpCodes.Pop);

                yield return new CodeInstruction(OpCodes.Ldsfld,
                    AccessTools.Field(typeof(SRTSMod), nameof(SRTSMod.mod)));
                yield return new CodeInstruction(OpCodes.Ldfld,
                    AccessTools.Field(typeof(SRTSMod), nameof(SRTSMod.settings)));
                yield return new CodeInstruction(OpCodes.Ldfld,
                    AccessTools.Field(typeof(SRTS_ModSettings), nameof(SRTS_ModSettings.allowEvenIfDowned)));

                yield return new CodeInstruction(OpCodes.Ldc_I4_0);

                yield return new CodeInstruction(OpCodes.Ldsfld,
                    AccessTools.Field(typeof(SRTSMod), nameof(SRTSMod.mod)));
                yield return new CodeInstruction(OpCodes.Ldfld,
                    AccessTools.Field(typeof(SRTSMod), nameof(SRTSMod.settings)));
                yield return new CodeInstruction(OpCodes.Ldfld,
                    AccessTools.Field(typeof(SRTS_ModSettings), nameof(SRTS_ModSettings.allowEvenIfPrisonerUnsecured)));

                yield return new CodeInstruction(OpCodes.Ldsfld,
                    AccessTools.Field(typeof(SRTSMod), nameof(SRTSMod.mod)));
                yield return new CodeInstruction(OpCodes.Ldfld,
                    AccessTools.Field(typeof(SRTSMod), nameof(SRTSMod.settings)));
                yield return new CodeInstruction(OpCodes.Ldfld,
                    AccessTools.Field(typeof(SRTS_ModSettings), nameof(SRTS_ModSettings.allowCapturablePawns)));

                instruction.labels.Add(label);
            }

            yield return instruction;
        }
    }

    public static bool CustomOptionsPawnsToTransportOverride(List<CompTransporter> ___transporters, Map ___map,
        Dialog_LoadTransporters __instance)
    {
        if (!___transporters.Any(x => x.parent.TryGetComp<CompLaunchableSRTS>() != null))
        {
            return true;
        }

        var pawnlist = CaravanFormingUtility.AllSendablePawns(___map, SRTSMod.mod.settings.allowEvenIfDowned, false,
            SRTSMod.mod.settings.allowEvenIfPrisonerUnsecured, SRTSMod.mod.settings.allowCapturablePawns);
        foreach (var p in pawnlist)
        {
            AccessTools.Method(typeof(Dialog_LoadTransporters), "AddToTransferables")
                .Invoke(__instance, [p]);
        }

        return false;
    }
}