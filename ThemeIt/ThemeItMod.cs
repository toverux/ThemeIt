using System;
using System.Collections.Generic;
using CitiesHarmony.API;
using ModsCommon;
using ModsCommon.UI;

namespace ThemeIt;

// ReSharper disable once UnusedType.Global
public sealed class ThemeItMod : BaseMod<ThemeItMod> {
    public override string NameRaw => "Theme It";

    public override List<Version> Versions => new() {
        new Version(0, 0, 1)
    };

    protected override string IdRaw => "ThemeIt";

    public override bool IsBeta => true;

    public override string Description => "Create themes for growables and apply them to cities and districts.";

    protected override ulong StableWorkshopId => 0;

    protected override ulong BetaWorkshopId => 0;

    private Patcher Patcher { get; }

    public ThemeItMod() {
        this.Patcher = new Patcher(this.IdRaw, this.Logger);
    }

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
