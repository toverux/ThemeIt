using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ColossalFramework.UI;
using HarmonyLib;
using JetBrains.Annotations;
using ModsCommon;
using ModsCommon.UI;
using UnityEngine;
using ILogger = ModsCommon.ILogger;
using UIUtils = ThemeIt.GUI.UIUtils;

namespace ThemeIt.Patches;

[HarmonyPatch(typeof(PoliciesPanel)), UsedImplicitly]
public static class PoliciesPanelPatches {
    private static readonly MethodInfo UiTabStripTabCountMethod = AccessTools.PropertyGetter(
        typeof(UITabstrip),
        nameof(UITabstrip.tabCount));

    private static ILogger Logger => SingletonMod<ThemeItMod>.Logger;

    private static UITabstrip? FindTabstrip(this UICustomControl panel) =>
        panel.Find<UITabstrip>("Tabstrip");

    private static UIScrollbar? FindScrollbar(this UICustomControl panel) =>
        panel.Find<UIScrollbar>("Scrollbar");

    private static UITabContainer? FindPolicyContainer(this UICustomControl panel) =>
        panel.Find<UITabContainer>("PolicyContainer");

    private static PoliciesPanel? currentInstance;

    private static UICheckBox? currentThemeManagementCheckBox;

    private static float originalPolicyContainerWidth;

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
    [HarmonyPatch("Awake")]
    public static void PostfixAwake(PoliciesPanel __instance) {
        var tabstrip = __instance.FindTabstrip();
        var scrollbar = __instance.FindScrollbar();
        var policyContainer = __instance.FindPolicyContainer();

        if (policyContainer is null || tabstrip is null) {
            PoliciesPanelPatches.Logger.Error("Cannot find PolicyContainer or Tabstrip, not updating Policies tabs.");
            return;
        }

        if (scrollbar is null) {
            PoliciesPanelPatches.Logger.Error("Cannot find PoliciesPanel scrollbar, Themes tab may be broken.");
        }

        PoliciesPanelPatches.currentInstance = __instance;
        PoliciesPanelPatches.originalPolicyContainerWidth = policyContainer.width;

        //=> A margin to apply on the container block and between elements, based on what the game uses in that panel.
        var spacing = policyContainer.relativePosition.x;

        //=> Add a custom tab.
        var tab = tabstrip.AddTab("Themes");
        tab.name = "ThemesStrip";

        //=> Set some properties to mimic these particular tab instance in the policies panel.
        tab.textScale = ((UIButton) tabstrip.tabs[0]).textScale;
        tab.playAudioEvents = true;

        //=> PoliciesPanel.SetParentButton expects all tabs to have a TutorialUITag, we add an empty one.
        tab.gameObject.AddComponent<TutorialUITag>();

        //=> Listen for tab activation events, to change/restore the container width based on the tab kind.
        //   It should not be necessary to remove the listener on this instance (destroyed together).
        tabstrip.eventSelectedIndexChanged += PoliciesPanelPatches.OnTabStripSelectedIndexChanged;

        //=> Initialize tab contents.
        // The container for the policies was created by the game when we added the tab.
        var tabPanel = tabstrip.tabPages.childCount - 1;
        var pagePanel = (UIPanel) tabstrip.tabPages.components[tabPanel];

        //=> Only make the tab visible if our tab was selected when the panel was closed last time.
        pagePanel.isVisible = tabstrip.selectedIndex == tabPanel;

        //=> Eat the space of the unused and disabled scrollbar that only the other tabs use.
        //   We need to change the size of the container so the child can be expanded, otherwise the width is clamped.
        //   But when switching tabs, the container's width will be changed/restored, according to the tab kind.
        if (scrollbar is not null) {
            policyContainer.width += scrollbar.width;
            pagePanel.width += scrollbar.width;
        }

        //=> Layout from top to bottom.
        //=> Add checkbox for theme management activation on city/district.
        var themeManagementCheckBox = UIUtils.CreateCheckBox(pagePanel);
        themeManagementCheckBox.relativePosition = new Vector2(0, spacing);
        themeManagementCheckBox.name = "ThemeManagementCheckbox";
        themeManagementCheckBox.width = pagePanel.width - spacing;

        PoliciesPanelPatches.currentThemeManagementCheckBox = themeManagementCheckBox;

        //=> Add a spacer
        var spacer1 = AddSpacer(pagePanel);
        spacer1.relativePosition =
            new Vector2(0, themeManagementCheckBox.relativePosition.y + themeManagementCheckBox.size.y + spacing);

        //=> Layout from bottom to top.
        //=> Add the Theme Manager button.
        var showManagerButton = UIUtils.CreateButton(pagePanel);
        showManagerButton.name = "ShowManagerButton";
        showManagerButton.text = "Open Theme Manager";
        showManagerButton.width = pagePanel.width - spacing;
        showManagerButton.relativePosition = new Vector2(0, pagePanel.height - showManagerButton.height - spacing);

        //=> Add a spacer
        var spacer2 = AddSpacer(pagePanel);
        spacer2.relativePosition = new Vector2(0, showManagerButton.relativePosition.y - spacer2.height - spacing);

        //=> Insert between top- and bottom-docked elements.
        //=> Add a scrollable panel -- the other tabs have one as their main tab page view, we don't (got a UIPanel).
        //   Anyway our page in not structured the same, we have static elements, and the scroll panel in the middle.
        //   We could attach the scrollbar from the parent as this scrollbar is shared between all tabs, but 1) it
        //   appears to be buggy (stack overflow, no mouse wheel...) and 2) it's not really prettier...
        var themesPanel = pagePanel.AddUIComponent<AdvancedScrollablePanel>();
        themesPanel.name = "ThemesPanel";
        themesPanel.width = pagePanel.width;
        themesPanel.relativePosition = new Vector2(0, spacer1.relativePosition.y + spacer1.height);
        themesPanel.height = spacer2.relativePosition.y - (spacer1.relativePosition.y + spacer1.height);

        themesPanel.Content.autoLayout = true;
        themesPanel.Content.autoLayoutDirection = LayoutDirection.Vertical;
        if (scrollbar is not null) {
            themesPanel.Content.verticalScrollbar.width = scrollbar.width;
        }

        // @todo Temporary code for demoing scroll, remove later.
        for (var index = 0; index < 50; index++) {
            var label = themesPanel.Content.AddUIComponent<UILabel>();
            label.text = index.ToString();
        }

        UIPanel AddSpacer(UIComponent parent) {
            var spacer = parent.AddUIComponent<UIPanel>();

            spacer.width = parent.width - spacing;
            spacer.height = 8;
            spacer.backgroundSprite = "ContentManagerItemBackground";

            return spacer;
        }
    }

