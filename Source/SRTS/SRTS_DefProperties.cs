using System.Collections.Generic;
using RimWorld;
using Verse;

namespace SRTS;

public class SRTS_DefProperties : IExposable
{
    private bool bombingCapable;

    public float bombingSpeed = 1;

    private List<string> customResearchDefNames;

    public List<ResearchProjectDef> customResearchRequirements;

    public bool defaultValues = true;

    public float distanceBetweenDrops = 10f;

    public float flightSpeed = 10;

    public float fuelPerTile = 2.25f;

    public float massCapacity;

    public int maxPassengers = 2;

    public int minPassengers = 1;
    public int numberBombs;

    public int precisionBombingNumBombs = 1;

    public int radiusDrop = 1;

    /// <summary>
    ///     Base SRTS values
    /// </summary>
    public ThingDef referencedDef;

    public List<ResearchProjectDef> requiredResearch;

    public float researchPoints = 1f;

    public bool shuttleBayLanding;

    /* SOS2 Compatibility */
    public bool spaceFaring;

    public SRTS_DefProperties()
    {
    }

    public SRTS_DefProperties(ThingDef def)
    {
        referencedDef = def;
        defaultValues = true;
        massCapacity = referencedDef.GetCompProperties<CompProperties_Transporter>().massCapacity;
        minPassengers = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().minPassengers;
        maxPassengers = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().maxPassengers;
        flightSpeed = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().travelSpeed;
        fuelPerTile = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().fuelPerTile;

        spaceFaring = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().spaceFaring;
        shuttleBayLanding = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().shuttleBayLanding;

        requiredResearch = [];

        requiredResearch.AddRange(referencedDef.researchPrerequisites);
        researchPoints = RequiredResearch?[0].baseCost ?? 1;
        customResearchRequirements = [];
        customResearchDefNames = [];

        bombingCapable = referencedDef.GetCompProperties<CompProperties_BombsAway>() != null;
        if (!BombCapable)
        {
            return;
        }

        numberBombs = referencedDef.GetCompProperties<CompProperties_BombsAway>().numberBombs;
        radiusDrop = referencedDef.GetCompProperties<CompProperties_BombsAway>().radiusOfDrop;
        bombingSpeed = referencedDef.GetCompProperties<CompProperties_BombsAway>().speed;
        distanceBetweenDrops = referencedDef.GetCompProperties<CompProperties_BombsAway>().distanceBetweenDrops;
        precisionBombingNumBombs =
            referencedDef.GetCompProperties<CompProperties_BombsAway>().precisionBombingNumBombs;
    }

    public bool IsDefault
    {
        get
        {
            var validSetup = true;
            if (BombCapable)
            {
                validSetup = numberBombs == referencedDef.GetCompProperties<CompProperties_BombsAway>().numberBombs &&
                             radiusDrop == referencedDef.GetCompProperties<CompProperties_BombsAway>().radiusOfDrop &&
                             precisionBombingNumBombs == referencedDef.GetCompProperties<CompProperties_BombsAway>()
                                 .precisionBombingNumBombs && bombingSpeed ==
                             referencedDef.GetCompProperties<CompProperties_BombsAway>().speed
                             && distanceBetweenDrops == referencedDef.GetCompProperties<CompProperties_BombsAway>()
                                 .distanceBetweenDrops;
            }

            return validSetup && RequiredResearch[0].baseCost == researchPoints && !customResearchDefNames.Any() &&
                   massCapacity == referencedDef.GetCompProperties<CompProperties_Transporter>().massCapacity &&
                   minPassengers == referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().minPassengers
                   && maxPassengers == referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().maxPassengers &&
                   flightSpeed == referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().travelSpeed &&
                   fuelPerTile == referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().fuelPerTile;
        }
    }
    /* ------------------ */


    public List<ResearchProjectDef> ResearchPrerequisites
    {
        get
        {
            if (requiredResearch is null)
            {
                requiredResearch = [];
                requiredResearch.AddRange(referencedDef.researchPrerequisites);
            }

            customResearchRequirements ??= [];

            List<ResearchProjectDef> projects = [];
            projects.AddRange(requiredResearch);
            projects.AddRange(customResearchRequirements);
            return projects;
        }
    }

    public List<ResearchProjectDef> RequiredResearch
    {
        get
        {
            if (requiredResearch is not null && requiredResearch.Count > 0)
            {
                return requiredResearch;
            }

            requiredResearch = [];
            requiredResearch.AddRange(referencedDef.researchPrerequisites);

            return requiredResearch;
        }
    }

