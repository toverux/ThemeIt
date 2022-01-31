using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ColossalFramework.UI;
using HarmonyLib;
using JetBrains.Annotations;
using UIUtils = ThemeIt.GUI.UIUtils;

namespace ThemeIt.Patches;

[HarmonyPatch(typeof(PoliciesPanel)), UsedImplicitly]
public static class PoliciesPanelPatches {
    private static readonly MethodInfo UiTabStripTabCountMethod = AccessTools.PropertyGetter(
        typeof(UITabstrip),
        nameof(UITabstrip.tabCount));

    private static UICheckBox? enableThemeManagementCheckBox;

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
    public static IEnumerable<CodeInstruction> TranspileRefreshPanel(IEnumerable<CodeInstruction> instructions) {
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
                instruction.Is(OpCodes.Callvirt, PoliciesPanelPatches.UiTabStripTabCountMethod)) {
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
    [HarmonyPatch("Awake")]
    public static void PostfixAwake(PoliciesPanel __instance) {
        //=> Find the list of tabs in policies panel
        var tabStrip = __instance.Find<UITabstrip>("Tabstrip");

        //=> Add a custom tab
        var tab = tabStrip.AddTab("Themes");
        // Base game changes those tabs' text scale
        tab.textScale = ((UIButton) tabStrip.tabs[0]).textScale;

        //=> this.SetParentButton expects all tabs to have a TutorialUITag, we add an empty one. 
        tab.gameObject.AddComponent<TutorialUITag>();

        //=> Initialize tab contents
        // The container for the policies was created by the game when we added the tab
        var pageIndex = tabStrip.tabPages.childCount - 1;
        var container = (UIPanel) tabStrip.tabPages.components[pageIndex];

        container.autoLayout = true;
        container.autoLayoutDirection = LayoutDirection.Vertical;
        container.autoLayoutPadding.top = 5;

        //=> Only make the tab visible if our tab was selected when the panel was closed last time
        container.isVisible = tabStrip.selectedIndex == pageIndex;

        //=> The panel holding the other controls
        var controls = container.AddUIComponent<UIPanel>();

        controls.width = container.width;
        controls.autoLayout = true;
        controls.autoLayoutDirection = LayoutDirection.Vertical;
        controls.autoLayoutPadding.top = 5;

        //=> Add a checkbox to "Enable Theme Management for this district"
        PoliciesPanelPatches.enableThemeManagementCheckBox = UIUtils.CreateCheckBox(controls);
        PoliciesPanelPatches.enableThemeManagementCheckBox.name = "Theme Management Checkbox";

        //=> Add a button to show the Building Theme Manager
        var showThemeManager = UIUtils.CreateButton(controls);
        showThemeManager.width = controls.width;
        showThemeManager.text = "Theme Manager";
    }

    [HarmonyPostfix, UsedImplicitly]
    [HarmonyPatch(nameof(PoliciesPanel.Set), typeof(byte))]
    public static void PostfixSet(byte district) {
        if (PoliciesPanelPatches.enableThemeManagementCheckBox is not null) {
            PoliciesPanelPatches.enableThemeManagementCheckBox.text = district == 0
                ? "Enable Theme Management for this city"
                : "Enable Theme Management for this district";
        }
    }
}
