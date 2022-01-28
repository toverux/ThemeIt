using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ColossalFramework.Math;
using HarmonyLib;
using UnityEngine;

namespace ThemeIt;

public static class Patches {
    private static readonly MethodInfo GetRandomBuildingInfoMethod = AccessTools.Method(
        typeof(BuildingManager),
        nameof(BuildingManager.GetRandomBuildingInfo));

    private static readonly MethodInfo GetRandomBuildingInfoPatchMethod = AccessTools.Method(
        typeof(Patches),
        nameof(Patches.GetRandomBuildingInfoPatch));
    
    public static IEnumerable<CodeInstruction> TranspileZoneBlockSimulationStep(
        IEnumerable<CodeInstruction> instructions) =>
        Patches.TranspileGetRandomBuildingInfoConsumer(instructions, 64);
    
    public static IEnumerable<CodeInstruction> TranspilePrivateBuildingAiGetUpgradeInfo(
        IEnumerable<CodeInstruction> instructions) =>
        Patches.TranspileGetRandomBuildingInfoConsumer(instructions, 4);

    public static IEnumerable<CodeInstruction> TranspilePrivateBuildingAiSimulationStep(
        IEnumerable<CodeInstruction> instructions) =>
        Patches.TranspileGetRandomBuildingInfoConsumer(instructions, 21);

    public static IEnumerable<CodeInstruction> TranspileGetRandomBuildingInfoConsumer(
        IEnumerable<CodeInstruction> instructions,
        int districtLocalVariableIndex) {

        var targetSiteFound = false;

        foreach (var instruction in instructions) {
            if (instruction.Is(OpCodes.Callvirt, Patches.GetRandomBuildingInfoMethod)) {
                targetSiteFound = true;

                yield return new CodeInstruction(OpCodes.Ldloc_S, districtLocalVariableIndex);
                yield return new CodeInstruction(OpCodes.Call, Patches.GetRandomBuildingInfoPatchMethod);
            }
            else {
                yield return instruction;
            }
        }

        if (!targetSiteFound) {
            throw new Exception("Cannot find target site to apply patch on.");
        }
    }

    private static BuildingInfo GetRandomBuildingInfoPatch(
        BuildingManager instance,
        ref Randomizer randomizer,
        ItemClass.Service service,
        ItemClass.SubService subService,
        ItemClass.Level level,
        int width,
        int length,
        BuildingInfo.ZoningMode zoningMode,
        int style,
        byte district) {

        Debug.Log("Called GetRandomBuildingInfo");
        Debug.Log($"service={service} subService={subService} level={level} width={width} length={length} zoningMode={zoningMode} district={district}");

        return instance.GetRandomBuildingInfo(
            ref randomizer, service, subService, level, width, length, zoningMode, style);
    }
}