    public List<ResearchProjectDef> CustomResearch
    {
        get
        {
            customResearchRequirements ??= [];

            customResearchDefNames ??= [];

            if (customResearchDefNames.Count == customResearchRequirements.Count)
            {
                return customResearchRequirements;
            }

            customResearchRequirements.Clear();
            foreach (var defName in customResearchDefNames)
            {
                customResearchRequirements.Add(DefDatabase<ResearchProjectDef>.GetNamed(defName));
            }

            return customResearchRequirements;
        }
    }

    /// <summary>
    ///     Bomb related values
    /// </summary>
    public bool BombCapable => bombingCapable;

    public void ExposeData()
    {
        Scribe_Values.Look(ref defaultValues, "defaultValues");
        Scribe_Values.Look(ref massCapacity, "massCapacity");
        Scribe_Values.Look(ref minPassengers, "minPassengers");
        Scribe_Values.Look(ref maxPassengers, "maxPassengers");
        Scribe_Values.Look(ref flightSpeed, "flightSpeed");
        //Scribe_Values.Look(ref this.researchPoints, "researchPoints");
        Scribe_Values.Look(ref fuelPerTile, "fuelPerTile");

        Scribe_Values.Look(ref spaceFaring, "spaceFaring");
        Scribe_Values.Look(ref shuttleBayLanding, "shuttleBayLanding");

        //Scribe_Collections.Look<string>(ref customResearchDefNames, "customResearchDefNames", LookMode.Value, new object[0]); ;

        Scribe_Values.Look(ref bombingCapable, "bombingCapable");
        Scribe_Values.Look(ref numberBombs, "numberBombs");
        Scribe_Values.Look(ref radiusDrop, "radiusDrop");
        Scribe_Values.Look(ref distanceBetweenDrops, "distanceBetweenDrops");
        Scribe_Values.Look(ref bombingSpeed, "bombingSpeed");
        Scribe_Values.Look(ref precisionBombingNumBombs, "precisionBombingNumBombs");
    }

    public void AddCustomResearch(ResearchProjectDef proj)
    {
        customResearchRequirements.Add(proj);
        customResearchDefNames.Add(proj.defName);
    }

    public void RemoveCustomResearch(ResearchProjectDef proj)
    {
        customResearchRequirements.Remove(proj);
        customResearchDefNames.Remove(proj.defName);
    }

    public void ResetCustomResearch()
    {
        if (customResearchRequirements is null || customResearchDefNames is null)
        {
            customResearchRequirements = [];
            customResearchDefNames = [];
        }

        customResearchRequirements.Clear();
        customResearchDefNames.Clear();
    }

    public void ResetToDefaultValues()
    {
        defaultValues = true;

        massCapacity = referencedDef.GetCompProperties<CompProperties_Transporter>().massCapacity;
        minPassengers = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().minPassengers;
        maxPassengers = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().maxPassengers;
        flightSpeed = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().travelSpeed;
        fuelPerTile = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().fuelPerTile;

        spaceFaring = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().spaceFaring;
        shuttleBayLanding = referencedDef.GetCompProperties<CompProperties_LaunchableSRTS>().shuttleBayLanding;

        var num = 0;
        foreach (var proj in referencedDef.researchPrerequisites)
        {
            num += (int)proj.baseCost;
        }

        researchPoints = num;
        requiredResearch ??= [];

        requiredResearch = referencedDef.researchPrerequisites;
        ResetCustomResearch();
        if (!BombCapable)
        {
            return;
        }

        bombingSpeed = referencedDef.GetCompProperties<CompProperties_BombsAway>().speed;
        numberBombs = referencedDef.GetCompProperties<CompProperties_BombsAway>().numberBombs;
        radiusDrop = referencedDef.GetCompProperties<CompProperties_BombsAway>().radiusOfDrop;
        distanceBetweenDrops = referencedDef.GetCompProperties<CompProperties_BombsAway>().distanceBetweenDrops;
        precisionBombingNumBombs =
            referencedDef.GetCompProperties<CompProperties_BombsAway>().precisionBombingNumBombs;
    }

    public bool ResetReferencedDef(string defName)
    {
        referencedDef ??= DefDatabase<ThingDef>.GetNamedSilentFail(defName);

        return referencedDef != null;
    }
}