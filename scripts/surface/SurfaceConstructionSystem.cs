using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGame;

public sealed class SurfaceConstructionSystem
{
    private readonly GameSession _session;
    private readonly DataRegistry _registry;
    private readonly bool _allowDebugBlueprintBypass;

    public SurfaceConstructionSystem(GameSession session, DataRegistry registry, bool allowDebugBlueprintBypass = false)
    {
        _session = session;
        _registry = registry;
        _allowDebugBlueprintBypass = allowDebugBlueprintBypass;
    }

    public bool TryCreateConstructionSite(
        ExpeditionState expeditionState,
        string buildingId,
        Vector2I position,
        string assignedUnitInstanceId,
        out ConstructionSiteState constructionSite,
        out string message)
    {
        constructionSite = new ConstructionSiteState
        {
            ExpeditionId = expeditionState.ExpeditionId,
            BuildingId = buildingId,
            Position = position,
            AssignedUnitInstanceId = assignedUnitInstanceId,
            CreatedAt = Time.GetUnixTimeFromSystem()
        };

        if (!_registry.TryGetBuilding(buildingId, out BuildingData? buildingData) || buildingData is null)
        {
            message = $"找不到建筑定义：{buildingId}";
            constructionSite.FailureReason = message;
            return false;
        }

        if (!HasRequiredBlueprint(buildingData.RequiresBlueprintId))
        {
            message = $"缺少建筑蓝图：{buildingData.RequiresBlueprintId}";
            constructionSite.FailureReason = message;
            return false;
        }

        string inventoryId = UniqueInventoryId($"construction_site_inventory_{expeditionState.ExpeditionId}_{buildingId}");
        InventoryContainer deliveredInventory = new()
        {
            InventoryId = inventoryId,
            OwnerType = "construction_site",
            OwnerId = constructionSite.ConstructionSiteId,
            SlotLimit = Math.Max(8, buildingData.BuildCost.Count + 4),
            WeightLimit = Math.Max(200f, buildingData.BuildCost.Sum(_registry.GetStackWeight) * 2f)
        };
        _session.Inventories[deliveredInventory.InventoryId] = deliveredInventory;

        constructionSite.ConstructionSiteId = UniqueConstructionSiteId(expeditionState, buildingId);
        constructionSite.DeliveredInventoryId = deliveredInventory.InventoryId;
        deliveredInventory.OwnerId = constructionSite.ConstructionSiteId;
        constructionSite.RequiredItems.AddRange(CopyStacks(buildingData.BuildCost));
        _session.ConstructionSites[constructionSite.ConstructionSiteId] = constructionSite;
        expeditionState.ConstructionSiteIds.Add(constructionSite.ConstructionSiteId);
        expeditionState.LocationInventoryIds.Add(deliveredInventory.InventoryId);

        message = $"施工点已创建：{buildingData.DisplayName}";
        GD.Print($"[建造] 创建施工点：{constructionSite.ConstructionSiteId} {buildingId}");
        return true;
    }

    public bool TryDeliverConstructionMaterials(ExpeditionState expeditionState, string constructionSiteId, out string message)
    {
        if (!_session.ConstructionSites.TryGetValue(constructionSiteId, out ConstructionSiteState? constructionSite) ||
            !_session.Inventories.TryGetValue(constructionSite.DeliveredInventoryId, out InventoryContainer? deliveredInventory))
        {
            message = $"找不到施工点或施工库存：{constructionSiteId}";
            return false;
        }

        if (!TryPlanTransfers(
                expeditionState,
                MissingStacks(constructionSite.RequiredItems, deliveredInventory),
                deliveredInventory,
                string.Empty,
                out List<PlannedTransfer> plannedTransfers,
                out message))
        {
            constructionSite.FailureReason = message;
            return false;
        }

        if (!CommitTransfers(
            expeditionState,
            plannedTransfers,
            deliveredInventory,
            "construction_site",
            constructionSite.ConstructionSiteId,
            "construction_material_delivery",
            out List<InventoryTransfer> _,
            out string commitMessage))
        {
            constructionSite.FailureReason = commitMessage;
            message = commitMessage;
            return false;
        }

        constructionSite.State = "ready_to_build";
        constructionSite.FailureReason = string.Empty;
        message = $"施工材料已送达：{constructionSite.BuildingId}";
        GD.Print($"[物流] 施工材料送达：{constructionSite.ConstructionSiteId}");
        return true;
    }

