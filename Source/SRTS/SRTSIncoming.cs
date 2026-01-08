using RimWorld;
using Verse;

namespace SRTS;

public class SRTSIncoming : Skyfaller, IActiveTransporter
{
    public Rot4 SRTSRotation
    {
        set => field = value;
    }

    public ActiveTransporterInfo Contents => ((ActiveTransporter)innerContainer[0]).Contents;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        Rotation = Rot4.East;
    }

    protected override void Impact()
    {
        for (var i = 0; i < 6; i++)
        {
            var loc = Position.ToVector3Shifted() + Gen.RandomHorizontalVector(1f);
            FleckMaker.ThrowDustPuff(loc, Map, 1.2f);
        }

        FleckMaker.ThrowLightningGlow(Position.ToVector3Shifted(), Map, 2f);
        GenClamor.DoClamor(this, 15f, ClamorDefOf.Impact);
        base.Impact();
    }
}