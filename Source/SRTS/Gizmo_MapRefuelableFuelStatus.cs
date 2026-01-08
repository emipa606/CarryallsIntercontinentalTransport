using System;
using UnityEngine;
using Verse;

namespace SRTS;

[StaticConstructorOnStartup]
public class Gizmo_MapRefuelableFuelStatus : Gizmo
{
    private const float ArrowScale = 0.5f;

    private static readonly Texture2D FullBarTex =
        SolidColorMaterials.NewSolidColorTexture(new Color(0.35f, 0.35f, 0.2f));

    private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(Color.black);

    private static readonly Texture2D
        TargetLevelArrow = ContentFinder<Texture2D>.Get("UI/Misc/BarInstantMarkerRotated");

    public string compLabel;
    public float maxFuel;
    public float nowFuel;

    public override float GetWidth(float maxWidth)
    {
        return 140f;
    }

    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
    {
        var overRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
        Find.WindowStack.ImmediateWindow(1523289473, overRect, WindowLayer.GameUI, (Action)(() =>
        {
            var rect1 = overRect.AtZero().ContractedBy(6f);
            var rect2 = rect1;
            rect2.height = overRect.height / 2f;
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect2, compLabel);
            var rect3 = rect1;
            rect3.yMin = overRect.height / 2f;
            var fillPercent = nowFuel / maxFuel;
            Widgets.FillableBar(rect3, fillPercent, FullBarTex, EmptyBarTex, false);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect3, $"{nowFuel:F0} / {maxFuel:F0}");
            Text.Anchor = TextAnchor.UpperLeft;
        }));
        return new GizmoResult(GizmoState.Clear);
    }
}