    public bool TryCompleteConstruction(ExpeditionState expeditionState, string constructionSiteId, out BuildingInstance buildingInstance, out string message)
    {
        buildingInstance = new BuildingInstance();
        if (!_session.ConstructionSites.TryGetValue(constructionSiteId, out ConstructionSiteState? constructionSite) ||
            !_session.Inventories.TryGetValue(constructionSite.DeliveredInventoryId, out InventoryContainer? deliveredInventory))
        {
            message = $"找不到施工点或施工库存：{constructionSiteId}";
            return false;
        }

        if (!_registry.TryGetBuilding(constructionSite.BuildingId, out BuildingData? buildingData) || buildingData is null)
        {
            message = $"找不到建筑定义：{constructionSite.BuildingId}";
            constructionSite.FailureReason = message;
            return false;
        }

        foreach (ItemStack requiredStack in constructionSite.RequiredItems)
        {
            if (deliveredInventory.GetItemCount(requiredStack.ItemId) < requiredStack.Count)
            {
                message = $"施工点材料不足：{requiredStack.ItemId}";
                constructionSite.FailureReason = message;
                return false;
            }
        }

        foreach (ItemStack requiredStack in constructionSite.RequiredItems)
        {
            deliveredInventory.RemoveStack(requiredStack.ItemId, requiredStack.Count);
            _session.InventoryTransfers.Add(new InventoryTransfer
            {
                TransferId = Guid.NewGuid().ToString("N"),
                FromInventoryId = deliveredInventory.InventoryId,
                ToInventoryId = $"building_complete:{constructionSite.ConstructionSiteId}",
                ItemId = requiredStack.ItemId,
                Count = requiredStack.Count,
                Reason = "construction_material_consume",
                ExpeditionId = expeditionState.ExpeditionId
            });
        }

        buildingInstance = new BuildingInstance
        {
            BuildingInstanceId = UniqueBuildingInstanceId(expeditionState, buildingData.Id),
            BuildingId = buildingData.Id,
            Position = constructionSite.Position,
            Rotation = constructionSite.Rotation,
            MaxDurability = Math.Max(120, buildingData.Footprint.X * buildingData.Footprint.Y * 60),
            ConstructionProgress = 1f,
            PowerState = "isolated"
        };
        buildingInstance.Durability = buildingInstance.MaxDurability;
        CreateBuildingInventories(expeditionState, buildingData, buildingInstance);
        _session.BuildingInstances[buildingInstance.BuildingInstanceId] = buildingInstance;
        expeditionState.BuildingInstanceIds.Add(buildingInstance.BuildingInstanceId);
        constructionSite.State = "completed";
        constructionSite.ConstructionProgress = 1f;
        constructionSite.FailureReason = string.Empty;

        message = $"建筑完成：{buildingData.DisplayName}";
        GD.Print($"[建造] 建筑完成：{buildingInstance.BuildingInstanceId} {buildingData.Id}");
        return true;
    }

