using Verse;

namespace SRTS;

public class CompProperties_LaunchableSRTS : CompProperties
{
    public ThingDef eastSkyfaller;

    public ThingDef eastSkyfallerActive;

    public ThingDef eastSkyfallerIncoming;

    public float fuelPerTile = 2.25f;
    public bool hasSelfDestruct;
    public int maxPassengers = 2;
    public int minPassengers = 1;

    public bool needsConfirmation;
    public bool shuttleBayLanding;

    /* SOS2 Compatibility */
    public bool spaceFaring;

    public float travelSpeed = 25f;
    public ThingDef westSkyfaller;
    public ThingDef westSkyfallerActive;
    public ThingDef westSkyfallerIncoming;

    public CompProperties_LaunchableSRTS()
    {
        compClass = typeof(CompLaunchableSRTS);
    }
    /* ------------------ */
}