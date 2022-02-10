using HarmonyLib;
using JetBrains.Annotations;
using ModsCommon;
using ThemeIt.GUI;

namespace ThemeIt.Patches;

[HarmonyPatch(typeof(DistrictWorldInfoPanel))]
internal static class DistrictWorldInfoPanelPatches {
    [HarmonyPostfix, UsedImplicitly]
    [HarmonyPatch("Start")]
    internal static void PostfixStart(DistrictWorldInfoPanel __instance) {
        var locator = SingletonItem<ThemeItMod>.Instance.Locator;

        locator.Find<DistrictInfoPanelManager>().Install(__instance);
    }
}
