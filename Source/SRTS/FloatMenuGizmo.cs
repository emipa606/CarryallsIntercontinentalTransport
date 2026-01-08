using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SRTS;

public class FloatMenuGizmo(List<FloatMenuOption> options, Thing srtsSelected, string title, Vector3 clickPos)
    : FloatMenu(options, title)
{
    public const int RevalidateEveryFrame = 3;

    private Vector3 clickPos = clickPos;

    public override void DoWindowContents(Rect rect)
    {
        if (srtsSelected is null || srtsSelected.Destroyed)
        {
            Find.WindowStack.TryRemove(this);
            return;
        }

        base.DoWindowContents(rect);
    }
}