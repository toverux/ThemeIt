using HarmonyLib;
using JetBrains.Annotations;
using ThemeIt.GUI;

namespace ThemeIt.Patches;

[HarmonyPatch(typeof(DistrictWorldInfoPanel))]
internal static class DistrictWorldInfoPanelPatches {
    [HarmonyPostfix, UsedImplicitly]
    [HarmonyPatch("Start")]
    internal static void PostfixStart(DistrictWorldInfoPanel __instance) {
        var logger = Locator.Current.Find<ThemeItMod>().Logger;

        var districtInfoPanelManager = new DistrictInfoPanelManager(logger, __instance);

        districtInfoPanelManager.Install();

        Locator.Current.Register(districtInfoPanelManager);
    }
}
