using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;

namespace ThemeIt.Patches; 

[HarmonyPatch(typeof(PrivateBuildingAI)), UsedImplicitly]
// ReSharper disable once InconsistentNaming
public static class PrivateBuildingAIPatches {
    [HarmonyTranspiler, UsedImplicitly]
    [HarmonyPatch(
        nameof(PrivateBuildingAI.GetUpgradeInfo),
        new[] { typeof(ushort), typeof(Building) },
        new[] { ArgumentType.Normal, ArgumentType.Ref })]
    public static IEnumerable<CodeInstruction> TranspileGetUpgradeInfo(
        IEnumerable<CodeInstruction> instructions) =>
        RandomBuildingInfoPatcher.TranspileGetRandomBuildingInfoConsumer(instructions, 4);

    [HarmonyTranspiler, UsedImplicitly]
    [HarmonyPatch(
        nameof(PrivateBuildingAI.SimulationStep),
        new[] { typeof(ushort), typeof(Building), typeof(Building.Frame) },
        new[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref })]
    public static IEnumerable<CodeInstruction> TranspileSimulationStep(
        IEnumerable<CodeInstruction> instructions) =>
        RandomBuildingInfoPatcher.TranspileGetRandomBuildingInfoConsumer(instructions, 21);
}
