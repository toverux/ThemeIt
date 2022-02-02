using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;

namespace ThemeIt.Patches;

[HarmonyPatch(typeof(ZoneBlock))]
[UsedImplicitly]
internal static class ZoneBlockPatches {
    [HarmonyTranspiler, UsedImplicitly]
    [HarmonyPatch(nameof(ZoneBlock.SimulationStep), typeof(ushort))]
    internal static IEnumerable<CodeInstruction> TranspileSimulationStep(
        IEnumerable<CodeInstruction> instructions) =>
        RandomBuildingInfoPatcher.TranspileGetRandomBuildingInfoConsumer(instructions);
}
