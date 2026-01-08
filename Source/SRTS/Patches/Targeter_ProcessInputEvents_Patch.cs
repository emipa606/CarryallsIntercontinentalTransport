using HarmonyLib;
using RimWorld;

namespace SRTS.Patches;

[HarmonyPatch(typeof(Targeter), nameof(Targeter.ProcessInputEvents))]
public static class Targeter_ProcessInputEvents_Patch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        StartUp.ProcessBombingInputEvents();
    }
}