using System;
using ColossalFramework.UI;
using UnityEngine;

namespace ThemeIt.GUI.ThemesManager;

/**
 * Top title bar of the Themes Manager modal.
 */
internal sealed class UITitlePanel : UIPanel {
    internal ThemeItMod Mod {
        set => this.modLabel.text = value.Name;
    }

    internal UIComponent DragHandleTarget {
        get => this.dragHandle.target;
        set => this.dragHandle.target = value;
    }

    internal event Action? ShouldClose;

    private readonly UISprite iconSprite;

    private readonly UIButton closeButton;

    private readonly UIDragHandle dragHandle;

    private readonly UILabel titleLabel;

    private readonly UILabel modLabel;

    internal UITitlePanel() {
        //=> Icon.
        this.iconSprite = this.AddUIComponent<UISprite>();
        this.iconSprite.spriteName = "ToolbarIconZoomOutCity";
        this.iconSprite.ResizeIcon(new Vector2(30, 30));

        //=> Close button.
        this.closeButton = this.AddUIComponent<UIButton>();
        this.closeButton.normalBgSprite = "buttonclose";
        this.closeButton.hoveredBgSprite = "buttonclosehover";
        this.closeButton.pressedBgSprite = "buttonclosepressed";

        this.closeButton.eventClick += (_, _) => this.ShouldClose?.Invoke();

        //=> Title labels
        this.titleLabel = this.AddUIComponent<UILabel>();
        this.titleLabel.text = Properties.Localize.GUI_UIThemesManagerTitlePanel_Title;

        this.modLabel = this.AddUIComponent<UILabel>();
        this.modLabel.opacity = .3f;

        //=> Make the window draggable.
        this.dragHandle = this.AddUIComponent<UIDragHandle>();
    }

    protected override void OnSizeChanged() {
        const int spacing = 8;

        this.iconSprite.relativePosition = new Vector3(spacing, (this.height - this.iconSprite.height) / 2);

        this.closeButton.relativePosition = new Vector3(
            this.width - this.closeButton.width - spacing / 2f,
            (this.height - this.closeButton.height) / 2);

        this.titleLabel.relativePosition = this.iconSprite.GetPositionAfter(
            spacing, (this.iconSprite.height - this.titleLabel.height) / 2);

        this.modLabel.relativePosition = this.titleLabel.GetPositionAfter(spacing);

        this.dragHandle.width = this.width - this.closeButton.width - spacing;
        this.dragHandle.height = this.height;
        this.dragHandle.relativePosition = Vector3.zero;

        base.OnSizeChanged();
    }
}