    public bool TryRepairBuilding(
        ExpeditionState expeditionState,
        string buildingInstanceId,
        string unitInstanceId,
        out RepairRecord repairRecord,
        out string message)
    {
        repairRecord = new RepairRecord
        {
            ExpeditionId = expeditionState.ExpeditionId,
            UnitInstanceId = unitInstanceId,
            TargetType = "building_friendly",
            TargetId = buildingInstanceId
        };

        if (!_session.BuildingInstances.TryGetValue(buildingInstanceId, out BuildingInstance? buildingInstance) ||
            !_registry.TryGetBuilding(buildingInstance.BuildingId, out BuildingData? buildingData) ||
            buildingData is null)
        {
            return FailRepair(expeditionState, repairRecord, $"找不到维修目标建筑：{buildingInstanceId}", out message);
        }

        int maxDurability = buildingInstance.MaxDurability > 0
            ? buildingInstance.MaxDurability
            : Math.Max(buildingInstance.Durability, Math.Max(120, buildingData.Footprint.X * buildingData.Footprint.Y * 60));
        if (buildingInstance.Durability >= maxDurability)
        {
            return FailRepair(expeditionState, repairRecord, "维修目标未受损。", out message);
        }

        if (!CanUnitRepair(expeditionState, unitInstanceId))
        {
            return FailRepair(expeditionState, repairRecord, "单位缺少维修指令或维修工具。", out message);
        }

        if (buildingInstance.PowerState is "offline" or "isolated" && buildingData.PowerConsumption > 0)
        {
            return FailRepair(expeditionState, repairRecord, "维修目标电力状态不允许维修。", out message);
        }

        int damage = maxDurability - buildingInstance.Durability;
        int metalCost = Math.Max(1, (int)Math.Ceiling(damage / 60f));
        List<ItemStack> repairCost = new()
        {
            new ItemStack { ItemId = "metal", Count = metalCost }
        };
        InventoryContainer repairSink = new()
        {
            InventoryId = $"repair_sink:{repairRecord.RepairRecordId}",
            OwnerType = "repair_sink",
            OwnerId = repairRecord.RepairRecordId
        };
        if (!TryPlanTransfers(
                expeditionState,
                repairCost,
                repairSink,
                string.Empty,
                out List<PlannedTransfer> plannedTransfers,
                out string transferMessage))
        {
            return FailRepair(expeditionState, repairRecord, transferMessage, out message);
        }

        if (!CommitTransfers(
            expeditionState,
            plannedTransfers,
            repairSink,
            "repair_sink",
            repairRecord.RepairRecordId,
            "building_repair_consume",
            out List<InventoryTransfer> consumedTransfers,
            out string commitMessage,
            createLogisticsOrders: false))
        {
            return FailRepair(expeditionState, repairRecord, commitMessage, out message);
        }

        repairRecord.DurabilityBefore = buildingInstance.Durability;
        buildingInstance.Durability = Math.Min(maxDurability, buildingInstance.Durability + metalCost * 60);
        repairRecord.DurabilityAfter = buildingInstance.Durability;
        repairRecord.Result = "completed";
        repairRecord.ConsumedItems.AddRange(repairCost);
        repairRecord.ConsumedTransferIds.AddRange(consumedTransfers.Select(transfer => transfer.TransferId));
        expeditionState.RepairRecords.Add(repairRecord);
        message = $"维修完成：{buildingData.DisplayName} {repairRecord.DurabilityBefore}->{repairRecord.DurabilityAfter}";
        GD.Print($"[建造] {message}");
        return true;
    }

    public bool TryDeliverRecipeInputs(
        ExpeditionState expeditionState,
        string buildingInstanceId,
        string recipeId,
        out string message)
    {
        if (!TryResolveProductionTarget(buildingInstanceId, recipeId, out BuildingInstance? buildingInstance, out RecipeData? recipeData, out message) ||
            buildingInstance is null ||
            recipeData is null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(buildingInstance.InputInventoryId) ||
            !_session.Inventories.TryGetValue(buildingInstance.InputInventoryId, out InventoryContainer? inputInventory))
        {
            message = $"建筑缺少输入库存：{buildingInstanceId}";
            return false;
        }

        if (!TryPlanTransfers(
                expeditionState,
                MissingStacks(recipeData.InputItems, inputInventory),
                inputInventory,
                inputInventory.InventoryId,
                out List<PlannedTransfer> plannedTransfers,
                out message))
        {
            return false;
        }

        if (!CommitTransfers(
            expeditionState,
            plannedTransfers,
            inputInventory,
            "building_input",
            buildingInstance.BuildingInstanceId,
            "production_input_delivery",
            out List<InventoryTransfer> _,
            out string commitMessage))
        {
            message = commitMessage;
            return false;
        }

        message = $"配方输入已送达：{recipeData.DisplayName}";
        return true;
    }

