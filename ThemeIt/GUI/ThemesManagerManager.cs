using ColossalFramework.UI;
using UnityEngine;

namespace ThemeIt.GUI;

/**
 * Manages lifecycle of the only UIThemesManagerPanel instance that should exist.
 * It must be used to create/show/hide the Themes Manager panel.
 */
internal sealed class ThemesManagerManager {
    private GameObject? currentPanelGameObject;

    private UIThemesManagerPanel? currentPanel;

    private readonly ThemeItMod mod;

    internal ThemesManagerManager(ThemeItMod mod) {
        this.mod = mod;
    }

    /**
     * Opens the Themes Manager.
     * The instance returned will be valid as long as the panel is open, so it must not be kept.
     */
    internal UIThemesManagerPanel Open() {
        if (this.currentPanel is null) {
            this.currentPanelGameObject = new GameObject("ThemesManager");
            this.currentPanelGameObject.transform.parent = UIView.GetAView().transform;

            this.currentPanel = this.currentPanelGameObject.AddComponent<UIThemesManagerPanel>();

            this.currentPanel.Mod = this.mod;
            this.currentPanel.ShouldClose += this.Close;

            this.currentPanel.CenterOnScreen();
        }

        return this.currentPanel;
    }

    /**
     * Closes the Themes Manager. Destroys associated components since this puts pressure on the renderer, even if the
     * panel is not visible.
     */
    internal void Close() {
        if (this.currentPanel is null) {
            return;
        }

        Object.Destroy(this.currentPanel);
        Object.Destroy(this.currentPanelGameObject);

        this.currentPanel = null;
        this.currentPanelGameObject = null;
    }
}
