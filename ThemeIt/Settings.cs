using ModsCommon;

namespace ThemeIt;

/**
 * Responsible of constructing the mod settings UI and load/save the mod's XML save settings.
 */
internal sealed class Settings : BaseSettings<ThemeItMod> {
    protected override void FillSettings() {
        base.FillSettings();

        this.AddLanguage(this.GeneralTab);
        this.AddNotifications(this.GeneralTab);
    }
}