    public bool TryRunRecipe(ExpeditionState expeditionState, string buildingInstanceId, string recipeId, out ProductionJobState productionJob, out string message)
    {
        productionJob = new ProductionJobState
        {
            ExpeditionId = expeditionState.ExpeditionId,
            BuildingInstanceId = buildingInstanceId,
            RecipeId = recipeId
        };

        if (!TryResolveProductionTarget(buildingInstanceId, recipeId, out BuildingInstance? buildingInstance, out RecipeData? recipeData, out message) ||
            buildingInstance is null ||
            recipeData is null)
        {
            productionJob.State = "failed";
            productionJob.FailureReason = message;
            expeditionState.ProductionJobs.Add(productionJob);
            return false;
        }

        if (recipeData.PowerCost > 0 && buildingInstance.PowerState is not ("online" or "low_power"))
        {
            message = $"建筑电力不足：{buildingInstanceId}";
            productionJob.State = "waiting_power";
            productionJob.FailureReason = message;
            expeditionState.ProductionJobs.Add(productionJob);
            return false;
        }

        if (!_session.Inventories.TryGetValue(buildingInstance.InputInventoryId, out InventoryContainer? inputInventory) ||
            !_session.Inventories.TryGetValue(buildingInstance.OutputInventoryId, out InventoryContainer? outputInventory))
        {
            message = $"建筑输入或输出库存缺失：{buildingInstanceId}";
            productionJob.State = "failed";
            productionJob.FailureReason = message;
            expeditionState.ProductionJobs.Add(productionJob);
            return false;
        }

        productionJob.InputInventoryId = inputInventory.InventoryId;
        productionJob.OutputInventoryId = outputInventory.InventoryId;
        foreach (ItemStack input in recipeData.InputItems)
        {
            if (inputInventory.GetItemCount(input.ItemId) < input.Count)
            {
                message = $"配方输入不足：{input.ItemId}";
                productionJob.State = "waiting_input";
                productionJob.FailureReason = message;
                expeditionState.ProductionJobs.Add(productionJob);
                return false;
            }
        }

        InventoryContainer outputSimulation = CloneInventory(outputInventory, $"{outputInventory.InventoryId}_precheck");
        foreach (ItemStack output in recipeData.OutputItems)
        {
            InventoryTransferResult fitResult = outputSimulation.AddStack(output, _registry);
            if (!fitResult.IsSuccess)
            {
                message = $"建筑输出库存不足：{fitResult.Message}";
                productionJob.State = "output_blocked";
                productionJob.FailureReason = message;
                expeditionState.ProductionJobs.Add(productionJob);
                return false;
            }
        }

        foreach (ItemStack input in recipeData.InputItems)
        {
            inputInventory.RemoveStack(input.ItemId, input.Count);
            _session.InventoryTransfers.Add(new InventoryTransfer
            {
                TransferId = Guid.NewGuid().ToString("N"),
                FromInventoryId = inputInventory.InventoryId,
                ToInventoryId = $"recipe_input:{recipeData.Id}",
                ItemId = input.ItemId,
                Count = input.Count,
                Reason = "surface_production_input",
                ExpeditionId = expeditionState.ExpeditionId
            });
        }

        foreach (ItemStack output in recipeData.OutputItems)
        {
            InventoryTransferResult addResult = outputInventory.AddStack(output, _registry);
            if (addResult.IsSuccess && addResult.Transfer is not null)
            {
                addResult.Transfer.FromInventoryId = $"recipe_output:{recipeData.Id}";
                addResult.Transfer.Reason = "surface_production_output";
                addResult.Transfer.ExpeditionId = expeditionState.ExpeditionId;
                _session.InventoryTransfers.Add(addResult.Transfer);
            }
        }

        buildingInstance.ActiveRecipeId = recipeData.Id;
        buildingInstance.ProductionProgress = 1f;
        productionJob.State = "completed";
        productionJob.Progress = 1f;
        expeditionState.ProductionJobs.Add(productionJob);
        message = $"生产完成：{recipeData.DisplayName}";
        GD.Print($"[生产] {message}");
        return true;
    }

