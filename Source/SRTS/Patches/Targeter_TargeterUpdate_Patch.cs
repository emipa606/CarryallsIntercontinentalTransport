using HarmonyLib;
using RimWorld;

namespace SRTS.Patches;

[HarmonyPatch(typeof(Targeter), nameof(Targeter.TargeterUpdate))]
public static class Targeter_TargeterUpdate_Patch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        StartUp.BombTargeterUpdate();
    }
}