using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ColossalFramework.UI;
using HarmonyLib;
using JetBrains.Annotations;
using ThemeIt.GUI;

namespace ThemeIt.Patches;

[HarmonyPatch(typeof(PoliciesPanel)), UsedImplicitly]
internal static class PoliciesPanelPatches {
    private static readonly MethodInfo UiTabStripTabCountMethod = AccessTools.PropertyGetter(
        typeof(UITabstrip),
        nameof(UITabstrip.tabCount));

    [HarmonyPostfix, UsedImplicitly]
    [HarmonyPatch("Awake")]
    internal static void PostfixAwake(PoliciesPanel __instance) {
        ThemesTabBuilder.SetupThemesTab(__instance);
    }

    /**
     * Base game iterates over policies panel tabs and uses their name to hydrate them dynamically with a list of
     * policies.
     * This makes it crash when we add a tab that's not related to a predefined group of policies, that we can't change.
     * This transpiler changes the incriminated for() condition to avoid iterating on the latest tab, the one we
     * inserted. For that we basically insert a "- 1" in CIL after this.m_Tabstrip.tabCount, the iteration limit, so
     * that is ignored.
     */
    [HarmonyTranspiler, UsedImplicitly]
    [HarmonyPatch("RefreshPanel")]
    internal static IEnumerable<CodeInstruction> TranspileRefreshPanel(IEnumerable<CodeInstruction> instructions) {
        var foundTabIndexLdloc3 = false;
        var targetSiteFound = false;

        foreach (var instruction in instructions) {
            if (instruction.opcode == OpCodes.Ldloc_3) {
                //=> We found the index variable for tabs iteration in the for() that causes a problem.
                //   Next time we found a callvirt to tabCount getter, we can inject our code.
                //   This is a fragile method of detection, if local variables change in the future and that index
                //   variable is no longer third in the list... We may want to use a smarter matching logic here.
                foundTabIndexLdloc3 = true;

                yield return instruction;
            }
            else if (
                !targetSiteFound &&
                foundTabIndexLdloc3 &&
                instruction.Calls(PoliciesPanelPatches.UiTabStripTabCountMethod)) {
                //=> We found the target site, the reading of tabCount, to which we must subtract 1.

                targetSiteFound = true;

                yield return instruction; // the callvirt, pushes the tab count
                yield return new CodeInstruction(OpCodes.Ldc_I4_1); // push 1
                yield return new CodeInstruction(OpCodes.Sub_Ovf_Un); // subtract that 1 off tabCount
            }
            else {
                //=> Any other code.
                yield return instruction;
            }
        }

        if (!targetSiteFound) {
            throw new Exception("Cannot find target site to apply patch on.");
        }
    }

    [HarmonyPostfix, UsedImplicitly]
    [HarmonyPatch(nameof(PoliciesPanel.Set), typeof(byte))]
    internal static void PostfixSet(byte district) {
        ThemesTabBuilder.SetCurrentDistrict(district);
    }
}
