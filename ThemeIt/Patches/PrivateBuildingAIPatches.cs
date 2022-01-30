using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;

namespace ThemeIt.Patches; 

[HarmonyPatch(typeof(PrivateBuildingAI)), UsedImplicitly]
// ReSharper disable once InconsistentNaming
public static class PrivateBuildingAIPatches {
    [HarmonyTranspiler, UsedImplicitly]
    [HarmonyPatch(nameof(PrivateBuildingAI.GetUpgradeInfo))]
    public static IEnumerable<CodeInstruction> TranspileGetUpgradeInfo(
        IEnumerable<CodeInstruction> instructions) =>
        RandomBuildingInfoPatcher.TranspileGetRandomBuildingInfoConsumer(instructions, 4);
    
    [HarmonyTranspiler, UsedImplicitly]
    [HarmonyPatch(nameof(PrivateBuildingAI.SimulationStep))]
    public static IEnumerable<CodeInstruction> TranspileSimulationStep(
        IEnumerable<CodeInstruction> instructions) =>
        RandomBuildingInfoPatcher.TranspileGetRandomBuildingInfoConsumer(instructions, 21);
}