    public PowerNetworkState RecalculatePowerNetwork(ExpeditionState expeditionState)
    {
        int generation = 0;
        int consumption = 0;
        int storageCapacity = 0;
        foreach (string buildingInstanceId in expeditionState.BuildingInstanceIds)
        {
            if (!_session.BuildingInstances.TryGetValue(buildingInstanceId, out BuildingInstance? buildingInstance) ||
                !_registry.TryGetBuilding(buildingInstance.BuildingId, out BuildingData? buildingData) ||
                buildingData is null)
            {
                continue;
            }

            generation += buildingData.PowerGeneration;
            consumption += buildingData.PowerConsumption;
            if (buildingData.FunctionTags.Contains("power_storage"))
            {
                storageCapacity += buildingData.StorageCapacity;
            }
        }

        string networkId = $"power_network_{expeditionState.ExpeditionId}_main";
        PowerNetworkState network = expeditionState.PowerNetworkStates.FirstOrDefault(state => state.PowerNetworkId == networkId) ?? new PowerNetworkState
        {
            PowerNetworkId = networkId,
            ExpeditionId = expeditionState.ExpeditionId
        };
        if (!expeditionState.PowerNetworkStates.Contains(network))
        {
            expeditionState.PowerNetworkStates.Add(network);
        }

        network.TotalGeneration = generation;
        network.TotalConsumption = consumption;
        network.StorageCapacity = storageCapacity;
        network.State = generation <= 0 && consumption > 0
            ? "offline"
            : generation >= consumption
                ? "online"
                : "low_power";

        foreach (string buildingInstanceId in expeditionState.BuildingInstanceIds)
        {
            if (_session.BuildingInstances.TryGetValue(buildingInstanceId, out BuildingInstance? buildingInstance))
            {
                buildingInstance.PowerNetworkId = network.PowerNetworkId;
                buildingInstance.PowerState = network.State;
            }
        }

        GD.Print($"[电力] 网络 {network.State} 发电 {generation} 耗电 {consumption}");
        return network;
    }

    private bool TryResolveProductionTarget(
        string buildingInstanceId,
        string recipeId,
        out BuildingInstance? buildingInstance,
        out RecipeData? recipeData,
        out string message)
    {
        buildingInstance = null;
        recipeData = null;
        if (!_session.BuildingInstances.TryGetValue(buildingInstanceId, out BuildingInstance? foundBuilding) ||
            !_registry.TryGetBuilding(foundBuilding.BuildingId, out BuildingData? buildingData) ||
            buildingData is null)
        {
            message = $"找不到生产建筑：{buildingInstanceId}";
            return false;
        }

        if (!_registry.Recipes.TryGetValue(recipeId, out RecipeData? foundRecipe) || foundRecipe is null)
        {
            message = $"找不到配方：{recipeId}";
            return false;
        }

        bool tagMatched = foundRecipe.RequiredBuildingTags.Count == 0 ||
            foundRecipe.RequiredBuildingTags.Any(buildingData.FunctionTags.Contains);
        bool listedRecipe = buildingData.RecipeIds.Count == 0 || buildingData.RecipeIds.Contains(recipeId);
        if (!tagMatched || !listedRecipe)
        {
            message = $"建筑不能执行该配方：{foundBuilding.BuildingId} -> {recipeId}";
            return false;
        }

        if (!HasRequiredBlueprint(foundRecipe.RequiredBlueprintId))
        {
            message = $"缺少配方蓝图：{foundRecipe.RequiredBlueprintId}";
            return false;
        }

        buildingInstance = foundBuilding;
        recipeData = foundRecipe;
        message = string.Empty;
        return true;
    }

