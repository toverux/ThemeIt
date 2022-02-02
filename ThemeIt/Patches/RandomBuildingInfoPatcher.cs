using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ColossalFramework.Math;
using HarmonyLib;
using UnityEngine;

namespace ThemeIt.Patches;

internal static class RandomBuildingInfoPatcher {
    private static readonly MethodInfo GetDistrictMethod = AccessTools.Method(
        typeof(DistrictManager),
        nameof(DistrictManager.GetDistrict),
        new[] { typeof(Vector3) });

    private static readonly MethodInfo GetRandomBuildingInfoMethod = AccessTools.Method(
        typeof(BuildingManager),
        nameof(BuildingManager.GetRandomBuildingInfo));

    private static readonly MethodInfo GetRandomBuildingInfoPatchMethod = AccessTools.Method(
        typeof(RandomBuildingInfoPatcher),
        nameof(RandomBuildingInfoPatcher.GetRandomBuildingInfoPatch));

    /**
     * The original GetRandomBuildingInfo doesn't receive the district ID, which we need.
     * It's too bad, because GetRandomBuildingInfo already gets passed the style of the district, and any caller HAS to
     * know the district first, in order to retrieve its style and call GetRandomBuildingInfo.
     * We will patch any caller of GetRandomBuildingInfo, and replace its calls to that method with our patched static
     * method, but adding the district ID from the scope of the caller as an argument.
     * In the future, maybe we'll may also pass the Building data (when available) if we use the previous building info
     * to derive new info from it (eg. a building configured to upgrade to a predetermined building, a feature that
     * Building Themes had).
     */
    internal static IEnumerable<CodeInstruction> TranspileGetRandomBuildingInfoConsumer(
        IEnumerable<CodeInstruction> enumerableInstructions) {

        var instructions = new List<CodeInstruction>(enumerableInstructions);

        //=> Find the call to GetRandomBuilding. We will replace it.
        var getRandomBuildingInfoCalls = instructions.FindAll(instruction =>
            instruction.Calls(RandomBuildingInfoPatcher.GetRandomBuildingInfoMethod));

        //=> Check that we only have one call to GetRandomBuildingInfo as we only handle replacing one.
        if (getRandomBuildingInfoCalls.Count != 1) { // handle none or > 1
            throw new Exception("Only one call to GetRandomBuildingInfo, this is unexpected.");
        }

        var getRandomBuildingInfoIndex = instructions.IndexOf(getRandomBuildingInfoCalls[0]);

        //=> Look back a little to find in which local variable index the district is stored, for this we find the last
        //   call to GetDistrict() first...
        var lastGetDistrictIndex = instructions.FindLastIndex(instruction =>
            instruction.Calls(RandomBuildingInfoPatcher.GetDistrictMethod));

        //=> The return value of GetDistrict() should be immediately stored in the local variable, we will use this
        //   stloc.s to copy its operand: it's our local variable index.
        var districtStloc = instructions[lastGetDistrictIndex + 1];
        if (districtStloc.opcode != OpCodes.Stloc_S) {
            throw new Exception("Cannot find district variable in patched method.");
        }

        //=> Remove original callvirt to GetRandomBuildingInfo
        instructions.RemoveAt(getRandomBuildingInfoIndex);

        //=> Replace with a call to our patched GetRandomBuildingInfo, but with the district index added in arguments.
        instructions.InsertRange(getRandomBuildingInfoIndex, new[] {
            new CodeInstruction(OpCodes.Ldloc_S, districtStloc.operand),
            new CodeInstruction(OpCodes.Call, RandomBuildingInfoPatcher.GetRandomBuildingInfoPatchMethod)
        });

        return instructions;
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
        Debug.Log(Environment.StackTrace);
        Debug.Log($"service={service} subService={subService} level={level} width={width} length={length} zoningMode={zoningMode} district={district}");

        return instance.GetRandomBuildingInfo(
            ref randomizer, service, subService, level, width, length, zoningMode, style);
    }
}
