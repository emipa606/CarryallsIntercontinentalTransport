using System;
using System.Collections.Generic;
using System.Linq;
using Mlie;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SRTS;

public class SRTSMod : Mod
{
    public static SRTSMod mod;
    private static string currentVersion;
    public readonly SRTS_ModSettings settings;

    private string currentKey;

    private SettingsCategory currentPage;

    public SRTS_DefProperties props;

    public Vector2 scrollPosition;

    public SRTSMod(ModContentPack content) : base(content)
    {
        settings = GetSettings<SRTS_ModSettings>();
        mod = this;
        currentVersion = VersionFromManifest.GetVersionFromModMetaData(content.ModMetaData);
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        CheckDictionaryValid();

        var font = Text.Font;
        Text.Font = GameFont.Tiny;
        Text.Font = font;

        var listing_Standard = new Listing_Standard();
        var settingsCategory = new Rect((inRect.width / 2) - (inRect.width / 12), inRect.y, inRect.width / 6,
            inRect.height);
        var groupSRTS = new Rect(settingsCategory.x - settingsCategory.width, settingsCategory.y,
            settingsCategory.width, settingsCategory.height);

        if (Prefs.DevMode)
        {
            var emergencyReset = new Rect(inRect.width - settingsCategory.width, settingsCategory.y,
                settingsCategory.width, settingsCategory.height);
            listing_Standard.Begin(emergencyReset);
            if (listing_Standard.ButtonText("DevMode Reset"))
            {
                settings.defProperties.Clear();
                CheckDictionaryValid();
                ResetMainSettings();
                Log.Message("========================== \n DevMode Settings Reset:");
                foreach (var pair in settings.defProperties)
                {
                    Log.Message($"KVP: {pair.Key} : {pair.Value.referencedDef.defName}");
                }

                Log.Message("==========================");
            }

            listing_Standard.End();
        }

        listing_Standard.Begin(groupSRTS);
        if (currentPage == SRTS.SettingsCategory.Settings)
        {
            listing_Standard.ButtonText(string.Empty);
        }
        else if (currentPage == SRTS.SettingsCategory.Stats || currentPage == SRTS.SettingsCategory.Research)
        {
            if (listing_Standard.ButtonText(currentKey))
            {
                List<FloatMenuOption> options = [];
                foreach (var s in settings.defProperties.Keys)
                {
                    options.Add(new FloatMenuOption(s, () => currentKey = s));
                }

                if (!options.Any())
                {
                    options.Add(new FloatMenuOption("NoneBrackets".Translate(), null));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        listing_Standard.End();

        listing_Standard.Begin(settingsCategory);
        if (listing_Standard.ButtonText(EnumToString(currentPage)))
        {
            var op1 = new FloatMenuOption("MainSettings".Translate(),
                () => currentPage = SRTS.SettingsCategory.Settings);
            var op2 = new FloatMenuOption("DefSettings".Translate(), () => currentPage = SRTS.SettingsCategory.Stats);
            Find.WindowStack.Add(new FloatMenu([
                op1, op2
            ])); //Removed SettingsCategory.Research from float menu, removing all research related patches. DO NOT ADD BACK
        }

        listing_Standard.End();

        props = settings.defProperties[currentKey];
        if (props is null)
        {
            ResetMainSettings();
            settings.defProperties.Clear();
            CheckDictionaryValid();
            props = settings.defProperties[currentKey];
        }

        ReferenceDefCheck(ref props);

        var propsReset = new Rect(settingsCategory.x + settingsCategory.width, settingsCategory.y,
            settingsCategory.width, settingsCategory.height);
        listing_Standard.Begin(propsReset);
        if (listing_Standard.ButtonText("ResetDefault".Translate(), "ResetDefaultTooltip".Translate()))
        {
            if (currentPage == SRTS.SettingsCategory.Settings)
            {
                ResetMainSettings();
            }
            else if (currentPage == SRTS.SettingsCategory.Stats || currentPage == SRTS.SettingsCategory.Research)
            {
                var op1 = new FloatMenuOption("ResetThisSRTS".Translate(), () => props.ResetToDefaultValues());
                var op2 = new FloatMenuOption("ResetAll".Translate(), delegate
                {
                    for (var i = 0; i < settings.defProperties.Count; i++)
                    {
                        var p = settings.defProperties.ElementAt(i).Value;
                        ReferenceDefCheck(ref p);
                        p.ResetToDefaultValues();
                    }
                });
                Find.WindowStack.Add(new FloatMenu([op1, op2]));
            }
        }

        listing_Standard.End();

        var group2 = new Rect(inRect.x, settingsCategory.y + 36f, inRect.width / 3, inRect.height);

        if (currentPage == SRTS.SettingsCategory.Stats)
        {
            listing_Standard.Begin(group2);

            listing_Standard.Settings_Header("SRTSSettings".Translate(), DialogSettings.highlightColor);

            listing_Standard.Settings_SliderLabeled("FlightSpeed".Translate(), string.Empty, ref props.flightSpeed,
                0.15f, 50, 2, 2, 9999f, "Instant".Translate());

            listing_Standard.Settings_SliderLabeled("FuelEfficiency".Translate(), "FuelEfficiencySymbol".Translate(),
                ref props.fuelPerTile, 1, 6f);

            if (settings.passengerLimits)
            {
                var min = props.minPassengers;
                var max = props.maxPassengers;
                listing_Standard.Settings_SliderLabeled("MinPassengers".Translate(), string.Empty,
                    ref props.minPassengers, 1, 100);
                listing_Standard.Settings_SliderLabeled("MaxPassengers".Translate(), string.Empty,
                    ref props.maxPassengers, 1, 100, 999, "\u221E");
                if (props.minPassengers > props.maxPassengers && min != props.minPassengers)
                {
                    props.maxPassengers = props.minPassengers;
                }

                if (props.maxPassengers < props.minPassengers && max != props.maxPassengers)
                {
                    props.minPassengers = props.maxPassengers;
                }
            }
            else
            {
                listing_Standard.Gap(54f);
            }

            var mass = (int)props.massCapacity;
            listing_Standard.Settings_IntegerBox("CargoCapacity".Translate(), ref mass, 100f, 50f, 0);
            props.massCapacity = mass;

            listing_Standard.Gap();

            if (props.BombCapable)
            {
                listing_Standard.Gap(24f);
                listing_Standard.Settings_Header("BombSettings".Translate(), DialogSettings.highlightColor);

                listing_Standard.Settings_SliderLabeled("BombSpeed".Translate(), string.Empty, ref props.bombingSpeed,
                    0.5f, 2.5f, 10f, 1);
                listing_Standard.Settings_SliderLabeled("RadiusDrop".Translate(), "CellsEndValue".Translate(),
                    ref props.radiusDrop, 1, 10);
                listing_Standard.Settings_SliderLabeled("DistanceBetweenDrops".Translate(), "CellsEndValue".Translate(),
                    ref props.distanceBetweenDrops, 0.2f, 11, 1, 1, 999, "SingleDrop".Translate());

                listing_Standard.Settings_Header("BombCountSRTS".Translate(), DialogSettings.highlightColor,
                    GameFont.Tiny);
                listing_Standard.Settings_SliderLabeled("PreciseBombing".Translate(), string.Empty,
                    ref props.precisionBombingNumBombs, 1, 10);
                listing_Standard.Settings_SliderLabeled("CarpetBombing".Translate(), string.Empty,
                    ref props.numberBombs, 1, 40);
            }

            listing_Standard.End();

            if (SRTSHelper.SOS2ModLoaded)
            {
                var sos2Rect = new Rect(inRect.width - (inRect.width / 4), inRect.height - (inRect.height / 8),
                    inRect.width / 4, inRect.height / 4);

                listing_Standard.Begin(sos2Rect);

                listing_Standard.Settings_Header("SOS2Compatibility".Translate(), DialogSettings.highlightColor,
                    GameFont.Small);
                listing_Standard.CheckboxLabeled("SpaceFaring".Translate(), ref props.spaceFaring);
                listing_Standard.CheckboxLabeled("ShuttleBayLanding".Translate(), ref props.shuttleBayLanding);

                listing_Standard.End();
            }
        }
        else if (currentPage == SRTS.SettingsCategory.Research)
        {
            listing_Standard.Begin(group2);

            listing_Standard.Settings_Header("ResearchDef".Translate(props.RequiredResearch[0].LabelCap),
                DialogSettings.highlightColor);

            var rPoints = (int)props.researchPoints;
            //listing_Standard.Settings_IntegerBox("SRTSResearch".Translate(), ref rPoints, 100f, 50f, 0, int.MaxValue);
            props.researchPoints = rPoints;

            listing_Standard.Gap(24f);

            listing_Standard.Settings_Header("SRTSResearchRequirements".Translate(), DialogSettings.highlightColor,
                GameFont.Small);

            foreach (var proj in props.requiredResearch)
            {
                listing_Standard.Settings_Header(proj.LabelCap, Color.clear, GameFont.Small);
            }

            for (var i = props.CustomResearch.Count - 1; i >= 0; i--)
            {
                var proj = props.customResearchRequirements[i];
                if (listing_Standard.Settings_ButtonLabeled(proj.LabelCap, "RemoveResearch".Translate(), Color.cyan,
                        60f, false))
                {
                    props.RemoveCustomResearch(proj);
                }

                listing_Standard.Gap(8f);
            }

            if (listing_Standard.Settings_Button("AddItemSRTS".Translate(),
                    new Rect(group2.width - 60f, group2.y + 24f, 60f, 20f), Color.white))
            {
                Find.WindowStack.Add(new Dialog_ResearchChange());
            }

            listing_Standard.Gap(24f);

            listing_Standard.End();
        }
        else if (currentPage == SRTS.SettingsCategory.Settings)
        {
            listing_Standard.Begin(group2);

            listing_Standard.CheckboxLabeled("PassengerLimit".Translate(), ref settings.passengerLimits,
                "PassengerLimitTooltip".Translate());
            listing_Standard.CheckboxLabeled("DisplayHomeItems".Translate(), ref settings.displayHomeItems,
                "DisplayHomeItemsTooltip".Translate());
            listing_Standard.CheckboxLabeled("DynamicWorldObjectSRTS".Translate(), ref settings.dynamicWorldDrawingSRTS,
                "DynamicWorldObjectSRTSTooltip".Translate());

            listing_Standard.Gap();

            listing_Standard.Settings_SliderLabeled("CA_ConfirmAtDistance".Translate(), string.Empty,
                ref settings.confirmDistance, 1, 100);

            listing_Standard.End();


            var transportGroupRect =
                new Rect(inRect.width - (inRect.width / 3), group2.y, inRect.width / 3, group2.height);

            listing_Standard.Begin(transportGroupRect);

            listing_Standard.Settings_Header("SRTSBoardingOptions".Translate(), DialogSettings.highlightColor,
                GameFont.Small);

            listing_Standard.CheckboxLabeled("AllowDownedSRTS".Translate(), ref settings.allowEvenIfDowned);
            listing_Standard.CheckboxLabeled("AllowUnsecurePrisonerSRTS".Translate(),
                ref settings.allowEvenIfPrisonerUnsecured);
            listing_Standard.CheckboxLabeled("AllowCapturablePawnSRTS".Translate(), ref settings.allowCapturablePawns);

            if (currentVersion != null)
            {
                listing_Standard.Gap();
                GUI.contentColor = Color.gray;
                listing_Standard.Label("carryalls.CurrentModVersion".Translate(currentVersion));
                GUI.contentColor = Color.white;
            }

            listing_Standard.End();
        }

        if (currentPage == SRTS.SettingsCategory.Stats || currentPage == SRTS.SettingsCategory.Research)
        {
            var graphicRequest = new GraphicRequest(props.referencedDef.graphicData.graphicClass,
                props.referencedDef.graphicData.texPath, ShaderTypeDefOf.Cutout.Shader,
                props.referencedDef.graphic.drawSize,
                Color.white, Color.white, props.referencedDef.graphicData, 0, null, null);
            var texPath = props.referencedDef.graphicData.texPath;
            if (graphicRequest.graphicClass == typeof(Graphic_Multi))
            {
                texPath += "_north";
            }

            var pictureRect = new Rect(inRect.width / 2, inRect.height / 3, 300f, 300f);
            GUI.DrawTexture(pictureRect, ContentFinder<Texture2D>.Get(texPath));
            DialogSettings.Draw_Label(new Rect(pictureRect.x, (inRect.height / 3) - 60f, 300f, 100f),
                props.referencedDef.label.Replace("SRTS ", ""), Color.clear, Color.white, GameFont.Medium,
                TextAnchor.MiddleCenter);

            var valueFont = Text.Font;
            var alignment = Text.Anchor;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(
                new Rect(inRect.width - settingsCategory.width, settingsCategory.y + (Prefs.DevMode ? 30f : 0f),
                    settingsCategory.width, 30f),
                props.defaultValues ? "DefaultValues".Translate() : "CustomValues".Translate());
            Text.Font = valueFont;
            Text.Anchor = alignment;
        }

        if (props.defaultValues && !props.IsDefault)
        {
            props.defaultValues = false;
        }

        base.DoSettingsWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
        return "SRTSExpanded".Translate();
    }

    private void ResetMainSettings()
    {
        SoundDefOf.Click.PlayOneShotOnCamera();
        settings.passengerLimits = true;
        settings.dynamicWorldDrawingSRTS = true;
        settings.displayHomeItems = true;
        settings.allowEvenIfDowned = true;
        settings.allowEvenIfPrisonerUnsecured = false;
        settings.allowCapturablePawns = true;
        settings.confirmDistance = 30;
        settings.disallowedBombs.Clear();
        //this.settings.allowedBombs.Clear();
        SRTSHelper.PopulateAllowedBombs();
    }

    internal void ResetBombList()
    {
        settings.disallowedBombs.Clear();
        //this.settings.allowedBombs.Clear();
        SRTSHelper.PopulateAllowedBombs();
    }

    private void ReferenceDefCheck(ref SRTS_DefProperties props)
    {
        if (props.referencedDef is not null)
        {
            return;
        }

        var propsRef = props;
        props.referencedDef =
            DefDatabase<ThingDef>.GetNamed(settings.defProperties.FirstOrDefault(x => x.Value == propsRef).Key);
    }

    public void CheckDictionaryValid()
    {
        if (settings.defProperties is null || settings.defProperties.Count <= 0)
        {
            settings.defProperties = new Dictionary<string, SRTS_DefProperties>();
            foreach (var t in DefDatabase<ThingDef>.AllDefs.Where(x =>
                         x.GetCompProperties<CompProperties_LaunchableSRTS>() != null))
            {
                settings.defProperties.Add(t.defName, new SRTS_DefProperties(t));
            }
        }

        if (currentKey is null || currentKey.NullOrEmpty())
        {
            currentKey = settings.defProperties.Keys.First();
        }
    }

    private static string EnumToString(SettingsCategory category)
    {
        switch (category)
        {
            case SRTS.SettingsCategory.Settings:
                return "MainSettings".Translate();
            case SRTS.SettingsCategory.Stats:
                return "DefSettings".Translate();
            case SRTS.SettingsCategory.Research:
                return "ResearchSettings".Translate();
        }

        Log.Warning($"Setting Category {category} not yet implemented. - Smash Phil");
        return category.ToString();
    }

    public static T GetStatFor<T>(string defName, StatName stat)
    {
        mod.CheckDictionaryValid();
        if (!mod.settings.defProperties.ContainsKey(defName) && DefDatabase<ThingDef>.GetNamedSilentFail(defName)
                ?.GetCompProperties<CompProperties_LaunchableSRTS>() != null)
        {
            Log.Warning(
                $"Key was not able to be found inside ModSettings Dictionary. Resetting to default values and initializing: {defName}");
            mod.settings.defProperties.Add(defName,
                new SRTS_DefProperties(DefDatabase<ThingDef>.GetNamed(defName))); //Initialize
        }

        switch (stat)
        {
            case StatName.massCapacity:
                return (T)Convert.ChangeType(mod.settings.defProperties[defName].massCapacity, typeof(T));
            case StatName.minPassengers:
                return (T)Convert.ChangeType(mod.settings.defProperties[defName].minPassengers, typeof(T));
            case StatName.maxPassengers:
                return (T)Convert.ChangeType(mod.settings.defProperties[defName].maxPassengers, typeof(T));
            case StatName.flightSpeed:
                return (T)Convert.ChangeType(mod.settings.defProperties[defName].flightSpeed, typeof(T));

            /* SOS2 Compatibility */
            case StatName.spaceFaring:
                return (T)Convert.ChangeType(mod.settings.defProperties[defName].spaceFaring, typeof(T));
            case StatName.shuttleBayLanding:
                return (T)Convert.ChangeType(mod.settings.defProperties[defName].shuttleBayLanding, typeof(T));
            /* ------------------ */

            case StatName.bombingSpeed:
                return (T)Convert.ChangeType(mod.settings.defProperties[defName].bombingSpeed, typeof(T));
            case StatName.numberBombs:
                return (T)Convert.ChangeType(mod.settings.defProperties[defName].numberBombs, typeof(T));
            case StatName.radiusDrop:
                return (T)Convert.ChangeType(mod.settings.defProperties[defName].radiusDrop, typeof(T));
            case StatName.distanceBetweenDrops:
                return (T)Convert.ChangeType(mod.settings.defProperties[defName].distanceBetweenDrops, typeof(T));
            case StatName.researchPoints:
                return (T)Convert.ChangeType(mod.settings.defProperties[defName].researchPoints, typeof(T));
            case StatName.fuelPerTile:
                return (T)Convert.ChangeType(mod.settings.defProperties[defName].fuelPerTile, typeof(T));
            case StatName.precisionBombingNumBombs:
                return (T)Convert.ChangeType(mod.settings.defProperties[defName].precisionBombingNumBombs, typeof(T));
        }

        return default;
    }

    //private bool checkValidityBombs = false;
}