    private bool TryPlanTransfers(
        ExpeditionState expeditionState,
        IReadOnlyList<ItemStack> missingStacks,
        InventoryContainer targetInventory,
        string excludedInventoryId,
        out List<PlannedTransfer> plannedTransfers,
        out string message)
    {
        plannedTransfers = new List<PlannedTransfer>();
        if (missingStacks.Count == 0)
        {
            message = string.Empty;
            return true;
        }

        InventoryContainer targetSimulation = CloneInventory(targetInventory, $"{targetInventory.InventoryId}_precheck");
        foreach (ItemStack missingStack in missingStacks)
        {
            InventoryTransferResult capacityResult = targetSimulation.AddStack(missingStack, _registry);
            if (!capacityResult.IsSuccess)
            {
                message = capacityResult.Message;
                return false;
            }
        }

        foreach (ItemStack missingStack in missingStacks)
        {
            int remaining = missingStack.Count;
            foreach (InventoryContainer sourceInventory in SourceInventories(expeditionState, excludedInventoryId))
            {
                int alreadyPlanned = plannedTransfers
                    .Where(transfer => transfer.SourceInventory.InventoryId == sourceInventory.InventoryId && transfer.ItemId == missingStack.ItemId)
                    .Sum(transfer => transfer.Count);
                int available = sourceInventory.GetItemCount(missingStack.ItemId) - alreadyPlanned;
                if (available <= 0)
                {
                    continue;
                }

                int moved = Math.Min(available, remaining);
                plannedTransfers.Add(new PlannedTransfer(sourceInventory, missingStack.ItemId, moved));
                remaining -= moved;
                if (remaining <= 0)
                {
                    break;
                }
            }

            if (remaining > 0)
            {
                message = $"远征内具体库存缺少 {missingStack.ItemId} x{remaining}";
                return false;
            }
        }

        message = string.Empty;
        return true;
    }

    private bool CommitTransfers(
        ExpeditionState expeditionState,
        IEnumerable<PlannedTransfer> plannedTransfers,
        InventoryContainer targetInventory,
        string targetLocationType,
        string targetOwnerId,
        string reason,
        out List<InventoryTransfer> committedTransfers,
        out string message,
        bool createLogisticsOrders = true)
    {
        committedTransfers = new List<InventoryTransfer>();
        List<PlannedTransfer> committedPlans = new();
        foreach (PlannedTransfer plannedTransfer in plannedTransfers)
        {
            InventoryTransferResult result = plannedTransfer.SourceInventory.TransferTo(
                targetInventory,
                plannedTransfer.ItemId,
                plannedTransfer.Count,
                _registry,
                reason,
                expeditionState.ExpeditionId);
            if (!result.IsSuccess || result.Transfer is null)
            {
                RollbackCommittedTransfers(expeditionState, targetInventory, committedPlans, reason);
                message = $"预检后提交失败：{result.Message}";
                GD.PushError($"[物流] {message}");
                return false;
            }

            _session.InventoryTransfers.Add(result.Transfer);
            committedTransfers.Add(result.Transfer);
            committedPlans.Add(plannedTransfer);
            if (!createLogisticsOrders)
            {
                continue;
            }

            LogisticsOrderState order = new()
            {
                LogisticsOrderId = Guid.NewGuid().ToString("N"),
                ExpeditionId = expeditionState.ExpeditionId,
                ItemId = plannedTransfer.ItemId,
                Count = plannedTransfer.Count,
                SourceLocation = ItemLocation.FromInventory(plannedTransfer.SourceInventory.OwnerType, plannedTransfer.SourceInventory.InventoryId, plannedTransfer.SourceInventory.OwnerId),
                TargetLocation = ItemLocation.FromInventory(targetLocationType, targetInventory.InventoryId, targetOwnerId),
                State = "completed",
                CreatedBy = reason
            };
            _session.LogisticsOrders[order.LogisticsOrderId] = order;
            expeditionState.LogisticsOrderIds.Add(order.LogisticsOrderId);
        }

        message = string.Empty;
        return true;
    }

