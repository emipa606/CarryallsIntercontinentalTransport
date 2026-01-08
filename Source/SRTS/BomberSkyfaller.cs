using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using SPExtended;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SRTS;

[StaticConstructorOnStartup]
public class BomberSkyfaller : Thing, IThingHolder
{
    private static readonly MaterialPropertyBlock shadowPropertyBlock = new();

    public float angle;

    public List<IntVec3> bombCells;

    public BombingType bombType;

    public ThingOwner innerContainer;

    public int numberOfBombs;

    public Map originalMap;

    public int precisionBombingNumBombs;

    public int radius;

    public SoundDef sound;

    public IntVec3 sourceLandingSpot;

    public float speed;

    private int ticksToExit;

    public BomberSkyfaller()
    {
        innerContainer = new ThingOwner<Thing>(this);
        bombCells = [];
    }

    public override Graphic Graphic
    {
        get
        {
            var thingForGraphic = GetThingForGraphic();
            return thingForGraphic == this
                ? base.Graphic
                : thingForGraphic.Graphic.ExtractInnerGraphicFor(thingForGraphic).GetShadowlessGraphic();
        }
    }

    public override Vector3 DrawPos
    {
        get
        {
            switch (def.skyfaller.movementType)
            {
                case SkyfallerMovementType.Accelerate:
                    return SkyfallerDrawPosUtility.DrawPos_Accelerate(base.DrawPos, ticksToExit, angle, speed);
                case SkyfallerMovementType.ConstantSpeed:
                    return SkyfallerDrawPosUtility.DrawPos_ConstantSpeed(base.DrawPos, ticksToExit, angle, speed);
                case SkyfallerMovementType.Decelerate:
                    return SkyfallerDrawPosUtility.DrawPos_Decelerate(base.DrawPos, ticksToExit, angle, speed);
                default:
                    Log.ErrorOnce($"SkyfallerMovementType not handled: {def.skyfaller.movementType}",
                        thingIDNumber ^ 1948576711);
                    return SkyfallerDrawPosUtility.DrawPos_Accelerate(base.DrawPos, ticksToExit, angle, speed);
            }
        }
    }

    public IntVec3 DrawPosCell => new((int)DrawPos.x, (int)DrawPos.y, (int)DrawPos.z);

    private Material ShadowMaterial
    {
        get
        {
            if (field is null && !def.skyfaller.shadow.NullOrEmpty())
            {
                field = MaterialPool.MatFrom(def.skyfaller.shadow, ShaderDatabase.Transparent);
            }

            return field;
        }
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        return innerContainer;
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        Scribe_References.Look(ref originalMap, "originalMap");
        Scribe_Values.Look(ref ticksToExit, "ticksToExit");
        Scribe_Values.Look(ref angle, "angle");
        Scribe_Values.Look(ref sourceLandingSpot, "sourceLandingSpot");
        Scribe_Collections.Look(ref bombCells, "bombCells", LookMode.Value);

        Scribe_Values.Look(ref numberOfBombs, "numberOfBombs");
        Scribe_Values.Look(ref speed, "speed");
        Scribe_Values.Look(ref radius, "radius");
        Scribe_Defs.Look(ref sound, "sound");
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (!respawningAfterLoad)
        {
            ticksToExit =
                Mathf.CeilToInt(
                    (float)SPExtra.Distance(new IntVec3(map.Size.x / 2, map.Size.y, map.Size.z / 2), Position) * 2 /
                    speed);
        }

        sound?.PlayOneShotOnCamera(Map);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        base.Destroy(mode);
        innerContainer.ClearAndDestroyContents();
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        var thingForGraphic = GetThingForGraphic();
        var extraRotation = angle;
        Graphic.Draw(drawLoc, !flip ? thingForGraphic.Rotation.Opposite : thingForGraphic.Rotation, thingForGraphic,
            extraRotation);
        DrawDropSpotShadow();
    }

    protected override void Tick()
    {
        try
        {
            ticksToExit--;
            if (bombCells.Any() && Math.Abs(DrawPosCell.x - bombCells.First().x) < 3 &&
                Math.Abs(DrawPosCell.z - bombCells.First().z) < 3)
            {
                DropBomb();
            }

            if (ticksToExit == 0)
            {
                ExitMap();
            }
        }
        catch (Exception ex)
        {
            Log.Error(
                $"Exception thrown while ticking {this}. Immediately sending to world to avoid loss of contents. Ex=\"{ex.Message}\"");
            ExitMap();
        }
    }

