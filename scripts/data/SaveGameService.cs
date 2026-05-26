using System.Collections.Generic;
using System.Linq;

namespace GodotGame;

public sealed class SaveGameService
{
    public DataLoadReport Validate(SaveGame saveGame, DataRegistry registry)
    {
        DataLoadReport report = new();
        if (saveGame.SaveVersion <= 0)
        {
            report.Add(DefinitionStatus.FatalError, "存档缺少有效版本字段");
        }

        ValidateInventories(saveGame, registry, report);
        ValidateInstances(saveGame, registry, report);
        ValidateDropPlans(saveGame, registry, report);
        ValidateActiveExpedition(saveGame, registry, report);
        ValidateRunRecords(saveGame, report);
        ValidateInstanceOwnership(saveGame, report);
        return report;
    }

    private static void ValidateInstances(SaveGame saveGame, DataRegistry registry, DataLoadReport report)
    {
        foreach (ItemInstance itemInstance in saveGame.ItemInstances.Values)
        {
            if (!registry.TryGetItem(itemInstance.ItemId, out ItemData? itemData) || itemData is null)
            {
                report.Add(DefinitionStatus.RecoverableError, $"道具实例 {itemInstance.InstanceId} 引用缺失道具：{itemInstance.ItemId}");
            }
        }

        foreach (UnitInstance unitInstance in saveGame.UnitInstances.Values)
        {
            if (!registry.TryGetUnit(unitInstance.UnitId, out UnitData? unitData) || unitData is null)
            {
                report.Add(DefinitionStatus.RecoverableError, $"单位实例 {unitInstance.UnitInstanceId} 引用缺失单位：{unitInstance.UnitId}");
            }
        }

        foreach (BuildingInstance buildingInstance in saveGame.BuildingInstances.Values)
        {
            if (!registry.TryGetBuilding(buildingInstance.BuildingId, out BuildingData? buildingData) || buildingData is null)
            {
                report.Add(DefinitionStatus.RecoverableError, $"建筑实例 {buildingInstance.BuildingInstanceId} 引用缺失建筑：{buildingInstance.BuildingId}");
            }
        }
    }

    private static void ValidateInventories(SaveGame saveGame, DataRegistry registry, DataLoadReport report)
    {
        foreach (InventoryContainer inventory in saveGame.Inventories.Values)
        {
            foreach (ItemStack stack in inventory.ItemStacks)
            {
                if (stack.Count <= 0)
                {
                    report.Add(DefinitionStatus.RecoverableError, $"库存 {inventory.InventoryId} 存在非法数量：{stack.ItemId}");
                }

                if (!registry.TryGetItem(stack.ItemId, out ItemData? itemData) || itemData is null)
                {
                    report.Add(DefinitionStatus.RecoverableError, $"库存 {inventory.InventoryId} 引用缺失道具：{stack.ItemId}");
                    continue;
                }

                if (itemData.RequiresInstance)
                {
                    report.Add(DefinitionStatus.RecoverableError, $"库存 {inventory.InventoryId} 用堆叠保存实例道具：{stack.ItemId}");
                }
            }

            foreach (string itemInstanceId in inventory.ItemInstanceIds)
            {
                if (!saveGame.ItemInstances.ContainsKey(itemInstanceId))
                {
                    report.Add(DefinitionStatus.RecoverableError, $"库存 {inventory.InventoryId} 引用缺失道具实例：{itemInstanceId}");
                }
            }
        }
    }

