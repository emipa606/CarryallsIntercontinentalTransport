using System;
using System.Collections.Generic;
using RimWorld;
using SPExtended;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SRTS;

public class BombingTargeter
{
    private const float RadiusPreciseMultiplier = 0.6f;

    private Action<IEnumerable<IntVec3>, Pair<IntVec3, IntVec3>> action;

    private Action actionWhenFinished;

    private ThingDef bomber;

    private BombingType bombType;

    private Pawn caster;

    private Map map;

    private Texture2D mouseAttachment;

    private int numRings;

    private List<LocalTargetInfo> selections = [];

    private float targetingLength;

    private TargetingParameters targetParams;
    private bool IsTargeting => action != null;

    public void BeginTargeting(TargetingParameters targetParams,
        Action<IEnumerable<IntVec3>, Pair<IntVec3, IntVec3>> action, ThingDef bomber, BombingType bombType, Map map,
        Pawn caster = null, Action actionWhenFinished = null, Texture2D mouseAttachment = null)
    {
        this.action = action;
        this.targetParams = targetParams;
        this.caster = caster;
        this.actionWhenFinished = actionWhenFinished;
        this.mouseAttachment = mouseAttachment;
        selections = [];
        this.bomber = bomber;
        this.map = map;
        this.bombType = bombType;
    }

    public void StopTargeting()
    {
        if (actionWhenFinished != null)
        {
            var whenFinished = actionWhenFinished;
            actionWhenFinished = null;
            whenFinished();
        }

        action = null;
        selections.Clear();
    }

    public void ProcessInputEvents()
    {
        ConfirmStillValid();
        if (!IsTargeting)
        {
            return;
        }

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            Event.current.Use();
            if (selections.Count < 2 && action != null)
            {
                var obj = CurrentTargetUnderMouse();
                if (obj.Cell.InBounds(map) && !selections.Contains(obj))
                {
                    selections.Add(obj);
                }
                else
                {
                    SoundDefOf.ClickReject.PlayOneShotOnCamera();
                    return;
                }
            }

            SoundDefOf.Tick_High.PlayOneShotOnCamera();
            if (selections.Count == 2)
            {
                action?.Invoke(BombCellsFinalized(),
                    new Pair<IntVec3, IntVec3>(selections[0].Cell, selections[1].Cell));
                StopTargeting();
            }
        }

        if ((Event.current.type != EventType.MouseDown || Event.current.button != 1) &&
            !KeyBindingDefOf.Cancel.KeyDownEvent)
        {
            return;
        }

        if (selections.Any())
        {
            selections.RemoveLast();
        }
        else
        {
            StopTargeting();
        }

