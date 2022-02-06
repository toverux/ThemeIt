using System;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using CitiesHarmony.API;
using ICities;
using ModsCommon;
using ModsCommon.UI;
using ThemeIt.GUI;
using ThemeIt.Properties;

namespace ThemeIt;

// ReSharper disable once UnusedType.Global
public sealed class ThemeItMod : BaseMod<ThemeItMod> {
    public override string NameRaw => "Theme It";

    public override List<Version> Versions => new() {
        new Version(0, 0, 1)
    };

    protected override string IdRaw => "ThemeIt";

    public override bool IsBeta => true;

    public override string Description => Localize.ThemeItMod_Description;

    protected override ulong StableWorkshopId => 0;

    protected override ulong BetaWorkshopId => 0;

    protected override ResourceManager LocalizeManager => Localize.ResourceManager;

    private Patcher Patcher { get; }

    public ThemeItMod() {
        this.Patcher = new Patcher(this.IdRaw, this.Logger);

        Locator.Current.Register(this);
        Locator.Current.Register(new ThemesManagerManager());
    }

    protected override void SetCulture(CultureInfo culture) => Localize.Culture = culture;

    protected override void GetSettings(UIHelperBase helper) => new Settings().OnSettingsUI(helper);

    protected override void Enable() {
        HarmonyHelper.DoOnHarmonyReady(() => {
            try {
                this.Patcher.PatchAll();
            }
            catch {
                var message = MessageBox.Show<ErrorPatchMessageBox>();
                message.Init<ThemeItMod>();

                throw;
            }
        });
    }

    protected override void Disable() {
        if (HarmonyHelper.IsHarmonyInstalled) {
            this.Patcher.UnpatchAll();
        }
    }
}
