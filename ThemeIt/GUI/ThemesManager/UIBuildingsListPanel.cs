using ColossalFramework.UI;
using ThemeIt.ThirdParty;
using UnityEngine;

namespace ThemeIt.GUI.ThemesManager;

/**
 * Panel hosting the list of buildings in a theme, and the buttons for creating/deleting themes.
 */
internal sealed class UIBuildingsListPanel : UIPanel {
    private readonly UIFastList fastList;

    internal UIBuildingsListPanel() {
        this.fastList = UIFastList.Create<UIBuildingRowPanel>(this);

        this.fastList.BackgroundSprite = "UnlockingPanel";
        this.fastList.CanSelect = true;
        this.fastList.RowHeight = 40;
        this.fastList.AutoHideScrollbar = true;
        this.fastList.RowsData = new FastList<object>();
    }

    protected override void OnSizeChanged() {
        this.fastList.width = this.width;
        this.fastList.height = this.height;
        this.fastList.relativePosition = Vector3.zero;

        base.OnSizeChanged();
    }
}
