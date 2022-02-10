using System;
using ColossalFramework.UI;
using UnityEngine;

namespace ThemeIt.GUI;

internal sealed class UIThemesManagerPanel : UIPanel {
    internal ThemeItMod Mod {
        set => this.titlePanel.Mod = value;
    }

    internal event Action? ShouldClose;

    private const float WidthFactorToContainer = .5f;

    private const float HeightFactorToContainer = .6f;

    private readonly UIThemesManagerTitlePanel titlePanel;

    internal UIThemesManagerPanel() {
        this.backgroundSprite = "UnlockingPanel2";

        this.titlePanel = this.AddUIComponent<UIThemesManagerTitlePanel>();
        this.titlePanel.DragHandleTarget = this;

        this.titlePanel.ShouldClose += () => this.ShouldClose?.Invoke();
    }

    internal void CenterOnScreen() {
        var host = this.GetUIView();

        this.size = new Vector2(
            Mathf.Floor(host.fixedWidth * UIThemesManagerPanel.WidthFactorToContainer),
            Mathf.Floor(host.fixedHeight * UIThemesManagerPanel.HeightFactorToContainer));

        this.relativePosition = new Vector3(
            Mathf.Floor((host.fixedWidth - this.width) / 2),
            Mathf.Floor((host.fixedHeight - this.height) / 2));

        this.titlePanel.size = new Vector2(this.width, 40);
        this.titlePanel.relativePosition = Vector3.zero;
    }
}
