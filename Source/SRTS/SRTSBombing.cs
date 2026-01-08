using RimWorld;
using Verse;

namespace SRTS;

public class SRTSBombing : BomberSkyfaller, IActiveTransporter
{
    public Rot4 SRTSRotation { get; set; }

    public ActiveTransporterInfo Contents
    {
        get => ((ActiveTransporter)innerContainer[0]).Contents;
        set => ((ActiveTransporter)innerContainer[0]).Contents = value;
    }
}