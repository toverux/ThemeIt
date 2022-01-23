using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ThemeIt {
    public static class HarmonyPatcher {
        private const string HarmonyId = "toverux/ThemeIt";

        private static bool patched;

        public static void PatchAll() {
            if (HarmonyPatcher.patched) {
                return;
            }


            HarmonyPatcher.patched = true;

            var harmony = new Harmony(HarmonyPatcher.HarmonyId);

            harmony.PatchAll(Assembly.GetExecutingAssembly());
            
            Debug.Log("ThemeIt: Game routines have been patched.");
        }

        public static void UnpatchAll() {
            if (!HarmonyPatcher.patched) {
                return;
            }

            var harmony = new Harmony(HarmonyPatcher.HarmonyId);

            harmony.UnpatchAll(HarmonyPatcher.HarmonyId);

            HarmonyPatcher.patched = false;

            Debug.Log("ThemeIt: Game routines have been restored.");
        }
    }

    [HarmonyPatch(typeof(SimulationManager), "CreateRelay")]
    public static class SimulationManagerCreateRelayPatch {
        public static void Prefix() {
            Debug.Log("ThemeIt: CreateRelay Prefix");
        }
    }

    [HarmonyPatch(typeof(LoadingManager), "MetaDataLoaded")]
    public static class LoadingManagerMetaDataLoadedPatch {
        public static void Prefix() {
            Debug.Log("ThemeIt: MetaDataLoaded Prefix");
        }
    }
}
