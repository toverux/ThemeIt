using System.Reflection;
using HarmonyLib;
using ModsCommon;

namespace ThemeIt;

internal sealed class Patcher {
    private ILogger Logger { get; }

    private Harmony Harmony { get; }

    private bool patchesWereApplied;

    static Patcher() {
        #if DEBUG
        // This will create harmony.log.txt on the Desktop, with generated IL debug output.
        Harmony.DEBUG = true;
        #endif
    }

    internal Patcher(string id, ILogger logger) {
        this.Logger = logger;
        this.Harmony = new Harmony(id);
    }

    internal void PatchAll() {
        if (this.patchesWereApplied) {
            return;
        }

        this.Harmony.PatchAll(Assembly.GetExecutingAssembly());

        this.patchesWereApplied = true;

        this.Logger.Debug("All Harmony patches were applied.");
    }

    internal void UnpatchAll() {
        if (!this.patchesWereApplied) {
            return;
        }

        this.Harmony.UnpatchAll();

        this.patchesWereApplied = false;

        this.Logger.Debug("All Harmony patches were removed.");
    }
}