    private void DropBomb()
    {
        for (var i = 0; i < (bombType == BombingType.precise ? precisionBombingNumBombs : 1); ++i)
        {
            if (!innerContainer.Any(x =>
                    ((ActiveTransporter)x)?.Contents.innerContainer.Any(y =>
                        SRTSMod.mod.settings.allowedBombs.Contains(y.def.defName)) ?? false))
            {
                continue;
            }

            var srts = (ActiveTransporter)innerContainer.First();

            var thing = srts?.Contents.innerContainer.FirstOrDefault(y =>
                SRTSMod.mod.settings.allowedBombs.Contains(y.def.defName));
            if (thing is null)
            {
                return;
            }

            var thing2 = srts.Contents.innerContainer.Take(thing, 1);

            var bombPos = bombCells[0];
            if (bombType == BombingType.carpet)
            {
                bombCells.RemoveAt(0);
            }

            var timerTickExplode = 20 + Rand.Range(0, 5); //Change later to allow release timer
            if (SRTSHelper.CEModLoaded)
            {
                goto Block_CEPatched;
            }

            var bombThing = new FallingBomb(thing2, thing2.TryGetComp<CompExplosive>(), Map, def.skyfaller.shadow)
            {
                HitPoints = int.MaxValue,
                ticksRemaining = timerTickExplode
            };

            var c = (from x in GenRadial.RadialCellsAround(bombPos, GetCurrentTargetingRadius(), true)
                where x.InBounds(Map)
                select x).RandomElementByWeight(x =>
                1f - Mathf.Min(x.DistanceTo(Position) / GetCurrentTargetingRadius(), 1f) + 0.05f);
            bombThing.angle = angle + (SPTrig.LeftRightOfLine(DrawPosCell, Position, c) * -10);
            bombThing.speed = (float)SPExtra.Distance(DrawPosCell, c) / bombThing.ticksRemaining;
            var t = GenSpawn.Spawn(bombThing, c, Map);
            GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(t,
                thing2.TryGetComp<CompExplosive>().Props.explosiveDamageType);
            continue;

            Block_CEPatched: ;
            var CEComp = (thing2 as ThingWithComps)?.AllComps.Find(x => x.GetType().Name == "CompExplosiveCE");
            var CEbombThing = new FallingBombCE(thing2, CEComp?.props, CEComp, Map, def.skyfaller.shadow)
            {
                HitPoints = int.MaxValue,
                ticksRemaining = timerTickExplode
            };
            var c2 = (from x in GenRadial.RadialCellsAround(bombPos, GetCurrentTargetingRadius(), true)
                where x.InBounds(Map)
                select x).RandomElementByWeight(x =>
                1f - Mathf.Min(x.DistanceTo(Position) / GetCurrentTargetingRadius(), 1f) + 0.05f);
            CEbombThing.angle = angle + (SPTrig.LeftRightOfLine(DrawPosCell, Position, c2) * -10);
            CEbombThing.speed = (float)SPExtra.Distance(DrawPosCell, c2) / CEbombThing.ticksRemaining;
            _ = GenSpawn.Spawn(CEbombThing, c2, Map);
            //GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(CEt, DamageDefOf., null); /*Is GenExplosion CE compatible?*/
        }

        if (bombType == BombingType.precise && bombCells.Any())
        {
            bombCells.Clear();
        }
    }

    private void ExitMap()
    {
        var activeTransporter =
            (ActiveTransporter)ThingMaker.MakeThing(ThingDef.Named($"{def.defName.Split('_')[0]}_Active"));
        activeTransporter.Contents = new ActiveTransporterInfo();
        activeTransporter.Contents.innerContainer.TryAddRangeOrTransfer(
            ((ActiveTransporter)innerContainer.First()).Contents.innerContainer, true, true);

        var travelingTransportPods =
            (TravelingSRTS)WorldObjectMaker.MakeWorldObject(StaticDefOf.TravelingSRTS_Carryall);
        travelingTransportPods.Tile = Map.Tile;
        travelingTransportPods.SetFaction(Faction.OfPlayer);
        travelingTransportPods.destinationTile = originalMap.Tile;
        travelingTransportPods.arrivalAction =
            new TransportersArrivalAction_LandInSpecificCell(originalMap.Parent, sourceLandingSpot);
        travelingTransportPods.flyingThing = this;
        Find.WorldObjects.Add(travelingTransportPods);
        travelingTransportPods.AddTransporter(activeTransporter.Contents, true);
        Destroy();
    }


    private int GetCurrentTargetingRadius()
    {
        switch (bombType)
        {
            case BombingType.carpet:
                return radius;
            case BombingType.precise:
                return (int)(radius * 0.6f);
            case BombingType.missile:
            default:
                throw new NotImplementedException("BombingType");
        }
    }

    private Thing GetThingForGraphic()
    {
        if (def.graphicData != null || !innerContainer.Any)
        {
            return this;
        }

        return innerContainer[0];
    }

    private void DrawDropSpotShadow()
    {
        var shadowMaterial = ShadowMaterial;
        if (shadowMaterial == null)
        {
            return;
        }

        Skyfaller.DrawDropSpotShadow(base.DrawPos, Rotation, shadowMaterial, def.skyfaller.shadowSize, ticksToExit);
    }

    public static void DrawBombSpotShadow(Vector3 loc, Rot4 rot, Material material, Vector2 shadowSize, int ticksToExit)
    {
        if (rot.IsHorizontal)
        {
            Gen.Swap(ref shadowSize.x, ref shadowSize.y);
        }

        ticksToExit = Mathf.Max(ticksToExit, 0);
        var pos = loc;
        pos.y = AltitudeLayer.Shadows.AltitudeFor();
        var num = 1f + (ticksToExit / 100f);
        var s = new Vector3(num * shadowSize.x, 1f, num * shadowSize.y);
        var white = Color.white;
        if (ticksToExit > 150)
        {
            white.a = Mathf.InverseLerp(200f, 150f, ticksToExit);
        }

        shadowPropertyBlock.SetColor(ShaderPropertyIDs.Color, white);
        var matrix = default(Matrix4x4);
        matrix.SetTRS(pos, rot.AsQuat, s);
        Graphics.DrawMesh(MeshPool.plane10Back, matrix, material, 0, null, 0, shadowPropertyBlock);
    }
}