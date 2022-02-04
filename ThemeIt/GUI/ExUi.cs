using System;
using ColossalFramework.UI;
using UnityEngine;

// Disable a few warnings about unused symbols: it's a library that exposes stuff, used or not.
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ThemeIt.GUI;

/**
 * "ExUi" (Expressive/Extensions UI) is a helper class for creating UI components, with:
 *  - A coherent style, generally close to the base game's one;
 *  - An intuitive extensions-based API;
 *  - Less code;
 *  - More expressive code/easier to glance at.
 *
 * The API is designed to ease and streamline the heavy-lifting, not to replace standard game's UI library.
 * As such, only tooling and options for the most common use-cases are provided. The rest can still be done through the
 * game's UI library API.
 *
 * ExUI is based on previous work from:
 *  - @algernon-A: https://github.com/algernon-A/Realistic-Population-Revisited/blob/master/Code/Utils/UIControls.cs
 *  - @SamsamTS/@sway2020: https://github.com/sway2020/FindIt2/blob/master/FindIt/GUI/UIUtils.cs
 * Many thanks to them!
 */
internal static class ExUi {
    internal static Vector3 GetPositionUnder(this UIComponent componentAbove, float spacing = 0, float offsetX = 0) {
        return new Vector3(offsetX, componentAbove.relativePosition.y + componentAbove.size.y + spacing);
    }

    internal static Vector3 GetPositionAboveFor(
        this UIComponent componentUnder, UIComponent componentAbove, float spacing = 0, float offsetX = 0) {

        return new Vector3(offsetX, componentUnder.relativePosition.y - componentAbove.height - spacing);
    }

    internal static TComponent AddUIComponent<TComponent>(
        this UIComponent parent, UIComponentOptions? options = null) where TComponent : UIComponent {

        options ??= new UIComponentOptions();

        var component = parent.AddUIComponent<TComponent>();

        component.ApplyGenericOptions(parent, options);

        return component;
    }

    internal static UICheckBox AddUICheckBox(this UIComponent parent, CheckBoxOptions? options = null) {
        options ??= new CheckBoxOptions();

        var checkBox = parent.AddUIComponent<UICheckBox>();

        checkBox.ApplyGenericOptions(parent, options);

        checkBox.height = options.SpriteSize;

        var sprite = checkBox.AddUIComponent<UISprite>();
        sprite.spriteName = "ToggleBase";
        sprite.size = new Vector2(options.SpriteSize, options.SpriteSize);
        sprite.relativePosition = Vector3.zero;

        var checkedSprite = sprite.AddUIComponent<UISprite>();
        checkedSprite.spriteName = "ToggleBaseFocused";
        checkedSprite.size = new Vector2(options.SpriteSize, options.SpriteSize);
        checkedSprite.relativePosition = Vector3.zero;

        checkBox.checkedBoxObject = checkedSprite;

        checkBox.label = checkBox.AddUIComponent<UILabel>();
        checkBox.label.text = options.LabelText;
        checkBox.label.relativePosition = new Vector3(options.SpriteSize + options.LabelOffset.x, options.LabelOffset.y);

        return checkBox;
    }

    public static UIButton AddUIButton(this UIComponent parent, ButtonOptions? options = null) {
        options ??= new ButtonOptions();

        var button = parent.AddUIComponent<UIButton>();

        button.ApplyGenericOptions(parent, options);

        button.normalBgSprite = "ButtonMenu";
        button.hoveredBgSprite = "ButtonMenuHovered";
        button.pressedBgSprite = "ButtonMenuPressed";
        button.disabledBgSprite = "ButtonMenuDisabled";
        button.disabledTextColor = new Color32(128, 128, 128, 255);
        button.canFocus = false;

        button.textVerticalAlignment = UIVerticalAlignment.Middle;
        button.textHorizontalAlignment = UIHorizontalAlignment.Center;
        button.text = options.Text;

        return button;
    }

    public static UIPanel AddUIHorizontalRule(this UIComponent parent, HorizontalRuleOptions? options = null) {
        options ??= new HorizontalRuleOptions();

        var rule = parent.AddUIComponent<UIHorizontalRule>();

        rule.ApplyGenericOptions(parent, options);

        rule.backgroundSprite = "ContentManagerItemBackground";

        return rule;
    }

    private static void ApplyGenericOptions(
        this UIComponent component, UIComponent parent, UIComponentOptions options) {

        if (options.Name is not null) {
            component.name = options.Name;
        }

        if (options.Width is not null) {
            component.width = options.Width(parent);
        }

        if (options.Height is not null) {
            component.height = options.Height(parent);
        }

        if (options.RelativePosition is not null) {
            component.relativePosition = options.RelativePosition(parent, component);
        }
    }

    internal class UIComponentOptions {
        internal string? Name { get; set; }

        internal virtual Func<UIComponent, float>? Width { get; set; }

        internal virtual Func<UIComponent, float>? Height { get; set; }

        internal Func<UIComponent, UIComponent, Vector3>? RelativePosition { get; set; }
    }

    internal class CheckBoxOptions : UIComponentOptions {
        internal float SpriteSize { get; set; } = 16;

        internal Vector2 LabelOffset { get; set; } = new(6, 0);

        internal string? LabelText { get; set; }
    }

    internal class ButtonOptions : UIComponentOptions {
        internal string? Text { get; set; }

        internal override Func<UIComponent, float>? Height { get; set; } = _ => 30;
    }

    internal class HorizontalRuleOptions : UIComponentOptions {
        internal override Func<UIComponent, float>? Width { get; set; } = parent => parent.width;

        internal override Func<UIComponent, float>? Height { get; set; } = _ => 5;
    }

    internal class UIHorizontalRule : UIPanel {
    }
}
