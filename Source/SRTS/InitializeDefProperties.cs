using Verse;

namespace SRTS;

[StaticConstructorOnStartup]
internal static class InitializeDefProperties
{
    static InitializeDefProperties()
    {
        SRTSMod.mod.settings.CheckDictionarySavedValid();
        SRTS_ModSettings.CheckNewDefaultValues();
        SRTSHelper.PopulateDictionary();
        SRTSHelper.PopulateAllowedBombs();

        //ModCompatibilityInitialized();
    }
}