    [HarmonyPostfix, UsedImplicitly]
    [HarmonyPatch(nameof(PoliciesPanel.Set), typeof(byte))]
    public static void PostfixSet(byte district) {
        if (PoliciesPanelPatches.currentThemeManagementCheckBox is not null) {
            PoliciesPanelPatches.currentThemeManagementCheckBox.text = district == 0
                ? "Enable Theme Management for this city"
                : "Enable Theme Management for this district";
        }
    }

    private static void OnTabStripSelectedIndexChanged(UIComponent component, int tabIndex) {
        if (PoliciesPanelPatches.currentInstance is null) {
            return;
        }

        var tabStrip = PoliciesPanelPatches.currentInstance.FindTabstrip();
        var scrollbar = PoliciesPanelPatches.currentInstance.FindScrollbar();
        var policyContainer = PoliciesPanelPatches.currentInstance.FindPolicyContainer();

        if (tabStrip is null || scrollbar is null || policyContainer is null) {
            PoliciesPanelPatches.Logger.Error("Unknown layout state, not updating Policies panel tabs.");
            return;
        }

        var isThemesTab = tabIndex == tabStrip.tabPages.childCount - 1;

        scrollbar.enabled = !isThemesTab;

        if (isThemesTab) {
            policyContainer.width = PoliciesPanelPatches.originalPolicyContainerWidth + scrollbar.width;
        }
        else {
            policyContainer.width = PoliciesPanelPatches.originalPolicyContainerWidth;
        }
    }
}
