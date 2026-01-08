using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SRTS;

public static class SRTSHelper
{
    public static Dictionary<ThingDef, ResearchProjectDef> srtsDefProjects = new();

    public static bool CEModLoaded = false;
    public static BombingTargeter targeter = new();


    public static bool SOS2ModLoaded = false;

    public static bool SRTSInCaravan => TradeSession.playerNegotiator.GetCaravan().AllThings
        .Any(x => x.TryGetComp<CompLaunchableSRTS>() != null);

    public static bool DynamicTexturesEnabled => SRTSMod.mod.settings.dynamicWorldDrawingSRTS;
    public static Type CompProperties_ExplosiveCE => null;
    public static Type CompExplosiveCE => null;
    public static WorldObjectDef SpaceSite => null;
    public static Type SpaceSiteType => null;
    public static Type SOS2LaunchableType => null;

    public static float GetResearchStat(ResearchProjectDef project)
    {
        return SRTSMod.GetStatFor<float>(srtsDefProjects.FirstOrDefault(x => x.Value == project).Key.defName,
            StatName.researchPoints);
    }

    public static NamedArgument GetResearchStatString(ResearchProjectDef project)
    {
        return SRTSMod.GetStatFor<float>(srtsDefProjects.FirstOrDefault(x => x.Value == project).Key.defName,
            StatName.researchPoints).ToString("F0");
    }

    public static bool ContainedInDefProjects(ResearchProjectDef project)
    {
        return srtsDefProjects.Any(x => x.Value == project);
    }

    public static bool SRTSInTransporters(List<CompTransporter> transporters)
    {
        return transporters.Any(x => x.parent.GetComp<CompLaunchableSRTS>() != null);
    }

    public static bool SRTSLauncherSelected(List<CompTransporter> transporters)
    {
        return transporters.Any(x => x.parent.GetComp<CompLaunchableSRTS>() != null);
    }

    public static bool SRTSNonPlayerHomeMap(Map map)
    {
        return !map.IsPlayerHome;
    }

    public static void AddToSRTSFromCaravan(Caravan caravan, Thing thing)
    {
        if (caravan.AllThings.Any(x => x.TryGetComp<CompLaunchableSRTS>() != null))
        {
            caravan.AllThings.First(x => x.TryGetComp<CompLaunchableSRTS>() != null).TryGetComp<CompLaunchableSRTS>()
                ?.AddThingsToSRTS(thing);
        }
    }

    public static void FinishCustomPrerequisites(ResearchProjectDef project, ResearchManager instance)
    {
        var projects = SRTSMod.mod.settings
            .defProperties[srtsDefProjects.FirstOrDefault(x => x.Value == project).Key.defName].CustomResearch;
        foreach (var proj in projects)
        {
            if (!proj.IsFinished)
            {
                instance.FinishProject(proj);
            }
        }
    }

    public static void DrawLinesCustomPrerequisites(ResearchProjectDef project, ResearchTabDef curTab, Vector2 start,
        Vector2 end, ResearchProjectDef selectedProject, int i)
    {
        var defName = srtsDefProjects.FirstOrDefault(x => x.Value == project).Key?.defName;
        if (defName is null)
        {
            return;
        }

        var projects = SRTSMod.mod.settings.defProperties[defName].CustomResearch;

        start.x = (project.ResearchViewX * 190f) + 140f;
        start.y = (project.ResearchViewY * 100f) + 25f;
        foreach (var proj in projects)
        {
            if (proj == null || proj.tab != curTab)
            {
                continue;
            }

            end.x = proj.ResearchViewX * 190f;
            end.y = (proj.ResearchViewY * 100f) + 25f;
            if (selectedProject != null && (selectedProject == project || selectedProject == proj))
            {
                if (i == 1)
                {
                    Widgets.DrawLine(start, end, TexUI.HighlightLineResearchColor, 4f);
                }
            }

            if (i == 0)
            {
                Widgets.DrawLine(start, end, new Color(255, 215, 0, 0.25f), 2f);
            }
        }
    }

    /* =========================== Redacted but used as helper methods to ErrorOnNoPawnsTranspiler =========================== */
    public static string MinMaxString(List<CompTransporter> transporters, bool min)
    {
        var srts = transporters.First(x => x.parent.GetComp<CompLaunchableSRTS>() != null).parent;
        return min
            ? "Minimum Required Pawns for " + srts.def.LabelCap + ": " +
              SRTSMod.GetStatFor<int>(srts.def.defName, StatName.minPassengers)
            : "Maximum Pawns able to board " + srts.def.LabelCap + ": " +
              SRTSMod.GetStatFor<int>(srts.def.defName, StatName.maxPassengers);
    }

    public static bool NoPawnInSRTS(List<CompTransporter> transporters, List<Pawn> pawns)
    {
        return transporters.Any(x => x.parent.GetComp<CompLaunchableSRTS>() != null) &&
               !pawns.Any(x => x.IsColonistPlayerControlled);
    }

    public static bool MinPawnRestrictionsSRTS(List<CompTransporter> transporters, List<Pawn> pawns)
    {
        if (!transporters.Any(x => x.parent.GetComp<CompLaunchableSRTS>() != null))
        {
            return false;
        }

        var minPawns = transporters.Min(x => SRTSMod.GetStatFor<int>(x.parent.def.defName, StatName.minPassengers));
        return pawns.Count(x => x.IsColonistPlayerControlled) < minPawns;
    }

    public static bool MaxPawnRestrictionsSRTS(List<CompTransporter> transporters, List<Pawn> pawns)
    {
        if (!transporters.Any(x => x.parent.GetComp<CompLaunchableSRTS>() != null))
        {
            return false;
        }

        var maxPawns = transporters.Max(x => SRTSMod.GetStatFor<int>(x.parent.def.defName, StatName.maxPassengers));
        return pawns.Count > maxPawns;
    }
    /* ======================================================================================================================= */

    public static void PopulateDictionary()
    {
        srtsDefProjects = new Dictionary<ThingDef, ResearchProjectDef>();
        var defs = DefDatabase<ThingDef>.AllDefsListForReading.Where(x =>
            x?.researchPrerequisites?.Count > 0 && x.researchPrerequisites?[0].tab.ToString() == "SRTSE").ToList();
        foreach (var def in defs)
        {
            srtsDefProjects.Add(def, def.researchPrerequisites[0]);
        }
    }

    public static void PopulateAllowedBombs()
    {
        if (CEModLoaded)
        {
            var CEthings = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(x =>
                x.HasComp(Type.GetType("CombatExtended.CompExplosiveCE,CombatExtended")));
            SRTSMod.mod.settings.allowedBombs ??= [];

            SRTSMod.mod.settings.disallowedBombs ??= [];

            foreach (var td in CEthings)
            {
                if (!SRTSMod.mod.settings.allowedBombs.Contains(td.defName) &&
                    !SRTSMod.mod.settings.disallowedBombs.Contains(td.defName))
                {
                    SRTSMod.mod.settings.allowedBombs.Add(td.defName);
                }
            }

            return;
        }

        var things = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(x =>
            x.GetCompProperties<CompProperties_Explosive>() != null && x.projectileWhenLoaded != null);
        SRTSMod.mod.settings.allowedBombs ??= [];

        SRTSMod.mod.settings.disallowedBombs ??= [];

        foreach (var td in things)
        {
            if (!SRTSMod.mod.settings.allowedBombs.Contains(td.defName) &&
                !SRTSMod.mod.settings.disallowedBombs.Contains(td.defName))
            {
                SRTSMod.mod.settings.allowedBombs.Add(td.defName);
            }
        }
    }
}