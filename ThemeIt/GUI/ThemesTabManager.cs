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

    private readonly ThemesManagerManager themesManagerManager;

    private PoliciesPanel? policiesPanel;

    private int tabIndex;

    private UICheckBox? themeManagementCheckBox;

    private float originalPolicyContainerWidth;

    private UITabstrip? FindTabstrip() => this.policiesPanel?.Find<UITabstrip>("Tabstrip");

    private UIScrollbar? FindScrollbar() => this.policiesPanel?.Find<UIScrollbar>("Scrollbar");

    private UITabContainer? FindPolicyContainer() => this.policiesPanel?.Find<UITabContainer>("PolicyContainer");

    internal ThemesTabManager(ILogger logger, ThemesManagerManager themesManagerManager) {
        this.logger = logger;
        this.themesManagerManager = themesManagerManager;
    }

    /**
     * Installs the Themes tab on the panel from the game.
     */
    internal void Install(PoliciesPanel panel) {
        this.policiesPanel = panel;

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
        this.tabIndex = tabstrip.tabPages.childCount - 1;

        var tabPanel = (UIPanel) tabstrip.tabPages.components[this.tabIndex];
        tabPanel.name = "Themes";

        //=> Only make the tab visible if our tab was selected when the panel was closed last time.
        tabPanel.isVisible = tabstrip.selectedIndex == this.tabIndex;

        //=> Eat the space of the unused and disabled scrollbar that only the other tabs use.
        //   We need to change the size of the container so the child can be expanded, otherwise the width is clamped.
        //   But when switching tabs, the container's width will be changed/restored, according to the tab kind.
        if (scrollbar is not null) {
            policyContainer.width += scrollbar.width;
            tabPanel.width += scrollbar.width;
        }

        //=> Layout from top to bottom.
        //=> Add checkbox for theme management activation on city/district.
        this.themeManagementCheckBox = tabPanel.AddUICheckBox(new ExUi.CheckBoxOptions {
            Name = "ThemeManagementCheckbox",
            Width = parent => parent.width - spacing,
            RelativePosition = (_, _) => new Vector2(0, spacing * 2)
        });

        //=> Add a spacer
        var spacer1 = tabPanel.AddUIHorizontalRule(new ExUi.HorizontalRuleOptions {
            Width = parent => parent.width - spacing,
            RelativePosition = (_, _) => this.themeManagementCheckBox.GetPositionUnder(spacing * 2)
        });

        //=> Layout from bottom to top.
        //=> Add the Theme Manager button.
        var showManagerButton = tabPanel.AddUIButton(new ExUi.ButtonOptions {
            Name = "ShowManagerButton",
            Text = Localize.GUI_ThemesTabManager_OpenThemeManager,
            Width = parent => parent.width - spacing,
            Height = _ => 45,
            RelativePosition = (parent, self) => new Vector2(0, parent.height - self.height - spacing)
        });

        showManagerButton.eventClicked += (_, _) => this.themesManagerManager.Open();

        //=> Add a spacer
        var spacer2 = tabPanel.AddUIHorizontalRule(new ExUi.HorizontalRuleOptions {
            Width = parent => parent.width - spacing,
            RelativePosition = (_, self) => showManagerButton.GetPositionAboveFor(self, spacing)
        });

        //=> Insert between top- and bottom-docked elements.
        //=> Add a scrollable panel -- the other tabs have one as their main tab page view, we don't (got a UIPanel).
        //   Anyway our page in not structured the same, we have static elements, and the scroll panel in the middle.
        //   We could attach the scrollbar from the parent as this scrollbar is shared between all tabs, but 1) it
        //   appears to be buggy (stack overflow, no mouse wheel...) and 2) it's not really prettier...
        var themesPanel = tabPanel.AddUIComponent<AdvancedScrollablePanel>(new ExUi.UIComponentOptions {
            Name = "ThemesPanel",
            Width = parent => parent.width,
            Height = _ => spacer2.relativePosition.y - (spacer1.relativePosition.y + spacer1.height),
            RelativePosition = (_, _) => spacer1.GetPositionUnder()
        });

        themesPanel.Content.autoLayout = true;
        themesPanel.Content.autoLayoutDirection = LayoutDirection.Vertical;

        //=> Mimic original scrollbar of policies panel
        if (scrollbar is not null) {
            themesPanel.Content.verticalScrollbar.width = scrollbar.width;
        }

        // @todo Temporary code for demoing scroll, remove later.
        for (var index = 0; index < 50; index++) {
            var label = themesPanel.Content.AddUIComponent<UILabel>();
            label.text = index.ToString();
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
     * Focuses default policies panel tab.
     */
    internal void FocusDefaultTab() => this.FocusTabByIndex(0);

    /**
     * Focuses our custom policies panel tab.
     */
    internal void FocusThemesTab()  => this.FocusTabByIndex(this.tabIndex);

    /**
     * Focuses a policies panel tab found by index.
     */
    private void FocusTabByIndex(int index) {
        var tabstrip = this.FindTabstrip();
        if (tabstrip is null) {
            this.logger.Error($"Cannot focus policies tab #{index}, tab controller was not found.");
            return;
        }

        tabstrip.selectedIndex = index;
    }

    /**
     * Tab-switching handler for patching the tabs container based on the currently-shown tab.
     * Our "Themes" tab does not use the scrollbar from the Policies panel, so we have to hide it then change the size
     * of the container when the Themes tab is displayed.
     */
    private void OnTabStripSelectedIndexChanged(UIComponent component, int selectedTabIndex) {
        var tabStrip = this.FindTabstrip();
        var scrollbar = this.FindScrollbar();
        var policyContainer = this.FindPolicyContainer();

        if (tabStrip is null || scrollbar is null || policyContainer is null) {
            this.logger.Error("Unknown layout state, not updating Policies panel tabs.");
            return;
        }

        var isThemesTab = selectedTabIndex == tabStrip.tabPages.childCount - 1;

        scrollbar.enabled = !isThemesTab;

        if (isThemesTab) {
            policyContainer.width = this.originalPolicyContainerWidth + scrollbar.width;
        }
        else {
            policyContainer.width = this.originalPolicyContainerWidth;
        }
    }
}
