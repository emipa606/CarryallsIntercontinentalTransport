using System;
using HarmonyLib;
using Verse;

namespace SRTS;

[StaticConstructorOnStartup]
public class FallingBombCE : FallingBomb
{
    private readonly ThingComp CEcomp;

    private CompProperties CEprops;

    private float explosionRadius;

    public FallingBombCE(Thing reference, CompProperties comp, ThingComp CEcomp, Map map, string texPathShadow)
    {
        if (!SRTSHelper.CEModLoaded)
        {
            throw new NotImplementedException(
                "Calling wrong constructor. This is only enabled for Combat Extended calls. - Smash Phil");
        }

        def = reference.def;
        thingIDNumber = reference.thingIDNumber;
        this.map = map;
        factionInt = reference.Faction;
        graphicInt = reference.DefaultGraphic;
        hitPointsInt = reference.HitPoints;
        CEprops = comp;
        this.CEcomp = CEcomp;
        explosionRadius = (float)AccessTools.Field(SRTSHelper.CompProperties_ExplosiveCE, "explosionRadius")
            .GetValue(comp);
        this.texPathShadow = texPathShadow;
    }

    protected override void Tick()
    {
        if (ticksRemaining < 0)
        {
            ExplodeOnImpact();
        }

        ticksRemaining--;
    }

    protected override void ExplodeOnImpact()
    {
        if (!SpawnedOrAnyParentSpawned)
        {
            return;
        }

        AccessTools.Method(SRTSHelper.CompExplosiveCE, "Explode").Invoke(CEcomp, [
            this, Position.ToVector3(), Map, 1f
        ]);
        Destroy();
    }
}