    private void RollbackCommittedTransfers(
        ExpeditionState expeditionState,
        InventoryContainer targetInventory,
        IReadOnlyList<PlannedTransfer> committedPlans,
        string reason)
    {
        for (int index = committedPlans.Count - 1; index >= 0; index--)
        {
            PlannedTransfer committedPlan = committedPlans[index];
            InventoryTransferResult rollback = targetInventory.TransferTo(
                committedPlan.SourceInventory,
                committedPlan.ItemId,
                committedPlan.Count,
                _registry,
                $"{reason}_rollback",
                expeditionState.ExpeditionId);
            if (rollback.IsSuccess && rollback.Transfer is not null)
            {
                _session.InventoryTransfers.Add(rollback.Transfer);
            }
        }
    }

    private IEnumerable<InventoryContainer> SourceInventories(ExpeditionState expeditionState, string excludedInventoryId)
    {
        foreach (string inventoryId in expeditionState.LocationInventoryIds.Distinct())
        {
            if (inventoryId == excludedInventoryId ||
                !_session.Inventories.TryGetValue(inventoryId, out InventoryContainer? inventory))
            {
                continue;
            }

            if (inventory.OwnerType is "unit_inventory" or "drop_pod_cargo" or "building_output" or "storage")
            {
                yield return inventory;
            }
        }
    }

    private void CreateBuildingInventories(ExpeditionState expeditionState, BuildingData buildingData, BuildingInstance buildingInstance)
    {
        if (buildingData.FunctionTags.Contains("production") || buildingData.FunctionTags.Contains("crafting") || buildingData.FunctionTags.Contains("rocket"))
        {
            InventoryContainer input = CreateBuildingInventory(expeditionState, buildingInstance.BuildingInstanceId, "building_input", Math.Max(8, buildingData.StorageCapacity));
            InventoryContainer output = CreateBuildingInventory(expeditionState, buildingInstance.BuildingInstanceId, "building_output", Math.Max(8, buildingData.StorageCapacity));
            buildingInstance.InputInventoryId = input.InventoryId;
            buildingInstance.OutputInventoryId = output.InventoryId;
        }
        else if (buildingData.FunctionTags.Contains("storage") || buildingData.FunctionTags.Contains("fluid_storage"))
        {
            InventoryContainer storage = CreateBuildingInventory(expeditionState, buildingInstance.BuildingInstanceId, "storage", Math.Max(8, buildingData.StorageCapacity));
            buildingInstance.OutputInventoryId = storage.InventoryId;
        }
        else if (buildingData.FunctionTags.Contains("repair"))
        {
            InventoryContainer input = CreateBuildingInventory(expeditionState, buildingInstance.BuildingInstanceId, "building_input", Math.Max(4, buildingData.StorageCapacity));
            buildingInstance.InputInventoryId = input.InventoryId;
        }
    }

    private InventoryContainer CreateBuildingInventory(ExpeditionState expeditionState, string buildingInstanceId, string ownerType, int slotLimit)
    {
        InventoryContainer inventory = new()
        {
            InventoryId = UniqueInventoryId($"{ownerType}_{expeditionState.ExpeditionId}_{buildingInstanceId}"),
            OwnerType = ownerType,
            OwnerId = buildingInstanceId,
            SlotLimit = slotLimit,
            WeightLimit = 500f
        };
        _session.Inventories[inventory.InventoryId] = inventory;
        expeditionState.LocationInventoryIds.Add(inventory.InventoryId);
        return inventory;
    }

    private bool CanUnitRepair(ExpeditionState expeditionState, string unitInstanceId)
    {
        if (!expeditionState.ActiveUnitInstanceIds.Contains(unitInstanceId) ||
            !_session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance) ||
            !_registry.TryGetUnit(unitInstance.UnitId, out UnitData? unitData) ||
            unitData is null ||
            !unitData.AvailableCommands.Contains("repair"))
        {
            return false;
        }

