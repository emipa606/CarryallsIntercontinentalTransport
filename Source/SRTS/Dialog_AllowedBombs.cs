using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace SRTS;

public class Dialog_AllowedBombs : Window
{
    private const float BufferArea = 24f;

    private int charCount;

    private bool explosivesChanged = true;

    private List<ThingDef> explosivesSearched;

    private string explosivesString;

    private Vector2 scrollPosition;

    public Dialog_AllowedBombs()
    {
        forcePause = true;
        doCloseX = true;
        doCloseButton = false;
        closeOnClickedOutside = false;
        absorbInputAroundWindow = true;
        explosivesSearched = [];
    }

    public override Vector2 InitialSize => new(550f, 350f);

    public override void DoWindowContents(Rect inRect)
    {
        var labelRect = new Rect(inRect.width / 2, inRect.y, inRect.width / 2, 24f);
        var label2Rect = new Rect(inRect.x, inRect.y, inRect.width / 2, 24f);
        var searchBarRect = new Rect(inRect.width / 2, labelRect.y + 24f, inRect.width / 2, 24f);
        Widgets.Label(labelRect, "SearchExplosive".Translate());
        Widgets.Label(label2Rect, "CurrentExplosives".Translate());
        charCount = explosivesString?.Length ?? 0;
        explosivesString = Widgets.TextArea(searchBarRect, explosivesString);

        if (explosivesString.Length != charCount || explosivesChanged)
        {
            explosivesChanged = false;
            if (SRTSHelper.CEModLoaded)
            {
                explosivesSearched = DefDatabase<ThingDef>.AllDefs.Where(x =>
                    x.HasComp(Type.GetType("CombatExtended.CompExplosiveCE,CombatExtended")) &&
                    !SRTSMod.mod.settings.allowedBombs.Contains(x.defName)
                    && CultureInfo.CurrentCulture.CompareInfo.IndexOf(x.defName, explosivesString,
                        CompareOptions.IgnoreCase) >= 0).ToList();
            }
            else
            {
                explosivesSearched = DefDatabase<ThingDef>.AllDefs.Where(x =>
                    x.GetCompProperties<CompProperties_Explosive>() != null && x.building is null &&
                    !SRTSMod.mod.settings.allowedBombs.Contains(x.defName)
                    && CultureInfo.CurrentCulture.CompareInfo.IndexOf(x.defName, explosivesString,
                        CompareOptions.IgnoreCase) >= 0).ToList();
            }
        }

        for (var i = 0; i < explosivesSearched.Count; i++)
        {
            var explosive = explosivesSearched[i];
            var explosiveRect = new Rect(searchBarRect.x, searchBarRect.y + 24 + (24 * i), searchBarRect.width, 30f);
            if (!Widgets.ButtonText(explosiveRect, explosive.defName, false, false) ||
                SRTSMod.mod.settings.allowedBombs.Contains(explosive.defName))
            {
                continue;
            }

            explosivesChanged = true;
            SRTSMod.mod.settings.allowedBombs.Add(explosive.defName);
            if (SRTSMod.mod.settings.allowedBombs.Contains(explosive.defName))
            {
                SRTSMod.mod.settings.disallowedBombs.Remove(explosive.defName);
            }
        }

        var outRect = new Rect(inRect.x - 6f, searchBarRect.y + BufferArea, searchBarRect.width,
            inRect.height - (BufferArea * 2));
        var viewRect = new Rect(outRect.x, outRect.y, outRect.width - 32f,
            outRect.height * (SRTSMod.mod.settings.allowedBombs.Count / 11f));
        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        for (var i = 0; i < SRTSMod.mod.settings.allowedBombs.Count; i++)
        {
            var s = SRTSMod.mod.settings.allowedBombs[i];
            var allowedExplosivesRect = new Rect(inRect.x, searchBarRect.y + 24f + (24f * i), searchBarRect.width, 30f);
            if (!Widgets.ButtonText(allowedExplosivesRect, s, false, false))
            {
                continue;
            }

            explosivesChanged = true;
            SRTSMod.mod.settings.allowedBombs.Remove(s);
            SRTSMod.mod.settings.disallowedBombs.Add(s);
        }

        Widgets.EndScrollView();
    }
}