    private static void ValidateDropPlans(SaveGame saveGame, DataRegistry registry, DataLoadReport report)
    {
        foreach (DropPlan plan in saveGame.DropPlans.Values)
        {
            if (!registry.TryGetDropPod(plan.DropPodId, out DropPodData? pod) || pod is null)
            {
                report.Add(DefinitionStatus.RecoverableError, $"空投计划 {plan.DropPlanId} 引用缺失空投舱：{plan.DropPodId}");
                continue;
            }

            if (plan.SelectedAwakenedUnitInstanceIds.Count > pod.UnitCapacity)
            {
                report.Add(DefinitionStatus.RecoverableError, $"空投计划 {plan.DropPlanId} 超过单位容量");
            }

            InventoryContainer simulatedCargo = new()
            {
                InventoryId = $"validate_{plan.DropPlanId}",
                OwnerType = "drop_pod_cargo",
                OwnerId = pod.Id,
                SlotLimit = pod.SlotLimit,
                WeightLimit = pod.WeightLimit
            };
            simulatedCargo.AcceptedTags.AddRange(pod.AcceptedTags);
            simulatedCargo.BlockedTags.AddRange(pod.BlockedTags);
            float recalculatedWeight = 0f;

            foreach (SelectedUnitPlatformItem platformItem in plan.SelectedUnitPlatformItems)
            {
                if (!registry.TryGetUnit(platformItem.TargetUnitId, out UnitData? unitData) || unitData is null)
                {
                    report.Add(DefinitionStatus.RecoverableError, $"空投计划 {plan.DropPlanId} 引用缺失目标单位：{platformItem.TargetUnitId}");
                }
            }

            foreach (ItemStack stack in plan.SelectedStackItems)
            {
                if (stack.Count <= 0)
                {
                    report.Add(DefinitionStatus.RecoverableError, $"空投计划 {plan.DropPlanId} 存在非法道具数量：{stack.ItemId}");
                }

                if (!registry.TryGetItem(stack.ItemId, out ItemData? itemData) || itemData is null)
                {
                    report.Add(DefinitionStatus.RecoverableError, $"空投计划 {plan.DropPlanId} 引用缺失道具：{stack.ItemId}");
                    continue;
                }

                if (pod.BlockedTags.Exists(itemData.Tags.Contains) ||
                    !(pod.AcceptedTags.Exists(itemData.Tags.Contains) || pod.AcceptedTags.Contains(itemData.Category)))
                {
                    report.Add(DefinitionStatus.RecoverableError, $"空投计划 {plan.DropPlanId} 携带空投舱不接受的道具：{stack.ItemId}");
                }

                recalculatedWeight += itemData.UnitWeight * stack.Count;
                InventoryTransferResult fitResult = simulatedCargo.AddStack(stack, registry);
                if (!fitResult.IsSuccess)
                {
                    report.Add(DefinitionStatus.RecoverableError, $"空投计划 {plan.DropPlanId} 货舱容量非法：{fitResult.Message}");
                }
            }

            foreach (string itemInstanceId in plan.SelectedItemInstanceIds)
            {
                if (!saveGame.ItemInstances.TryGetValue(itemInstanceId, out ItemInstance? itemInstance))
                {
                    report.Add(DefinitionStatus.RecoverableError, $"空投计划 {plan.DropPlanId} 引用缺失道具实例：{itemInstanceId}");
                    continue;
                }

                if (!registry.TryGetItem(itemInstance.ItemId, out ItemData? itemData) || itemData is null)
                {
                    report.Add(DefinitionStatus.RecoverableError, $"空投计划 {plan.DropPlanId} 的实例引用缺失道具：{itemInstance.ItemId}");
                    continue;
                }

                if (pod.BlockedTags.Exists(itemData.Tags.Contains) ||
                    !(pod.AcceptedTags.Exists(itemData.Tags.Contains) || pod.AcceptedTags.Contains(itemData.Category)))
                {
                    report.Add(DefinitionStatus.RecoverableError, $"空投计划 {plan.DropPlanId} 携带空投舱不接受的实例：{itemInstanceId}");
                }

                recalculatedWeight += itemData.UnitWeight;
                InventoryTransferResult fitResult = simulatedCargo.AddItemInstance(itemInstanceId, saveGame.ItemInstances, registry);
                if (!fitResult.IsSuccess)
                {
                    report.Add(DefinitionStatus.RecoverableError, $"空投计划 {plan.DropPlanId} 实例货舱容量非法：{fitResult.Message}");
                }
            }

            if (recalculatedWeight > pod.WeightLimit)
            {
                report.Add(DefinitionStatus.RecoverableError, $"空投计划 {plan.DropPlanId} 重新计算后超过载重限制");
            }

            foreach (string unitInstanceId in plan.SelectedAwakenedUnitInstanceIds)
            {
                if (!saveGame.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance) || !unitInstance.IsAwakened)
                {
                    report.Add(DefinitionStatus.RecoverableError, $"空投计划 {plan.DropPlanId} 引用无效觉醒者实例：{unitInstanceId}");
                }
            }
        }
    }

    private static void ValidateRunRecords(SaveGame saveGame, DataLoadReport report)
    {
        foreach (RunRecord record in saveGame.GameSession.RunRecords)
        {
            foreach (string transferId in record.RelatedTransferIds)
            {
                if (saveGame.InventoryTransfers.All(transfer => transfer.TransferId != transferId))
                {
                    report.Add(DefinitionStatus.RecoverableError, $"轮次记录 {record.RunRecordId} 引用缺失转移记录：{transferId}");
                }
            }
        }
    }

    private static void ValidateActiveExpedition(SaveGame saveGame, DataRegistry registry, DataLoadReport report)
    {
        ExpeditionState? activeExpedition = saveGame.GameSession.ActiveExpedition;
        if (activeExpedition is null)
        {
            return;
        }

        if (!saveGame.DropPlans.ContainsKey(activeExpedition.DropPlanId))
        {
            report.Add(DefinitionStatus.RecoverableError, $"当前远征引用缺失空投计划：{activeExpedition.DropPlanId}");
        }

        if (!string.IsNullOrEmpty(activeExpedition.RocketState.CargoInventoryId) &&
            saveGame.Inventories.TryGetValue(activeExpedition.RocketState.CargoInventoryId, out InventoryContainer? rocketCargo))
        {
            float cargoWeight = rocketCargo.GetTotalWeight(registry, saveGame.ItemInstances);
            if (cargoWeight > activeExpedition.RocketState.CargoWeightLimit && !activeExpedition.RocketState.IsOverloaded)
            {
                report.Add(DefinitionStatus.RecoverableError, $"当前远征火箭货舱超载但未标记：{activeExpedition.ExpeditionId}");
            }
        }
    }

    private static void ValidateInstanceOwnership(SaveGame saveGame, DataLoadReport report)
    {
        Dictionary<string, string> owners = new();
        foreach (InventoryContainer inventory in saveGame.Inventories.Values)
        {
            foreach (string itemInstanceId in inventory.ItemInstanceIds)
            {
                if (owners.TryGetValue(itemInstanceId, out string? existingOwner))
                {
                    report.Add(DefinitionStatus.FatalError, $"道具实例 {itemInstanceId} 同时属于 {existingOwner} 和 {inventory.InventoryId}");
                }
                else
                {
                    owners[itemInstanceId] = inventory.InventoryId;
                }
            }
        }
    }
}
