using ColossalFramework.UI;
using ThemeIt.ThirdParty;
using UnityEngine;

namespace ThemeIt.GUI;

internal sealed class UIThemesManagerBuildingsListPanel : UIPanel {
    private readonly UIFastList fastList;

    internal UIThemesManagerBuildingsListPanel() {
        this.fastList = UIFastList.Create<UIThemesManagerBuildingRow>(this);

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
