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

            if (!registry.TryGetKnownCoordinate(plan.TargetCoordinateId, out KnownCoordinate? coordinate) || coordinate is null)
            {
                report.Add(DefinitionStatus.RecoverableError, $"空投计划 {plan.DropPlanId} 引用缺失坐标：{plan.TargetCoordinateId}");
            }

            int usedUnitCapacity = 0;
            foreach (string unitInstanceId in plan.SelectedAwakenedUnitInstanceIds.Concat(plan.SelectedMassUnitInstanceIds).Concat(plan.CreatedUnitInstanceIds))
            {
                if (saveGame.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance) &&
                    registry.TryGetUnit(unitInstance.UnitId, out UnitData? unitData) &&
                    unitData is not null)
                {
                    usedUnitCapacity += UnitCapacityCost(unitData, unitInstance.UnitId);
                }
            }

            if (usedUnitCapacity > pod.UnitCapacity)
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
                if (!registry.TryGetItem(platformItem.ItemId, out ItemData? itemData) ||
                    itemData is null ||
                    itemData.Category != "unit_platform" ||
                    itemData.TargetUnitId != platformItem.TargetUnitId)
                {
                    report.Add(DefinitionStatus.RecoverableError, $"空投计划 {plan.DropPlanId} 引用无效单位平台：{platformItem.ItemId}");
                    continue;
                }

                if (!registry.TryGetUnit(platformItem.TargetUnitId, out UnitData? unitData) || unitData is null)
                {
                    report.Add(DefinitionStatus.RecoverableError, $"空投计划 {plan.DropPlanId} 引用缺失目标单位：{platformItem.TargetUnitId}");
                }

                recalculatedWeight += itemData.UnitWeight * platformItem.Count;
                InventoryTransferResult fitResult = simulatedCargo.AddStack(new ItemStack { ItemId = platformItem.ItemId, Count = platformItem.Count }, registry);
                if (!fitResult.IsSuccess)
                {
                    report.Add(DefinitionStatus.RecoverableError, $"空投计划 {plan.DropPlanId} 平台货舱容量非法：{fitResult.Message}");
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

            foreach (string unitInstanceId in plan.SelectedMassUnitInstanceIds)
            {
                if (!saveGame.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance) || unitInstance.IsAwakened)
                {
                    report.Add(DefinitionStatus.RecoverableError, $"空投计划 {plan.DropPlanId} 引用无效量产单位实例：{unitInstanceId}");
                }
            }

            foreach (string unitInstanceId in plan.CreatedUnitInstanceIds)
            {
                if (!saveGame.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance) || unitInstance.IsAwakened)
                {
                    report.Add(DefinitionStatus.RecoverableError, $"空投计划 {plan.DropPlanId} 引用缺失平台生成单位：{unitInstanceId}");
                }
            }

            ValidateDropPlanTransferReferences(saveGame, plan, report);
        }
    }

    private static void ValidateDropPlanTransferReferences(SaveGame saveGame, DropPlan plan, DataLoadReport report)
    {
        foreach (string transferId in plan.RelatedTransferIds)
        {
            if (saveGame.InventoryTransfers.All(transfer => transfer.TransferId != transferId))
            {
                report.Add(DefinitionStatus.RecoverableError, $"空投计划 {plan.DropPlanId} 引用缺失转移记录：{transferId}");
            }
        }
    }

    private static int UnitCapacityCost(UnitData unitData, string unitId)
    {
        return unitId is "heavy_cargo_spider" or "rockbreaker" || unitData.Tags.Contains("heavy") ? 2 : 1;
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

        if (!saveGame.DropPlans.TryGetValue(activeExpedition.DropPlanId, out DropPlan? dropPlan))
        {
            report.Add(DefinitionStatus.RecoverableError, $"当前远征引用缺失空投计划：{activeExpedition.DropPlanId}");
        }
        else
        {
            ValidateActiveExpeditionDropPlan(saveGame, activeExpedition, dropPlan, report);
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

        ValidateActiveExpeditionUnitInventories(saveGame, activeExpedition, report);
        ValidateActiveExpeditionMinerals(saveGame, registry, activeExpedition, report);
        ValidateActiveExpeditionGatherRecords(saveGame, registry, activeExpedition, report);
        ValidateActiveExpeditionRepairRecords(saveGame, registry, activeExpedition, report);
        ValidateActiveExpeditionProductionAndPower(saveGame, registry, activeExpedition, report);
    }

    private static void ValidateActiveExpeditionUnitInventories(SaveGame saveGame, ExpeditionState activeExpedition, DataLoadReport report)
    {
        foreach (string unitInstanceId in activeExpedition.ActiveUnitInstanceIds)
        {
            if (!saveGame.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance))
            {
                report.Add(DefinitionStatus.RecoverableError, $"当前远征引用缺失单位实例：{unitInstanceId}");
                continue;
            }

            if (string.IsNullOrEmpty(unitInstance.InventoryId) ||
                !saveGame.Inventories.TryGetValue(unitInstance.InventoryId, out InventoryContainer? unitInventory) ||
                unitInventory.OwnerType != "unit_inventory" ||
                unitInventory.OwnerId != unitInstance.UnitInstanceId ||
                !activeExpedition.LocationInventoryIds.Contains(unitInventory.InventoryId))
            {
                report.Add(DefinitionStatus.RecoverableError, $"当前远征单位缺少远征背包库存：{unitInstanceId}");
            }
        }
    }

    private static void ValidateActiveExpeditionMinerals(SaveGame saveGame, DataRegistry registry, ExpeditionState activeExpedition, DataLoadReport report)
    {
        foreach (MineralDepositInstance mineralInstance in activeExpedition.MineralDepositStates.Values)
        {
            if (!registry.TryGetMineralDeposit(mineralInstance.MineralDepositId, out MineralDepositData? mineralData) || mineralData is null)
            {
                report.Add(DefinitionStatus.RecoverableError, $"矿产点实例 {mineralInstance.MineralDepositInstanceId} 引用缺失定义：{mineralInstance.MineralDepositId}");
                continue;
            }

            if (mineralInstance.RemainingYield < 0 || mineralInstance.RemainingYield > mineralData.MaxYield)
            {
                report.Add(DefinitionStatus.RecoverableError, $"矿产点实例 {mineralInstance.MineralDepositInstanceId} 剩余量非法");
            }

            if (mineralInstance.IsDiscovered &&
                !activeExpedition.MapState.DiscoveredMineralDepositIds.Contains(mineralInstance.MineralDepositInstanceId))
            {
                report.Add(DefinitionStatus.RecoverableError, $"已发现矿产点未写入地图发现列表：{mineralInstance.MineralDepositInstanceId}");
            }
        }
    }

    private static void ValidateActiveExpeditionGatherRecords(SaveGame saveGame, DataRegistry registry, ExpeditionState activeExpedition, DataLoadReport report)
    {
        foreach (GatherRecord record in activeExpedition.GatherRecords)
        {
            if (!activeExpedition.ActiveUnitInstanceIds.Contains(record.UnitInstanceId) && record.Result != "failed")
            {
                report.Add(DefinitionStatus.RecoverableError, $"采集记录引用非当前远征单位：{record.GatherRecordId}");
            }

            if (!string.IsNullOrEmpty(record.MineralDepositInstanceId) &&
                !activeExpedition.MineralDepositStates.ContainsKey(record.MineralDepositInstanceId))
            {
                report.Add(DefinitionStatus.RecoverableError, $"采集记录引用缺失矿产点：{record.GatherRecordId}");
            }

            if (!string.IsNullOrEmpty(record.ItemId) &&
                !registry.TryGetItem(record.ItemId, out ItemData? _))
            {
                report.Add(DefinitionStatus.RecoverableError, $"采集记录引用缺失道具：{record.GatherRecordId}");
            }

            if (!string.IsNullOrEmpty(record.TransferId) &&
                saveGame.InventoryTransfers.All(transfer => transfer.TransferId != record.TransferId))
            {
                report.Add(DefinitionStatus.RecoverableError, $"采集记录引用缺失库存转移：{record.GatherRecordId}");
            }

            if (!string.IsNullOrEmpty(record.DestinationInventoryId) &&
                !saveGame.Inventories.ContainsKey(record.DestinationInventoryId))
            {
                report.Add(DefinitionStatus.RecoverableError, $"采集记录引用缺失目标库存：{record.GatherRecordId}");
            }

            if (!string.IsNullOrEmpty(record.GroundItemStateId) &&
                !saveGame.GroundItems.ContainsKey(record.GroundItemStateId))
            {
                report.Add(DefinitionStatus.RecoverableError, $"采集记录引用缺失地上道具：{record.GatherRecordId}");
            }
        }
    }

    private static void ValidateActiveExpeditionProductionAndPower(SaveGame saveGame, DataRegistry registry, ExpeditionState activeExpedition, DataLoadReport report)
    {
        foreach (ProductionJobState job in activeExpedition.ProductionJobs)
        {
            if (!saveGame.BuildingInstances.ContainsKey(job.BuildingInstanceId))
            {
                report.Add(DefinitionStatus.RecoverableError, $"生产任务引用缺失建筑：{job.ProductionJobId}");
            }

            if (!registry.Recipes.ContainsKey(job.RecipeId))
            {
                report.Add(DefinitionStatus.RecoverableError, $"生产任务引用缺失配方：{job.ProductionJobId}");
            }

            if (!string.IsNullOrEmpty(job.InputInventoryId) && !saveGame.Inventories.ContainsKey(job.InputInventoryId))
            {
                report.Add(DefinitionStatus.RecoverableError, $"生产任务引用缺失输入库存：{job.ProductionJobId}");
            }

            if (!string.IsNullOrEmpty(job.OutputInventoryId) && !saveGame.Inventories.ContainsKey(job.OutputInventoryId))
            {
                report.Add(DefinitionStatus.RecoverableError, $"生产任务引用缺失输出库存：{job.ProductionJobId}");
            }
        }

        foreach (PowerNetworkState network in activeExpedition.PowerNetworkStates)
        {
            if (network.TotalGeneration < 0 || network.TotalConsumption < 0 || network.StorageCapacity < 0)
            {
                report.Add(DefinitionStatus.RecoverableError, $"电力网络数值非法：{network.PowerNetworkId}");
            }
        }
    }

    private static void ValidateActiveExpeditionRepairRecords(SaveGame saveGame, DataRegistry registry, ExpeditionState activeExpedition, DataLoadReport report)
    {
        foreach (RepairRecord record in activeExpedition.RepairRecords)
        {
            if (!activeExpedition.ActiveUnitInstanceIds.Contains(record.UnitInstanceId) && record.Result != "failed")
            {
                report.Add(DefinitionStatus.RecoverableError, $"维修记录引用非当前远征单位：{record.RepairRecordId}");
            }

            if (record.TargetType == "building_friendly" && !saveGame.BuildingInstances.ContainsKey(record.TargetId) && record.Result != "failed")
            {
                report.Add(DefinitionStatus.RecoverableError, $"维修记录引用缺失建筑：{record.RepairRecordId}");
            }

            foreach (ItemStack stack in record.ConsumedItems)
            {
                if (!registry.TryGetItem(stack.ItemId, out ItemData? _))
                {
                    report.Add(DefinitionStatus.RecoverableError, $"维修记录引用缺失消耗道具：{record.RepairRecordId}");
                }
            }

            foreach (string transferId in record.ConsumedTransferIds)
            {
                if (saveGame.InventoryTransfers.All(transfer => transfer.TransferId != transferId))
                {
                    report.Add(DefinitionStatus.RecoverableError, $"维修记录引用缺失转移记录：{record.RepairRecordId}");
                }
            }
        }
    }

    private static void ValidateActiveExpeditionDropPlan(
        SaveGame saveGame,
        ExpeditionState activeExpedition,
        DropPlan dropPlan,
        DataLoadReport report)
    {
        if (activeExpedition.Seed != dropPlan.Seed ||
            activeExpedition.TargetCoordinateId != dropPlan.TargetCoordinateId ||
            activeExpedition.DropPosition != dropPlan.TargetCoordinate)
        {
            report.Add(DefinitionStatus.RecoverableError, $"当前远征与空投计划目标不一致：{activeExpedition.ExpeditionId}");
        }

        if (string.IsNullOrEmpty(activeExpedition.DropPodCargoInventoryId) ||
            !saveGame.Inventories.TryGetValue(activeExpedition.DropPodCargoInventoryId, out InventoryContainer? dropCargo))
        {
            report.Add(DefinitionStatus.RecoverableError, $"当前远征缺少空投货舱库存：{activeExpedition.ExpeditionId}");
        }
        else
        {
            if (dropCargo.OwnerType != "drop_pod_cargo" || dropCargo.OwnerId != dropPlan.DropPodId)
            {
                report.Add(DefinitionStatus.RecoverableError, $"当前远征空投货舱归属非法：{dropCargo.InventoryId}");
            }

            if (!activeExpedition.LocationInventoryIds.Contains(dropCargo.InventoryId))
            {
                report.Add(DefinitionStatus.RecoverableError, $"当前远征位置库存缺少空投货舱：{dropCargo.InventoryId}");
            }
        }

        foreach (string unitInstanceId in dropPlan.SelectedAwakenedUnitInstanceIds.Concat(dropPlan.SelectedMassUnitInstanceIds).Concat(dropPlan.CreatedUnitInstanceIds))
        {
            if (!activeExpedition.ActiveUnitInstanceIds.Contains(unitInstanceId))
            {
                report.Add(DefinitionStatus.RecoverableError, $"当前远征缺少空投计划单位：{unitInstanceId}");
                continue;
            }

            if (saveGame.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance) &&
                unitInstance.LockedByExpeditionId != activeExpedition.ExpeditionId)
            {
                report.Add(DefinitionStatus.RecoverableError, $"单位实例未锁定到当前远征：{unitInstanceId}");
            }
        }

        HashSet<string> plannedUnitIds = new(dropPlan.SelectedAwakenedUnitInstanceIds.Concat(dropPlan.SelectedMassUnitInstanceIds).Concat(dropPlan.CreatedUnitInstanceIds));
        foreach (string unitInstanceId in activeExpedition.ActiveUnitInstanceIds)
        {
            if (plannedUnitIds.Contains(unitInstanceId))
            {
                continue;
            }

            if (saveGame.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance) &&
                unitInstance.CurrentCommand == $"expedition:{activeExpedition.ExpeditionId}")
            {
                report.Add(DefinitionStatus.RecoverableError, $"当前远征存在未由空投计划带入的初始单位：{unitInstanceId}");
            }
        }

        ValidateDropPlanTransferPayloads(saveGame, activeExpedition, dropPlan, report);
    }

    private static void ValidateDropPlanTransferPayloads(
        SaveGame saveGame,
        ExpeditionState activeExpedition,
        DropPlan dropPlan,
        DataLoadReport report)
    {
        Dictionary<string, int> expectedStacks = SumStacks(dropPlan.SelectedStackItems);
        Dictionary<string, int> actualStacks = new();
        HashSet<string> expectedInstances = new(dropPlan.SelectedItemInstanceIds);
        HashSet<string> actualInstances = new();
        Dictionary<string, int> expectedPlatforms = new();
        Dictionary<string, int> actualPlatforms = new();
        foreach (SelectedUnitPlatformItem platformItem in dropPlan.SelectedUnitPlatformItems)
        {
            AddCount(expectedPlatforms, platformItem.ItemId, platformItem.Count);
        }

        foreach (string transferId in dropPlan.RelatedTransferIds)
        {
            InventoryTransfer? transfer = saveGame.InventoryTransfers.FirstOrDefault(candidate => candidate.TransferId == transferId);
            if (transfer is null)
            {
                continue;
            }

            if (transfer.ExpeditionId != activeExpedition.ExpeditionId)
            {
                report.Add(DefinitionStatus.RecoverableError, $"空投转移记录远征不匹配：{transferId}");
            }

            if (transfer.Reason == "drop_plan_load")
            {
                if (transfer.FromInventoryId != saveGame.GameSession.OrbitState.InventoryId ||
                    transfer.ToInventoryId != activeExpedition.DropPodCargoInventoryId)
                {
                    report.Add(DefinitionStatus.RecoverableError, $"空投装载转移方向非法：{transferId}");
                }

                if (transfer.ItemInstanceIds.Count == 0)
                {
                    AddCount(actualStacks, transfer.ItemId, transfer.Count);
                }
                else
                {
                    foreach (string itemInstanceId in transfer.ItemInstanceIds)
                    {
                        actualInstances.Add(itemInstanceId);
                    }
                }
            }
            else if (transfer.Reason == "drop_platform_assemble")
            {
                if (transfer.FromInventoryId != saveGame.GameSession.OrbitState.InventoryId ||
                    transfer.ToInventoryId != $"unit_creation:{activeExpedition.ExpeditionId}")
                {
                    report.Add(DefinitionStatus.RecoverableError, $"单位平台转移方向非法：{transferId}");
                }

                AddCount(actualPlatforms, transfer.ItemId, transfer.Count);
            }
            else
            {
                report.Add(DefinitionStatus.RecoverableError, $"空投计划引用了非空投转移记录：{transferId}");
            }
        }

        CompareCounts(expectedStacks, actualStacks, $"空投计划 {dropPlan.DropPlanId} 堆叠物资转移记录不一致", report);
        CompareSets(expectedInstances, actualInstances, $"空投计划 {dropPlan.DropPlanId} 实例道具转移记录不一致", report);
        CompareCounts(expectedPlatforms, actualPlatforms, $"空投计划 {dropPlan.DropPlanId} 单位平台转移记录不一致", report);
    }

    private static Dictionary<string, int> SumStacks(IEnumerable<ItemStack> stacks)
    {
        Dictionary<string, int> counts = new();
        foreach (ItemStack stack in stacks)
        {
            AddCount(counts, stack.ItemId, stack.Count);
        }

        return counts;
    }

    private static void AddCount(Dictionary<string, int> counts, string itemId, int count)
    {
        counts.TryGetValue(itemId, out int existingCount);
        counts[itemId] = existingCount + count;
    }

    private static void CompareCounts(Dictionary<string, int> expected, Dictionary<string, int> actual, string message, DataLoadReport report)
    {
        if (expected.Count != actual.Count || expected.Any(pair => !actual.TryGetValue(pair.Key, out int count) || count != pair.Value))
        {
            report.Add(DefinitionStatus.RecoverableError, message);
        }
    }

    private static void CompareSets(HashSet<string> expected, HashSet<string> actual, string message, DataLoadReport report)
    {
        if (!expected.SetEquals(actual))
        {
            report.Add(DefinitionStatus.RecoverableError, message);
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
