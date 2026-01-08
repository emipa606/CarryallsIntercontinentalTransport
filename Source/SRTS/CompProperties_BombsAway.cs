using Verse;

namespace SRTS;

public class CompProperties_BombsAway : CompProperties
{
    public float distanceBetweenDrops = 5f;

    public ThingDef eastSkyfaller;

    public int numberBombs = 3;
    public int precisionBombingNumBombs = 1;
    public int radiusOfDrop = 10;
    public SoundDef soundFlyBy;
    public float speed = 0.8f;
    public ThingDef westSkyfaller;

    public CompProperties_BombsAway()
    {
        compClass = typeof(CompBombFlyer);
    }
}