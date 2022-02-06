using ColossalFramework.UI;
using UnityEngine;

namespace ThemeIt.GUI;

internal class UIThemesManagerPanel : UIPanel {
    private const float WidthFactorToContainer = .5f;

    private const float HeightFactorToContainer = .6f;

    internal UIThemesManagerPanel() {
        var host = this.GetUIView();

        this.size = new Vector2(
            Mathf.Floor(host.fixedWidth * UIThemesManagerPanel.WidthFactorToContainer),
            Mathf.Floor(host.fixedHeight * UIThemesManagerPanel.HeightFactorToContainer));

        this.relativePosition = new Vector3(
            Mathf.Floor((host.fixedWidth - this.width) / 2),
            Mathf.Floor((host.fixedHeight - this.height) / 2));

        this.backgroundSprite = "UnlockingPanel2";
    }
}
