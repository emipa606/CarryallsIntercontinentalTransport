using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Verse;

namespace SRTS;

public class Dialog_ResearchChange : Window
{
    private bool addedResearch;

    private int charCount;

    private List<ResearchProjectDef> projectsSearched;

    private bool researchChanged = true;

    private string researchString;

    public Dialog_ResearchChange()
    {
        forcePause = true;
        doCloseX = true;
        doCloseButton = false;
        closeOnClickedOutside = false;
        absorbInputAroundWindow = true;
        projectsSearched = [];
        addedResearch = false;
        charCount = 0;
    }

    public override Vector2 InitialSize => new(200f, 350f);

    public override void DoWindowContents(Rect inRect)
    {
        var labelRect = new Rect(inRect.x, inRect.y, inRect.width, 24f);
        var searchBarRect = new Rect(inRect.x, labelRect.y + 24f, inRect.width, 24f);
        Widgets.Label(labelRect, "SearchResearch".Translate());
        charCount = researchString?.Length ?? 0;
        researchString = Widgets.TextArea(searchBarRect, researchString);

        if (researchString.Length != charCount || researchChanged)
        {
            projectsSearched = DefDatabase<ResearchProjectDef>.AllDefs.Where(x =>
                !SRTSMod.mod.props.ResearchPrerequisites.Contains(x) &&
                CultureInfo.CurrentCulture.CompareInfo.IndexOf(x.defName, researchString, CompareOptions.IgnoreCase) >=
                0).ToList();
            researchChanged = false;
        }

        for (var i = 0; i < projectsSearched.Count; i++)
        {
            var proj = projectsSearched[i];
            var projectRect = new Rect(inRect.x, searchBarRect.y + 24 + (24 * i), inRect.width, 30f);
            if (!Widgets.ButtonText(projectRect, proj.defName, false, false) ||
                SRTSMod.mod.props.ResearchPrerequisites.Contains(proj))
            {
                continue;
            }

            addedResearch = true;
            researchChanged = true;
            SRTSMod.mod.props.AddCustomResearch(proj);
        }
    }

    public override void PreClose()
    {
        /*if (addedResearch)
            Messages.Message("RestartGameResearch".Translate(), MessageTypeDefOf.CautionInput, false);*/
        //Uncomment to send message to restart game if research has been changed
    }
}