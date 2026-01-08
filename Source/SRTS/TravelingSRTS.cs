using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SRTS;

public class TravelingSRTS : TravellingTransporters
{
    private const float ExpandingResize = 35f;

    private const float TransitionTakeoff = 0.015f;

    //
    public Thing flyingThing;
    private float transitionSize = 0f;

    private Material SRTSMaterial
    {
        get
        {
            if (flyingThing is null)
            {
                return Material;
            }

            if (flyingThing.Rotation == Rot4.West)
            {
                return flyingThing.Graphic.MatEast;
            }

            if (flyingThing.Rotation == Rot4.East)
            {
                return flyingThing.Graphic.MatWest;
            }

            if (field is not null)
            {
                return ExpandableWorldObjectsUtility.TransitionPct(this) > 0 ? flyingThing.Graphic.MatNorth : field;
            }

            var texPath = flyingThing.def.graphicData.texPath;
            var graphicRequest = new GraphicRequest(flyingThing.def.graphicData.graphicClass,
                flyingThing.def.graphicData.texPath, ShaderTypeDefOf.Cutout.Shader,
                flyingThing.def.graphic.drawSize,
                Color.white, Color.white, flyingThing.def.graphicData, 0, null, null);
            if (graphicRequest.graphicClass == typeof(Graphic_Multi))
            {
                texPath += "_north";
            }

            field = MaterialPool.MatFrom(texPath, ShaderDatabase.WorldOverlayTransparentLit,
                WorldMaterials.WorldObjectRenderQueue);

            return ExpandableWorldObjectsUtility.TransitionPct(this) > 0 ? flyingThing.Graphic.MatNorth : field;
        }
    }

    public Vector3 Direction => (DrawPos - Find.WorldGrid.GetTileCenter(destinationTile)).normalized;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref flyingThing, "flyingThing");
    }

    public override void Draw()
    {
        var pos = DrawPos;
        var normalized = pos.normalized;
        var size = Mathf.Lerp(0.5f, 1f, ExpandableWorldObjectsUtility.TransitionPct(this));
        size = Mathf.Lerp(size, 1.5f, ExpandableWorldObjectsUtility.ExpandMoreTransitionPct);

        var q = Quaternion.LookRotation(Vector3.Cross(normalized, Vector3.up), normalized) *
                Quaternion.Euler(0, Direction.AngleFlat(), 0);
        var s = new Vector3(size, 1f, size);
        var matrix = default(Matrix4x4);
        matrix.SetTRS(pos + (normalized * 0.015f), q, s);
        Graphics.DrawMesh(MeshPool.plane10, matrix, Material, WorldCameraManager.WorldLayer);
    }
}