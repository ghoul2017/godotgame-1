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
        ValidateOrbitTransactions(saveGame, registry, report);
        ValidateSurfaceLocationState(saveGame, registry, report);
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

    private static void ValidateOrbitTransactions(SaveGame saveGame, DataRegistry registry, DataLoadReport report)
    {
        foreach (OrbitTransactionRecord record in saveGame.OrbitTransactionRecords)
        {
            if (record.TransactionType is not ("trade" or "research"))
            {
                report.Add(DefinitionStatus.RecoverableError, $"轨道审计记录 {record.TransactionId} 类型非法：{record.TransactionType}");
            }

            foreach (ItemStack stack in record.CostItems.Concat(record.RewardItems))
            {
                if (stack.Count <= 0)
                {
                    report.Add(DefinitionStatus.RecoverableError, $"轨道审计记录 {record.TransactionId} 存在非法数量：{stack.ItemId}");
                }

                if (!registry.TryGetItem(stack.ItemId, out ItemData? itemData) || itemData is null)
                {
                    report.Add(DefinitionStatus.RecoverableError, $"轨道审计记录 {record.TransactionId} 引用缺失道具：{stack.ItemId}");
                }
            }

            foreach (string transferId in record.RelatedTransferIds)
            {
                if (saveGame.InventoryTransfers.All(transfer => transfer.TransferId != transferId))
                {
                    report.Add(DefinitionStatus.RecoverableError, $"轨道审计记录 {record.TransactionId} 引用缺失转移记录：{transferId}");
                }
            }

            foreach (string itemInstanceId in record.RewardItemInstanceIds)
            {
                if (!saveGame.ItemInstances.ContainsKey(itemInstanceId))
                {
                    report.Add(DefinitionStatus.RecoverableError, $"轨道审计记录 {record.TransactionId} 引用缺失奖励实例：{itemInstanceId}");
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

        foreach (string groundItemStateId in activeExpedition.GroundItemStateIds)
        {
            if (!saveGame.GroundItems.ContainsKey(groundItemStateId))
            {
                report.Add(DefinitionStatus.RecoverableError, $"当前远征引用缺失地上道具：{groundItemStateId}");
            }
        }

        foreach (string constructionSiteId in activeExpedition.ConstructionSiteIds)
        {
            if (!saveGame.ConstructionSites.ContainsKey(constructionSiteId))
            {
                report.Add(DefinitionStatus.RecoverableError, $"当前远征引用缺失施工点：{constructionSiteId}");
            }
        }

        foreach (string logisticsOrderId in activeExpedition.LogisticsOrderIds)
        {
            if (!saveGame.LogisticsOrders.ContainsKey(logisticsOrderId))
            {
                report.Add(DefinitionStatus.RecoverableError, $"当前远征引用缺失物流订单：{logisticsOrderId}");
            }
        }
    }

    private static void ValidateSurfaceLocationState(SaveGame saveGame, DataRegistry registry, DataLoadReport report)
    {
        foreach (GroundItemState groundItem in saveGame.GroundItems.Values)
        {
            if (groundItem.Stack.Count > 0 &&
                !registry.TryGetItem(groundItem.Stack.ItemId, out ItemData? _))
            {
                report.Add(DefinitionStatus.RecoverableError, $"地上道具 {groundItem.GroundItemStateId} 引用缺失道具：{groundItem.Stack.ItemId}");
            }
        }

        foreach (ConstructionSiteState constructionSite in saveGame.ConstructionSites.Values)
        {
            if (!registry.TryGetBuilding(constructionSite.BuildingId, out BuildingData? _))
            {
                report.Add(DefinitionStatus.RecoverableError, $"施工点 {constructionSite.ConstructionSiteId} 引用缺失建筑：{constructionSite.BuildingId}");
            }

            if (string.IsNullOrEmpty(constructionSite.DeliveredInventoryId) ||
                !saveGame.Inventories.TryGetValue(constructionSite.DeliveredInventoryId, out InventoryContainer? deliveredInventory) ||
                deliveredInventory.OwnerType != "construction_site")
            {
                report.Add(DefinitionStatus.RecoverableError, $"施工点 {constructionSite.ConstructionSiteId} 缺少施工点库存");
            }
        }

        foreach (LogisticsOrderState order in saveGame.LogisticsOrders.Values)
        {
            if (order.Count <= 0)
            {
                report.Add(DefinitionStatus.RecoverableError, $"物流订单 {order.LogisticsOrderId} 数量非法");
            }

            if (!registry.TryGetItem(order.ItemId, out ItemData? _))
            {
                report.Add(DefinitionStatus.RecoverableError, $"物流订单 {order.LogisticsOrderId} 引用缺失道具：{order.ItemId}");
            }

            if (string.IsNullOrEmpty(order.SourceLocation.LocationType) ||
                string.IsNullOrEmpty(order.TargetLocation.LocationType))
            {
                report.Add(DefinitionStatus.RecoverableError, $"物流订单 {order.LogisticsOrderId} 缺少来源或目标位置");
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
