using ColossalFramework.UI;
using ModsCommon.UI;
using ThemeIt.Properties;
using UnityEngine;
using ILogger = ModsCommon.ILogger;

namespace ThemeIt.GUI;

/**
 * Responsible of the "Themes" tab on the Policies panel from the game.
 */
internal sealed class ThemesTabManager {
    private readonly ILogger logger;

    private readonly PoliciesPanel policiesPanel;

    private readonly UICheckBox? themeManagementCheckBox;

    private readonly float originalPolicyContainerWidth;

    private UITabstrip? FindTabstrip() => this.policiesPanel.Find<UITabstrip>("Tabstrip");

    private UIScrollbar? FindScrollbar() => this.policiesPanel.Find<UIScrollbar>("Scrollbar");

    private UITabContainer? FindPolicyContainer() => this.policiesPanel.Find<UITabContainer>("PolicyContainer");

    /**
     * Installs the mod tab on the panel from the game.
     */
    internal ThemesTabManager(ILogger logger, PoliciesPanel policiesPanel) {
        this.logger = logger;
        this.policiesPanel = policiesPanel;

        var tabstrip = this.FindTabstrip();
        var scrollbar = this.FindScrollbar();
        var policyContainer = this.FindPolicyContainer();

        if (policyContainer is null || tabstrip is null) {
            this.logger.Error("Cannot find PolicyContainer or Tabstrip, not updating Policies tabs.");
            return;
        }

        if (scrollbar is null) {
            this.logger.Error("Cannot find PoliciesPanel scrollbar, Themes tab may be broken.");
        }

        this.originalPolicyContainerWidth = policyContainer.width;

        //=> A margin to apply on the container block and between elements, based on what the game uses in that panel.
        var spacing = policyContainer.relativePosition.x;

        //=> Add a custom tab.
        var tab = tabstrip.AddTab(Localize.GUI_ThemesTabManager_TabTitle);
        tab.name = "ThemesStrip";

        //=> Set some properties to mimic these particular tab instance in the policies panel.
        tab.textScale = ((UIButton) tabstrip.tabs[0]).textScale;
        tab.playAudioEvents = true;

        //=> PoliciesPanel.SetParentButton expects all tabs to have a TutorialUITag, we add an empty one.
        tab.gameObject.AddComponent<TutorialUITag>();

        //=> Listen for tab activation events, to change/restore the container width based on the tab kind.
        //   It should not be necessary to remove the listener on this instance (destroyed together).
        tabstrip.eventSelectedIndexChanged += this.OnTabStripSelectedIndexChanged;

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
        this.themeManagementCheckBox = UIUtils.CreateCheckBox(pagePanel);
        this.themeManagementCheckBox.relativePosition = new Vector2(0, spacing);
        this.themeManagementCheckBox.name = "ThemeManagementCheckbox";
        this.themeManagementCheckBox.width = pagePanel.width - spacing;

        //=> Add a spacer
        var spacer1 = AddSpacer(pagePanel);
        spacer1.relativePosition = new Vector2(
            0,
            this.themeManagementCheckBox.relativePosition.y
            + this.themeManagementCheckBox.size.y
            + spacing);

        //=> Layout from bottom to top.
        //=> Add the Theme Manager button.
        var showManagerButton = UIUtils.CreateButton(pagePanel);
        showManagerButton.name = "ShowManagerButton";
        showManagerButton.text = Localize.GUI_ThemesTabManager_OpenThemeManager;
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

    /**
     * Updates the UI state for the currently selected district. District 0 is the city.
     */
    internal void SetCurrentDistrict(byte districtId) {
        if (this.themeManagementCheckBox is not null) {
            this.themeManagementCheckBox.text = districtId == 0
                ? Localize.GUI_ThemesTabManager_EnableThemeManagementForCity
                : Localize.GUI_ThemesTabManager_EnableThemeManagementForDistrict;
        }
    }

    /**
     * Tab-switching handler for patching the tabs container based on the currently-shown tab.
     * Our "Themes" tab does not use the scrollbar from the Policies panel, so we have to hide it then change the size
     * of the container when the Themes tab is displayed.
     */
    private void OnTabStripSelectedIndexChanged(UIComponent component, int tabIndex) {
        var tabStrip = this.FindTabstrip();
        var scrollbar = this.FindScrollbar();
        var policyContainer = this.FindPolicyContainer();

        if (tabStrip is null || scrollbar is null || policyContainer is null) {
            this.logger.Error("Unknown layout state, not updating Policies panel tabs.");
            return;
        }

        var isThemesTab = tabIndex == tabStrip.tabPages.childCount - 1;

        scrollbar.enabled = !isThemesTab;

        if (isThemesTab) {
            policyContainer.width = this.originalPolicyContainerWidth + scrollbar.width;
        }
        else {
            policyContainer.width = this.originalPolicyContainerWidth;
        }
    }
}
