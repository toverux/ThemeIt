using System;
using System.Collections.Generic;
using ModsCommon;
using ThemeIt.Patches;

namespace ThemeIt {
    // ReSharper disable once UnusedType.Global
    public class ThemeItMod : BasePatcherMod<ThemeItMod> {
        public override string NameRaw => "Theme It";

        public override List<Version> Versions => new() {
            new Version(1, 0, 0)
        };

        protected override string IdRaw => "ThemeIt";

        public override bool IsBeta => false;

        public override string Description => "Create themes for growables and apply them to cities and districts.";

        protected override ulong StableWorkshopId => 0;

        protected override ulong BetaWorkshopId => 0;

        protected override bool PatchProcess() {
            var success = true;

            success &= this.AddPostfix(
                typeof(PoliciesPanelPatches), nameof(PoliciesPanelPatches.PostfixAwake),
                typeof(PoliciesPanel), "Awake");

            success &= this.AddTranspiler(
                typeof(PoliciesPanelPatches), nameof(PoliciesPanelPatches.TranspileRefreshPanel),
                typeof(PoliciesPanel), "RefreshPanel");

            success &= this.AddPostfix(
                typeof(PoliciesPanelPatches), nameof(PoliciesPanelPatches.PostfixSet),
                typeof(PoliciesPanel), nameof(PoliciesPanel.Set));

            success &= this.AddTranspiler(
                typeof(RandomBuildingInfoPatches), nameof(RandomBuildingInfoPatches.TranspileZoneBlockSimulationStep),
                typeof(ZoneBlock), nameof(ZoneBlock.SimulationStep),
                new[] { typeof(ushort) });

            success &= this.AddTranspiler(
                typeof(RandomBuildingInfoPatches),
                nameof(RandomBuildingInfoPatches.TranspilePrivateBuildingAiGetUpgradeInfo),
                typeof(PrivateBuildingAI), nameof(PrivateBuildingAI.GetUpgradeInfo),
                new[] { typeof(ushort), typeof(Building).MakeByRefType() });

            success &= this.AddTranspiler(
                typeof(RandomBuildingInfoPatches),
                nameof(RandomBuildingInfoPatches.TranspilePrivateBuildingAiSimulationStep),
                typeof(PrivateBuildingAI), nameof(PrivateBuildingAI.SimulationStep),
                new[] { typeof(ushort), typeof(Building).MakeByRefType(), typeof(Building.Frame).MakeByRefType() });

            return success;
        }
    }
}