        foreach (InventoryContainer inventory in SourceInventories(expeditionState, string.Empty))
        {
            foreach (string itemInstanceId in inventory.ItemInstanceIds)
            {
                if (_session.ItemInstances.TryGetValue(itemInstanceId, out ItemInstance? itemInstance) &&
                    itemInstance.ItemId == "repair_tool_basic")
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool FailRepair(ExpeditionState expeditionState, RepairRecord repairRecord, string reason, out string message)
    {
        repairRecord.Result = "failed";
        repairRecord.FailureReason = reason;
        expeditionState.RepairRecords.Add(repairRecord);
        message = reason;
        GD.PushWarning($"[建造] 维修失败：{reason}");
        return false;
    }

    private bool HasRequiredBlueprint(string blueprintId)
    {
        return string.IsNullOrEmpty(blueprintId) ||
            _session.OrbitState.UnlockedBlueprints.Contains(blueprintId) ||
            _allowDebugBlueprintBypass;
    }

    private static List<ItemStack> MissingStacks(IEnumerable<ItemStack> requiredStacks, InventoryContainer targetInventory)
    {
        Dictionary<string, int> missing = new();
        foreach (ItemStack requiredStack in requiredStacks)
        {
            int missingCount = requiredStack.Count - targetInventory.GetItemCount(requiredStack.ItemId);
            if (missingCount <= 0)
            {
                continue;
            }

            missing.TryGetValue(requiredStack.ItemId, out int existingCount);
            missing[requiredStack.ItemId] = existingCount + missingCount;
        }

        return missing.Select(pair => new ItemStack
        {
            ItemId = pair.Key,
            Count = pair.Value
        }).ToList();
    }

    private static InventoryContainer CloneInventory(InventoryContainer source, string inventoryId)
    {
        InventoryContainer clone = new()
        {
            InventoryId = inventoryId,
            OwnerType = source.OwnerType,
            OwnerId = source.OwnerId,
            SlotLimit = source.SlotLimit,
            WeightLimit = source.WeightLimit
        };
        clone.AcceptedTags.AddRange(source.AcceptedTags);
        clone.BlockedTags.AddRange(source.BlockedTags);
        foreach (ItemStack stack in source.ItemStacks)
        {
            clone.ItemStacks.Add(new ItemStack
            {
                ItemId = stack.ItemId,
                Count = stack.Count
            });
        }

        clone.ItemInstanceIds.AddRange(source.ItemInstanceIds);
        return clone;
    }

    private string UniqueConstructionSiteId(ExpeditionState expeditionState, string buildingId)
    {
        string baseId = $"construction_site_{expeditionState.ExpeditionId}_{buildingId}";
        return UniqueId(baseId, _session.ConstructionSites.ContainsKey);
    }

    private string UniqueBuildingInstanceId(ExpeditionState expeditionState, string buildingId)
    {
        string baseId = $"building_{expeditionState.ExpeditionId}_{buildingId}";
        return UniqueId(baseId, _session.BuildingInstances.ContainsKey);
    }

    private string UniqueInventoryId(string baseId)
    {
        return UniqueId(baseId, _session.Inventories.ContainsKey);
    }

    private static string UniqueId(string baseId, Func<string, bool> exists)
    {
        if (!exists(baseId))
        {
            return baseId;
        }

        int index = 1;
        string candidate;
        do
        {
            candidate = $"{baseId}_{index}";
            index++;
        }
        while (exists(candidate));

        return candidate;
    }

    private static IEnumerable<ItemStack> CopyStacks(IEnumerable<ItemStack> stacks)
    {
        foreach (ItemStack stack in stacks)
        {
            yield return new ItemStack
            {
                ItemId = stack.ItemId,
                Count = stack.Count
            };
        }
    }

    private sealed record PlannedTransfer(InventoryContainer SourceInventory, string ItemId, int Count);
}
