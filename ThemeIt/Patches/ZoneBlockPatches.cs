using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;

namespace ThemeIt.Patches; 

[HarmonyPatch(typeof(ZoneBlock))]
[UsedImplicitly]
public static class ZoneBlockPatches {
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ZoneBlock.SimulationStep)), UsedImplicitly]
    public static IEnumerable<CodeInstruction> TranspileSimulationStep(
        IEnumerable<CodeInstruction> instructions) =>
        RandomBuildingInfoPatcher.TranspileGetRandomBuildingInfoConsumer(instructions, 64);
}
