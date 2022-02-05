﻿using ColossalFramework.UI;
using ThemeIt.Properties;
using UnityEngine;
using ILogger = ModsCommon.ILogger;

namespace ThemeIt.GUI;

internal sealed class DistrictInfoPanelManager {
    private readonly ILogger logger;

    private readonly DistrictWorldInfoPanel districtWorldInfoPanel;

    private UIButton? FindPoliciesButton() => this.districtWorldInfoPanel.Find<UIButton>("PoliciesButton");

    private UIDropDown? FindStyleDropdown() => this.districtWorldInfoPanel.Find<UIDropDown>("StyleDropdown");

    internal DistrictInfoPanelManager(ILogger logger, DistrictWorldInfoPanel districtWorldInfoPanel) {
        this.logger = logger;
        this.districtWorldInfoPanel = districtWorldInfoPanel;
    }

    /**
     * Installs the "Themes" button on the floating district info panel.
     * Also, it re-creates the "Policies" button from the base game, so both will have a coherent style (the button's
     * style from the base game is a bit bizarre, so I chose to apply our own style).
     */
    internal void Install() {
        var originalPoliciesButton = this.FindPoliciesButton();
        var styleDropdown = this.FindStyleDropdown();

        if (originalPoliciesButton is null || styleDropdown is null) {
            this.logger.Error("Cannot find StyleDropdown or PoliciesButton, not updating district info panel.");
            return;
        }

        //=> Hide original policies button and district style dropdown.
        originalPoliciesButton.Hide();
        styleDropdown.Hide();

        var container = originalPoliciesButton.parent;
        var spacing = originalPoliciesButton.relativePosition.x;

        //=> Recreate a "Policies" button
        var policiesButton = container.AddUIButton(new ExUi.ButtonOptions {
            Name = "PoliciesButton",
            Text = originalPoliciesButton.text.ToUpper(),
            Width = _ => originalPoliciesButton.width,
            RelativePosition = (_, _) => new Vector3(spacing, 0)
        });

        //=> Open policies panel on click, but make sure it's not on the Themes tab (user explicitly clicked on
        //   "Policies" and not "Themes").
        policiesButton.eventClicked += (_, _) => {
            this.districtWorldInfoPanel.OnPoliciesClick();
            Locator.Current.Find<ThemesTabManager>().FocusDefaultTab();
        };

        //=> Create a "Themes" button.
        var themesButton = container.AddUIButton(new ExUi.ButtonOptions {
            Name = "ThemesButton",
            Text = Localize.GUI_DistrictInfoPanelManager_Themes.ToUpper(),
            Width = _ => originalPoliciesButton.width,
            RelativePosition = (_, _) => policiesButton.GetPositionAfter(spacing)
        });

        //=> Open policies panel on click, and select Themes tab.
        themesButton.eventClicked += (_, _) => {
            this.districtWorldInfoPanel.OnPoliciesClick();
            Locator.Current.Find<ThemesTabManager>().FocusThemesTab();
        };
    }
}
