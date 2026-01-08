using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SRTS;

public class SRTS_ModSettings : ModSettings
{
    public bool allowCapturablePawns = true;
    public List<string> allowedBombs = [];

    public bool allowEvenIfDowned = true;
    public bool allowEvenIfPrisonerUnsecured;

    public float buildCostMultiplier = 1f;

    private bool CEPreviouslyInitialized;
    public int confirmDistance = 30;
    public Dictionary<string, SRTS_DefProperties> defProperties = new();
    public bool disableAdvancedRecipes = false;
    public List<string> disallowedBombs = [];
    public bool displayHomeItems = true;
    public bool dynamicWorldDrawingSRTS = true;
    public bool expandBombPoints = true;

    public bool passengerLimits = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref passengerLimits, "passengerLimits", true);
        Scribe_Values.Look(ref confirmDistance, "confirmDistance", 30);
        Scribe_Values.Look(ref displayHomeItems, "displayHomeItems", true);
        Scribe_Values.Look(ref expandBombPoints, "expandBombPoints", true);
        Scribe_Values.Look(ref dynamicWorldDrawingSRTS, "dynamicWorldDrawingSRTS", true);

        Scribe_Values.Look(ref allowEvenIfDowned, "allowEvenIfDowned", true);
        Scribe_Values.Look(ref allowEvenIfPrisonerUnsecured, "allowEvenIfPrisonerUnsecured");
        Scribe_Values.Look(ref allowCapturablePawns, "allowCapturablePawns", true);

        Scribe_Collections.Look(ref defProperties, "defProperties", LookMode.Value, LookMode.Deep);
        //Scribe_Collections.Look<string>(ref allowedBombs, "allowedBombs", LookMode.Value);
        Scribe_Collections.Look(ref disallowedBombs, "disallowedBombs", LookMode.Value);

        Scribe_Values.Look(ref CEPreviouslyInitialized, "CEPreviouslyInitialized");
    }

    public void CheckDictionarySavedValid()
    {
        if (defProperties is not null && defProperties.Count > 0)
        {
            return;
        }

        defProperties = new Dictionary<string, SRTS_DefProperties>();

        foreach (var t in DefDatabase<ThingDef>.AllDefs.Where(x =>
                     x.GetCompProperties<CompProperties_LaunchableSRTS>() != null))
        {
            defProperties.Add(t.defName, new SRTS_DefProperties(t));
        }
    }

    public static void CheckNewDefaultValues()
    {
        SRTSMod.mod.settings.CheckDictionarySavedValid();
        foreach (var kvp in SRTSMod.mod.settings.defProperties)
        {
            if (SRTSMod.mod.settings.defProperties.ContainsKey(kvp.Key))
            {
                if (SRTSMod.mod.settings.defProperties[kvp.Key].ResetReferencedDef(kvp.Key))
                {
                    if (SRTSMod.mod.settings.defProperties[kvp.Key].defaultValues)
                    {
                        SRTSMod.mod.settings.defProperties[kvp.Key].ResetToDefaultValues();
                    }

                    continue;
                }
            }

            Log.Warning(
                $"[SRTSExpanded] Unable to perform loading procedures on key ({kvp.Key}). Performing a hard reset on the ModSettings.");
            SRTSMod.mod.settings.defProperties.Clear();
            SRTSMod.mod.CheckDictionaryValid();
            break;
        }
    }
}