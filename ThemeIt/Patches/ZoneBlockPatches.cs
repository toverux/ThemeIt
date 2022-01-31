using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;

namespace ThemeIt.Patches;

[HarmonyPatch(typeof(ZoneBlock))]
[UsedImplicitly]
public static class ZoneBlockPatches {
    [HarmonyTranspiler, UsedImplicitly]
    [HarmonyPatch(nameof(ZoneBlock.SimulationStep), typeof(ushort))]
    public static IEnumerable<CodeInstruction> TranspileSimulationStep(
        IEnumerable<CodeInstruction> instructions) =>
        RandomBuildingInfoPatcher.TranspileGetRandomBuildingInfoConsumer(instructions);
}
