using System;
using ColossalFramework.UI;
using UnityEngine;

namespace ThemeIt.GUI.ThemesManager;

/**
 * Main Themes Manager panel, opening as a modal.
 */
internal sealed class UIModalPanel : UIPanel {
    internal ThemeItMod Mod {
        set => this.titlePanel.Mod = value;
    }

    internal event Action? ShouldClose;

    private const float WidthFactorToContainer = .5f;

    private const float HeightFactorToContainer = .6f;

    private readonly UITitlePanel titlePanel;

    private readonly UIBuildingsListPanel buildingsListPanel;

    internal UIModalPanel() {
        //=> Self properties.
        this.backgroundSprite = "UnlockingPanel2";

        //=> Title bar panel at the top.
        this.titlePanel = this.AddUIComponent<UITitlePanel>();
        this.titlePanel.DragHandleTarget = this;

        this.titlePanel.ShouldClose += () => this.ShouldClose?.Invoke();

        //=> Scrollable list of buildings
        this.buildingsListPanel = this.AddUIComponent<UIBuildingsListPanel>();
    }

    internal void CenterOnScreen() {
        const int spacing = 8;

        var host = this.GetUIView();

        this.size = new Vector2(
            Mathf.Floor(host.fixedWidth * UIModalPanel.WidthFactorToContainer),
            Mathf.Floor(host.fixedHeight * UIModalPanel.HeightFactorToContainer));

        this.relativePosition = new Vector3(
            Mathf.Floor((host.fixedWidth - this.width) / 2),
            Mathf.Floor((host.fixedHeight - this.height) / 2));

        //=> Resize title bar.
        this.titlePanel.size = new Vector2(this.width, 40);
        this.titlePanel.relativePosition = Vector3.zero;

        //=> Resize buildings list.
        this.buildingsListPanel.size = new Vector2(
            this.width - spacing * 2,
            this.height - this.titlePanel.height - spacing);

        this.buildingsListPanel.relativePosition = this.titlePanel.GetPositionUnder(offsetX: spacing);
    }
}