        SoundDefOf.CancelMode.PlayOneShotOnCamera();
        Event.current.Use();
    }

    public void TargeterOnGUI()
    {
        if (action == null)
        {
            return;
        }

        var icon = mouseAttachment ?? Tex2D.LauncherTargeting;
        GenUI.DrawMouseAttachment(icon);
    }

    public void TargeterUpdate()
    {
        if (!selections.Any())
        {
            return;
        }

        GenDraw.DrawLineBetween(selections[0].CenterVector3, UI.MouseMapPosition().ToIntVec3().ToVector3Shifted(),
            SimpleColor.Red);
        DrawTargetingPoints();
    }

    private void DrawTargetingPoints()
    {
        targetingLength = Vector3.Distance(selections[0].CenterVector3,
            UI.MouseMapPosition().ToIntVec3().ToVector3Shifted());
        GenDraw.DrawTargetHighlight(new LocalTargetInfo(selections[0].Cell));
        if (bombType == BombingType.carpet)
        {
            GenDraw.DrawRadiusRing(selections[0].Cell, SRTSMod.GetStatFor<int>(bomber.defName, StatName.radiusDrop));

            numRings =
                ((int)(targetingLength / SRTSMod.GetStatFor<float>(bomber.defName, StatName.distanceBetweenDrops)))
                .Clamp(0, SRTSMod.GetStatFor<int>(bomber.defName, StatName.numberBombs));

            if (SRTSMod.mod.settings.expandBombPoints && numRings >= 1)
            {
                GenDraw.DrawRadiusRing(UI.MouseMapPosition().ToIntVec3(),
                    SRTSMod.GetStatFor<int>(bomber.defName, StatName.radiusDrop));
                GenDraw.DrawTargetHighlight(new LocalTargetInfo(UI.MouseMapPosition().ToIntVec3()));
            }

            for (var i = 1; i < numRings - (SRTSMod.mod.settings.expandBombPoints ? 1 : 0); i++)
            {
                var cellTargeted = TargeterToCell(i);
                GenDraw.DrawRadiusRing(cellTargeted, SRTSMod.GetStatFor<int>(bomber.defName, StatName.radiusDrop));
                GenDraw.DrawTargetHighlight(new LocalTargetInfo(cellTargeted));
            }
        }
        else if (bombType == BombingType.precise)
        {
            var centeredTarget = TargeterCentered();
            GenDraw.DrawTargetHighlight(new LocalTargetInfo(centeredTarget));
            GenDraw.DrawTargetHighlight(new LocalTargetInfo(UI.MouseMapPosition().ToIntVec3()));
            GenDraw.DrawRadiusRing(centeredTarget,
                SRTSMod.GetStatFor<int>(bomber.defName, StatName.radiusDrop) * RadiusPreciseMultiplier);
        }
    }

    private IntVec3 TargeterToCell(int bombNumber)
    {
        var mousePosition = UI.MouseMapPosition().ToIntVec3();
        var targetedCell = new IntVec3(mousePosition.x, selections[0].Cell.y, mousePosition.z);
        var angle = selections[0].Cell.AngleToPoint(targetedCell);
        var distanceToNextBomb = SRTSMod.mod.settings.expandBombPoints
            ? targetingLength / (numRings - 1) * bombNumber
            : SRTSMod.GetStatFor<float>(bomber.defName, StatName.distanceBetweenDrops) * bombNumber;
        var xDiff = selections[0].Cell.x + (Math.Sign(UI.MouseMapPosition().x - selections[0].CenterVector3.x) *
                                            (float)(distanceToNextBomb * Math.Cos(angle.DegreesToRadians())));
        var zDiff = selections[0].Cell.z + (Math.Sign(UI.MouseMapPosition().z - selections[0].CenterVector3.z) *
                                            (float)(distanceToNextBomb * Math.Sin(angle.DegreesToRadians())));
        return new IntVec3((int)xDiff, 0, (int)zDiff);
    }

    private IntVec3 TargeterCentered()
    {
        var mousePos = UI.MouseMapPosition().ToIntVec3();
        var targetedCell = new IntVec3(mousePos.x, selections[0].Cell.y, mousePos.z);
        var angle = selections[0].Cell.AngleToPoint(mousePos);
        var xDiff = selections[0].Cell.x + (Math.Sign(targetedCell.x - selections[0].CenterVector3.x) *
                                            (float)(targetingLength / 2 * Math.Cos(angle.DegreesToRadians())));
        var zDiff = selections[0].Cell.z + (Math.Sign(targetedCell.z - selections[0].CenterVector3.z) *
                                            (float)(targetingLength / 2 * Math.Sin(angle.DegreesToRadians())));
        return new IntVec3((int)xDiff, 0, (int)zDiff);
    }

    private IEnumerable<IntVec3> BombCellsFinalized()
    {
        if (bombType == BombingType.carpet)
        {
            for (var i = 0; i < numRings; i++)
            {
                yield return TargeterToCell(i);
            }
        }
        else if (bombType == BombingType.precise)
        {
            yield return TargeterCentered();
        }
    }

    private void ConfirmStillValid()
    {
        if (caster != null && (caster.Map != Find.CurrentMap || caster.Destroyed || !Find.Selector.IsSelected(caster)))
        {
            StopTargeting();
        }
    }

    private LocalTargetInfo CurrentTargetUnderMouse()
    {
        if (!IsTargeting)
        {
            return LocalTargetInfo.Invalid;
        }

        var localTarget = LocalTargetInfo.Invalid;
        using var enumerator = GenUI.TargetsAtMouse(targetParams).GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return localTarget;
        }

        var localTarget2 = enumerator.Current;
        localTarget = localTarget2;

        return localTarget;
    }
}