using RimWorld;
using UnityEngine;
using Verse;

namespace SRTS;

[StaticConstructorOnStartup]
public class FallingBomb : ThingWithComps
{
    protected static MaterialPropertyBlock shadowPropertyBlock = new();

    private readonly float explosionRadius;

    private readonly ProjectileProperties projectile;

    private readonly CompProperties_Explosive props;

    public float angle;

    protected Material cachedShadowMaterial;

    protected Graphic graphicInt;

    protected int hitPointsInt = -1;

    protected Map map;

    public float speed;

    protected string texPathShadow;

    public int ticksRemaining;

    public FallingBomb()
    {
    }

    public FallingBomb(Thing reference, CompExplosive comp, Map map, string texPathShadow)
    {
        def = reference.def;
        projectile = def.projectileWhenLoaded.projectile;
        thingIDNumber = reference.thingIDNumber;
        this.map = map;
        factionInt = reference.Faction;
        graphicInt = reference.DefaultGraphic;
        hitPointsInt = reference.HitPoints;
        props = reference.TryGetComp<CompExplosive>().Props;
        explosionRadius = projectile?.explosionRadius != null
            ? def.projectileWhenLoaded.projectile.explosionRadius
            : comp.ExplosiveRadius();

        this.texPathShadow = texPathShadow;
    }

    public override Vector3 DrawPos => DrawBombFalling(base.DrawPos, ticksRemaining, angle, speed);

    protected Material ShadowMaterial
    {
        get
        {
            if (texPathShadow != null && !texPathShadow.NullOrEmpty())
            {
                cachedShadowMaterial = MaterialPool.MatFrom(texPathShadow, ShaderDatabase.Transparent);
            }

            return cachedShadowMaterial;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksRemaining, "ticksRemaining");
        Scribe_Values.Look(ref angle, "angle");
        Scribe_Values.Look(ref texPathShadow, "cachedShadowMaterial");
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        var angleDropped = angle - 45f;
        Graphic.Draw(drawLoc, !flip ? Rotation.Opposite : Rotation, this, angleDropped);
        DrawDropSpotShadow();
    }

    protected void DrawDropSpotShadow()
    {
        if (ShadowMaterial is null)
        {
            return;
        }

        DrawDropSpotShadow(base.DrawPos, Rotation, ShadowMaterial, new Vector2(RotatedSize.x, RotatedSize.z),
            ticksRemaining);
    }

    protected static void DrawDropSpotShadow(Vector3 center, Rot4 rot, Material material, Vector2 shadowSize,
        int ticksToImpact)
    {
        if (rot.IsHorizontal)
        {
            Gen.Swap(ref shadowSize.x, ref shadowSize.y);
        }

        ticksToImpact = Mathf.Max(ticksToImpact, 0);
        var pos = center;
        pos.y = AltitudeLayer.Shadows.AltitudeFor();
        var num = 1f + (ticksToImpact / 100f);
        var s = new Vector3(num * shadowSize.x, 1f, num * shadowSize.y);
        var white = Color.white;
        if (ticksToImpact > 150)
        {
            white.a = Mathf.InverseLerp(200f, 150f, ticksToImpact);
        }

        shadowPropertyBlock.SetColor(ShaderPropertyIDs.Color, white);
        Matrix4x4 matrix = default;
        matrix.SetTRS(pos, rot.AsQuat, s);
        Graphics.DrawMesh(MeshPool.plane10Back, matrix, material, 0, null, 0, shadowPropertyBlock);
    }

    protected override void Tick()
    {
        if (ticksRemaining < 0)
        {
            ExplodeOnImpact();
        }

        ticksRemaining--;
    }

    protected virtual void ExplodeOnImpact()
    {
        if (!SpawnedOrAnyParentSpawned)
        {
            return;
        }

        if (props.destroyThingOnExplosionSize <= explosionRadius && !Destroyed)
        {
            Kill();
        }

        if (props.explosionEffect != null)
        {
            var effecter = props.explosionEffect.Spawn();
            effecter.Trigger(new TargetInfo(PositionHeld, map), new TargetInfo(PositionHeld, map));
            effecter.Cleanup();
        }

        if (def.projectileWhenLoaded?.projectile != null)
        {
            GenExplosion.DoExplosion(PositionHeld, map, explosionRadius, projectile.damageDef, this,
                projectile.GetDamageAmount(this), projectile.GetArmorPenetration(this),
                projectile.soundExplode, projectile: def.projectileWhenLoaded,
                postExplosionSpawnThingDef: projectile.postExplosionSpawnThingDef,
                postExplosionSpawnChance: projectile.postExplosionSpawnChance,
                postExplosionSpawnThingCount: projectile.postExplosionSpawnThingCount,
                applyDamageToExplosionCellsNeighbors: projectile.applyDamageToExplosionCellsNeighbors,
                preExplosionSpawnThingDef: projectile.preExplosionSpawnThingDef,
                preExplosionSpawnChance: projectile.preExplosionSpawnChance,
                preExplosionSpawnThingCount: projectile.preExplosionSpawnThingCount,
                chanceToStartFire: projectile.explosionChanceToStartFire,
                damageFalloff: projectile.explosionDamageFalloff);
        }
        else
        {
            GenExplosion.DoExplosion(PositionHeld, map, explosionRadius, props.explosiveDamageType, this,
                props.damageAmountBase, props.armorPenetrationBase,
                props.explosionSound,
                postExplosionSpawnThingDef: props.postExplosionSpawnThingDef,
                postExplosionSpawnChance: props.postExplosionSpawnChance,
                postExplosionSpawnThingCount: props.postExplosionSpawnThingCount,
                applyDamageToExplosionCellsNeighbors: props.applyDamageToExplosionCellsNeighbors,
                preExplosionSpawnThingDef: props.preExplosionSpawnThingDef,
                preExplosionSpawnChance: props.preExplosionSpawnChance,
                preExplosionSpawnThingCount: props.preExplosionSpawnThingCount,
                chanceToStartFire: props.chanceToStartFire, damageFalloff: props.damageFalloff);
        }
    }

    public static Vector3 DrawBombFalling(Vector3 center, int ticksToImpact, float angle, float speed)
    {
        var dist = ticksToImpact * speed;
        return center + (Vector3Utility.FromAngleFlat(angle - 90f) * dist);
    }
}