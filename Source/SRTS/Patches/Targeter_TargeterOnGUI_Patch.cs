using HarmonyLib;
using RimWorld;

namespace SRTS.Patches;

[HarmonyPatch(typeof(Targeter), nameof(Targeter.TargeterOnGUI))]
public static class Targeter_TargeterOnGUI_Patch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        StartUp.DrawBombingTargeter();
    }
}