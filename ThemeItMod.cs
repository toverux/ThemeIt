using CitiesHarmony.API;
using ICities;

namespace ThemeIt {
    public class ThemeItMod : IUserMod {
        public string Name => "Theme It";

        public string Description => "Create themes for growables and apply them to cities and districts.";

        public void OnEnabled() {
            HarmonyHelper.DoOnHarmonyReady(HarmonyPatcher.PatchAll);
        }

        public void OnDisabled() {
            if (HarmonyHelper.IsHarmonyInstalled) {
                HarmonyPatcher.UnpatchAll();
            }
        }
    }
}
