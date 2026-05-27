using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGame;

public sealed class ExpeditionCreationService
{
    private const string DefaultCoordinateId = "ruined_array_184_-72";
    private const string DefaultDropPodId = "drop_pod_single_use";

    private readonly GameSession _session;
    private readonly DataRegistry _registry;

    public ExpeditionCreationService(GameSession session, DataRegistry registry)
    {
        _session = session;
        _registry = registry;
    }

    public DropConfigSession CreateDefaultDropConfig(int seed = 0)
    {
        DropConfigSession config = new()
        {
            SourceOrbitStateId = _session.OrbitState.OrbitStateId,
            SelectedCoordinateId = _session.OrbitState.KnownCoordinates.Count > 0 ? _session.OrbitState.KnownCoordinates[0] : DefaultCoordinateId,
            SelectedDropPodId = _registry.DropPods.ContainsKey(DefaultDropPodId) ? DefaultDropPodId : FirstDropPodId(),
            Seed = seed > 0 ? seed : CreateSeed()
        };
        config.TargetCoordinate = ParseCoordinate(config.SelectedCoordinateId);

        AddDefaultAwakenedUnit(config);
        AddDefaultMassUnits(config);
        AddDefaultStack(config, "metal", 60);
        AddDefaultStack(config, "energy_cell", 30);
        AddDefaultItemInstances(config);
        ValidateDropConfig(config);
        return config;
    }

    public bool ValidateDropConfig(DropConfigSession config)
    {
        config.ValidationErrors.Clear();
        config.ValidationWarnings.Clear();
        RecalculateConfig(config);

        if (_session.ActiveExpedition is not null)
        {
            config.ValidationErrors.Add("已有进行中的远征，必须先完成或恢复当前远征。");
        }

        if (string.IsNullOrWhiteSpace(config.SelectedCoordinateId) ||
            !_session.OrbitState.KnownCoordinates.Contains(config.SelectedCoordinateId))
        {
            config.ValidationErrors.Add($"坐标不在轨道已知坐标中：{config.SelectedCoordinateId}");
        }

        if (!_registry.TryGetDropPod(config.SelectedDropPodId, out DropPodData? pod) || pod is null)
        {
            config.ValidationErrors.Add($"找不到空投舱定义：{config.SelectedDropPodId}");
            return false;
        }

        if (config.UsedWeight > pod.WeightLimit)
        {
            config.ValidationErrors.Add($"空投计划超重：{config.UsedWeight:0.0}/{pod.WeightLimit:0.0}");
        }

        if (config.UsedUnitCapacity > pod.UnitCapacity)
        {
            config.ValidationErrors.Add($"空投单位容量不足：{config.UsedUnitCapacity}/{pod.UnitCapacity}");
        }

        ValidateSelectedUnits(config);
        ValidateSelectedCargo(config, pod);
        if (config.SelectedStackItems.Count == 0 && config.SelectedItemInstanceIds.Count == 0)
        {
            config.ValidationWarnings.Add("未选择任何携带物资。");
        }

        return config.ValidationErrors.Count == 0;
    }

    public bool TryCreateExpeditionStartPayload(
        DropConfigSession config,
        string fromScene,
        bool debugEnabled,
        out ScenePayload payload,
        out string message)
    {
        payload = new ScenePayload();
        if (!ValidateDropConfig(config))
        {
            message = string.Join("\n", config.ValidationErrors);
            return false;
        }

        if (!_registry.TryGetDropPod(config.SelectedDropPodId, out DropPodData? pod) || pod is null)
        {
            message = $"找不到空投舱定义：{config.SelectedDropPodId}";
            return false;
        }

        int seed = config.Seed > 0 ? config.Seed : CreateSeed();
        config.Seed = seed;
        string expeditionId = $"expedition_{seed}";
        string dropPlanId = $"drop_plan_{seed}";
        string dropCargoId = $"drop_pod_cargo_{seed}";
        string rocketCargoId = $"rocket_cargo_{seed}";
        DropPlan dropPlan = CreateDropPlan(config, pod, dropPlanId, seed);
        InventoryContainer dropCargo = CreateCargoInventory(dropCargoId, "drop_pod_cargo", pod.Id, pod.SlotLimit, pod.WeightLimit, pod);
        InventoryContainer rocketCargo = new()
        {
            InventoryId = rocketCargoId,
            OwnerType = "rocket_cargo",
            OwnerId = expeditionId,
            SlotLimit = 24,
            WeightLimit = 140f
        };

        _session.Inventories[dropCargo.InventoryId] = dropCargo;
        _session.Inventories[rocketCargo.InventoryId] = rocketCargo;

        if (!TransferDropPlanCargo(dropPlan, dropCargo, expeditionId, out message))
        {
            _session.Inventories.Remove(dropCargo.InventoryId);
            _session.Inventories.Remove(rocketCargo.InventoryId);
            return false;
        }

        ExpeditionState expeditionState = CreateExpeditionState(config, expeditionId, dropPlanId, dropCargoId, rocketCargoId, seed);
        _session.DropPlans[dropPlan.DropPlanId] = dropPlan;
        _session.ActiveExpedition = expeditionState;

        if (debugEnabled)
        {
            StageDebugReturnCargo(dropCargo, rocketCargo, expeditionState);
        }

        ExpeditionStartPayloadData expeditionData = new()
        {
            ExpeditionId = expeditionState.ExpeditionId,
            DropPlanId = dropPlan.DropPlanId,
            DropPodCargoInventoryId = dropCargo.InventoryId,
            Seed = seed,
            DropPosition = expeditionState.DropPosition
        };
        expeditionData.InitialUnits.AddRange(expeditionState.InitialUnits);
        expeditionData.InitialItems.AddRange(expeditionState.InitialItems);

        payload = new ScenePayload
        {
            FromScene = fromScene,
            TargetScene = SceneId.SurfaceExpedition,
            PayloadType = "expedition_start",
            ExpeditionStartData = expeditionData,
            DebugEnabled = debugEnabled,
            Seed = seed
        };

        message = $"远征已创建：{expeditionId}";
        GD.Print($"[空投] 远征创建完成：{expeditionId}，坐标 {config.TargetCoordinate.X},{config.TargetCoordinate.Y}");
        return true;
    }

    private void AddDefaultAwakenedUnit(DropConfigSession config)
    {
        foreach (string unitInstanceId in _session.OrbitState.AwakenedUnits)
        {
            if (_session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? instance) &&
                instance.IsAwakened &&
                instance.Durability > 0)
            {
                config.SelectedAwakenedUnitInstanceIds.Add(unitInstanceId);
                return;
            }
        }
    }

    private void AddDefaultMassUnits(DropConfigSession config)
    {
        int capacity = 0;
        if (_registry.TryGetDropPod(config.SelectedDropPodId, out DropPodData? pod) && pod is not null)
        {
            capacity = pod.UnitCapacity;
        }

        int remaining = Math.Max(0, capacity - config.SelectedAwakenedUnitInstanceIds.Count);
        foreach (string unitInstanceId in _session.OrbitState.AvailableMassUnitInstanceIds)
        {
            if (remaining <= 0)
            {
                return;
            }

            if (_session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? instance) &&
                !instance.IsAwakened &&
                instance.Durability > 0)
            {
                config.SelectedMassUnitInstanceIds.Add(unitInstanceId);
                remaining -= 1;
            }
        }
    }

    private void AddDefaultStack(DropConfigSession config, string itemId, int desiredCount)
    {
        InventoryContainer orbitInventory = EnsureInventory(_session.OrbitState.InventoryId, "orbit_inventory", _session.OrbitState.OrbitStateId, 64, 2000f);
        int available = orbitInventory.GetItemCount(itemId);
        int count = Math.Min(desiredCount, available);
        if (count > 0)
        {
            config.SelectedStackItems.Add(new ItemStack { ItemId = itemId, Count = count });
        }
    }

    private void AddDefaultItemInstances(DropConfigSession config)
    {
        InventoryContainer orbitInventory = EnsureInventory(_session.OrbitState.InventoryId, "orbit_inventory", _session.OrbitState.OrbitStateId, 64, 2000f);
        foreach (string itemInstanceId in new[] { "scanner_basic_001", "repair_tool_basic_001", "rifle_basic_001", "servo_mod_basic_001" })
        {
            if (orbitInventory.ItemInstanceIds.Contains(itemInstanceId))
            {
                config.SelectedItemInstanceIds.Add(itemInstanceId);
            }
        }
    }

    private void RecalculateConfig(DropConfigSession config)
    {
        config.TargetCoordinate = ParseCoordinate(config.SelectedCoordinateId);
        config.UsedWeight = 0f;
        config.UsedSlots = 0;
        config.WeightLimit = 0f;
        config.SlotLimit = 0;
        config.UnitCapacity = 0;
        config.UsedUnitCapacity =
            config.SelectedAwakenedUnitInstanceIds.Count +
            config.SelectedMassUnitInstanceIds.Count +
            config.SelectedUnitPlatformItems.Sum(item => item.Count);

        if (_registry.TryGetDropPod(config.SelectedDropPodId, out DropPodData? pod) && pod is not null)
        {
            config.WeightLimit = pod.WeightLimit;
            config.SlotLimit = pod.SlotLimit;
            config.UnitCapacity = pod.UnitCapacity;
        }

        foreach (ItemStack stack in config.SelectedStackItems)
        {
            config.UsedWeight += _registry.GetStackWeight(stack);
        }

        foreach (string itemInstanceId in config.SelectedItemInstanceIds)
        {
            if (_session.ItemInstances.TryGetValue(itemInstanceId, out ItemInstance? itemInstance) &&
                _registry.TryGetItem(itemInstance.ItemId, out ItemData? itemData) &&
                itemData is not null)
            {
                config.UsedWeight += itemData.UnitWeight;
            }
        }
    }

    private void ValidateSelectedUnits(DropConfigSession config)
    {
        HashSet<string> selectedUnits = new();
        foreach (string unitInstanceId in config.SelectedAwakenedUnitInstanceIds)
        {
            if (!selectedUnits.Add(unitInstanceId) ||
                !_session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance) ||
                !unitInstance.IsAwakened ||
                unitInstance.Durability <= 0 ||
                !_session.OrbitState.AwakenedUnits.Contains(unitInstanceId) ||
                !_registry.TryGetUnit(unitInstance.UnitId, out UnitData? unitData) ||
                unitData is null)
            {
                config.ValidationErrors.Add($"觉醒者实例不可用于空投：{unitInstanceId}");
            }
        }

        foreach (string unitInstanceId in config.SelectedMassUnitInstanceIds)
        {
            if (!selectedUnits.Add(unitInstanceId) ||
                !_session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance) ||
                unitInstance.IsAwakened ||
                unitInstance.Durability <= 0 ||
                !_session.OrbitState.AvailableMassUnitInstanceIds.Contains(unitInstanceId) ||
                !_registry.TryGetUnit(unitInstance.UnitId, out UnitData? unitData) ||
                unitData is null)
            {
                config.ValidationErrors.Add($"量产单位实例不可用于空投：{unitInstanceId}");
            }
        }
    }

    private void ValidateSelectedCargo(DropConfigSession config, DropPodData pod)
    {
        InventoryContainer orbitInventory = EnsureInventory(_session.OrbitState.InventoryId, "orbit_inventory", _session.OrbitState.OrbitStateId, 64, 2000f);
        InventoryContainer simulatedDropCargo = CreateCargoInventory("drop_plan_validation", "drop_pod_cargo", pod.Id, pod.SlotLimit, pod.WeightLimit, pod);

        Dictionary<string, int> requiredStacks = new();
        foreach (ItemStack stack in config.SelectedStackItems)
        {
            if (stack.Count <= 0)
            {
                config.ValidationErrors.Add($"携带物资数量必须大于 0：{stack.ItemId}");
                continue;
            }

            requiredStacks.TryGetValue(stack.ItemId, out int currentCount);
            requiredStacks[stack.ItemId] = currentCount + stack.Count;
        }

        foreach (KeyValuePair<string, int> requiredStack in requiredStacks)
        {
            if (orbitInventory.GetItemCount(requiredStack.Key) < requiredStack.Value)
            {
                config.ValidationErrors.Add($"轨道库存不足：{requiredStack.Key}");
                continue;
            }

            InventoryTransferResult fitResult = simulatedDropCargo.AddStack(
                new ItemStack { ItemId = requiredStack.Key, Count = requiredStack.Value },
                _registry);
            if (!fitResult.IsSuccess)
            {
                config.ValidationErrors.Add($"空投货舱容量校验失败：{fitResult.Message}");
            }
        }

        HashSet<string> selectedInstances = new();
        foreach (string itemInstanceId in config.SelectedItemInstanceIds)
        {
            if (!selectedInstances.Add(itemInstanceId))
            {
                config.ValidationErrors.Add($"实例道具重复选择：{itemInstanceId}");
                continue;
            }

            if (!orbitInventory.ItemInstanceIds.Contains(itemInstanceId) ||
                !_session.ItemInstances.TryGetValue(itemInstanceId, out ItemInstance? itemInstance) ||
                !_registry.TryGetItem(itemInstance.ItemId, out ItemData? itemData) ||
                itemData is null)
            {
                config.ValidationErrors.Add($"轨道库存缺少实例道具：{itemInstanceId}");
                continue;
            }

            InventoryTransferResult fitResult = simulatedDropCargo.AddItemInstance(itemInstanceId, _session.ItemInstances, _registry);
            if (!fitResult.IsSuccess)
            {
                config.ValidationErrors.Add($"空投实例容量校验失败：{fitResult.Message}");
            }
        }

        config.UsedSlots = simulatedDropCargo.ItemStacks.Count + simulatedDropCargo.ItemInstanceIds.Count;
    }

    private DropPlan CreateDropPlan(DropConfigSession config, DropPodData pod, string dropPlanId, int seed)
    {
        DropPlan plan = new()
        {
            DropPlanId = dropPlanId,
            DropPodId = pod.Id,
            TargetCoordinate = config.TargetCoordinate,
            Seed = seed,
            WeightLimit = pod.WeightLimit,
            UsedWeight = config.UsedWeight,
            CreatedFromOrbitStateId = _session.OrbitState.OrbitStateId
        };
        plan.SelectedAwakenedUnitInstanceIds.AddRange(config.SelectedAwakenedUnitInstanceIds);
        plan.SelectedMassUnitInstanceIds.AddRange(config.SelectedMassUnitInstanceIds);
        foreach (SelectedUnitPlatformItem platformItem in config.SelectedUnitPlatformItems)
        {
            plan.SelectedUnitPlatformItems.Add(new SelectedUnitPlatformItem
            {
                ItemId = platformItem.ItemId,
                Count = platformItem.Count,
                TargetUnitId = platformItem.TargetUnitId
            });
        }

        foreach (ItemStack stack in config.SelectedStackItems)
        {
            plan.SelectedStackItems.Add(new ItemStack { ItemId = stack.ItemId, Count = stack.Count });
        }

        plan.SelectedItemInstanceIds.AddRange(config.SelectedItemInstanceIds);
        return plan;
    }

    private ExpeditionState CreateExpeditionState(
        DropConfigSession config,
        string expeditionId,
        string dropPlanId,
        string dropCargoId,
        string rocketCargoId,
        int seed)
    {
        ExpeditionState expeditionState = new()
        {
            ExpeditionId = expeditionId,
            Seed = seed,
            DropPlanId = dropPlanId,
            DropPodCargoInventoryId = dropCargoId,
            DropPosition = config.TargetCoordinate,
            CreatedAtRunTime = Time.GetUnixTimeFromSystem()
        };
        expeditionState.LocationInventoryIds.Add(dropCargoId);
        expeditionState.LocationInventoryIds.Add(rocketCargoId);
        expeditionState.RocketState.CargoInventoryId = rocketCargoId;
        expeditionState.MapState.ExploredRegionIds.Add("drop_zone_ruined_array");
        expeditionState.MapState.DiscoveredMineralSourceIds.Add("nearby_scrap_field");
        expeditionState.MapState.DiscoveredRuinIds.Add("ruin_signal_cache");

        foreach (string unitInstanceId in config.SelectedAwakenedUnitInstanceIds)
        {
            AddUnitToExpedition(expeditionState, unitInstanceId);
        }

        foreach (string unitInstanceId in config.SelectedMassUnitInstanceIds)
        {
            AddUnitToExpedition(expeditionState, unitInstanceId);
        }

        foreach (ItemStack stack in config.SelectedStackItems)
        {
            expeditionState.InitialItems.Add(new ItemStack { ItemId = stack.ItemId, Count = stack.Count });
        }

        return expeditionState;
    }

    private void AddUnitToExpedition(ExpeditionState expeditionState, string unitInstanceId)
    {
        if (!_session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance))
        {
            return;
        }

        expeditionState.ActiveUnitInstanceIds.Add(unitInstanceId);
        expeditionState.InitialUnits.Add(new UnitStack
        {
            UnitId = unitInstance.UnitId,
            Count = 1,
            ConfigId = unitInstanceId
        });
    }

    private bool TransferDropPlanCargo(DropPlan plan, InventoryContainer dropCargo, string expeditionId, out string message)
    {
        InventoryContainer orbitInventory = EnsureInventory(_session.OrbitState.InventoryId, "orbit_inventory", _session.OrbitState.OrbitStateId, 64, 2000f);
        List<ItemStack> movedStacks = new();
        List<string> movedInstances = new();

        foreach (ItemStack stack in plan.SelectedStackItems)
        {
            InventoryTransferResult result = orbitInventory.TransferTo(dropCargo, stack.ItemId, stack.Count, _registry, "drop_plan_load", expeditionId);
            if (result.IsSuccess && result.Transfer is not null)
            {
                _session.InventoryTransfers.Add(result.Transfer);
                plan.RelatedTransferIds.Add(result.Transfer.TransferId);
                movedStacks.Add(new ItemStack { ItemId = stack.ItemId, Count = stack.Count });
                continue;
            }

            RollbackDropCargo(orbitInventory, dropCargo, movedStacks, movedInstances);
            message = $"空投装载失败：{result.Message}";
            GD.PushWarning($"[库存] {message}");
            return false;
        }

        foreach (string itemInstanceId in plan.SelectedItemInstanceIds)
        {
            InventoryTransferResult result = orbitInventory.TransferItemInstanceTo(dropCargo, itemInstanceId, _session.ItemInstances, _registry, "drop_plan_load", expeditionId);
            if (result.IsSuccess && result.Transfer is not null)
            {
                _session.InventoryTransfers.Add(result.Transfer);
                plan.RelatedTransferIds.Add(result.Transfer.TransferId);
                movedInstances.Add(itemInstanceId);
                continue;
            }

            RollbackDropCargo(orbitInventory, dropCargo, movedStacks, movedInstances);
            message = $"空投实例装载失败：{result.Message}";
            GD.PushWarning($"[库存] {message}");
            return false;
        }

        message = string.Empty;
        return true;
    }

    private void RollbackDropCargo(
        InventoryContainer orbitInventory,
        InventoryContainer dropCargo,
        IReadOnlyList<ItemStack> movedStacks,
        IReadOnlyList<string> movedInstances)
    {
        for (int index = movedInstances.Count - 1; index >= 0; index--)
        {
            dropCargo.TransferItemInstanceTo(orbitInventory, movedInstances[index], _session.ItemInstances, _registry, "drop_plan_rollback");
        }

        for (int index = movedStacks.Count - 1; index >= 0; index--)
        {
            ItemStack stack = movedStacks[index];
            dropCargo.TransferTo(orbitInventory, stack.ItemId, stack.Count, _registry, "drop_plan_rollback");
        }
    }

    private void StageDebugReturnCargo(InventoryContainer dropCargo, InventoryContainer rocketCargo, ExpeditionState expeditionState)
    {
        expeditionState.RocketState.IsConstructed = true;
        expeditionState.RocketState.ConstructionProgress = 1f;
        expeditionState.RocketState.IsReadyToReturn = true;
        expeditionState.RocketState.LaunchConfirmed = true;
        TransferToRocket(dropCargo, rocketCargo, "metal", 25, expeditionState);
        TransferToRocket(dropCargo, rocketCargo, "energy_cell", 10, expeditionState);
        rocketCargo.AddStack(new ItemStack { ItemId = "scrap", Count = 18 }, _registry);
        rocketCargo.AddStack(new ItemStack { ItemId = "clean_data", Count = 2 }, _registry);
        TransferInstanceToRocket(dropCargo, rocketCargo, "scanner_basic_001", expeditionState);
        expeditionState.RocketState.CargoItems.Clear();
        expeditionState.RocketState.CargoItems.AddRange(rocketCargo.ItemStacks);
        expeditionState.RocketState.ReturningItemInstanceIds.Clear();
        expeditionState.RocketState.ReturningItemInstanceIds.AddRange(rocketCargo.ItemInstanceIds);
        foreach (string unitInstanceId in expeditionState.ActiveUnitInstanceIds)
        {
            if (_session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance) && unitInstance.IsAwakened)
            {
                expeditionState.RocketState.ReturningAwakenedUnitIds.Add(unitInstanceId);
            }
        }

        expeditionState.RocketState.ReturningChipIds.Add("ai_chip_basic");
        expeditionState.RocketState.ReturningBlueprintIds.Add("blueprint_rocket_pad_basic");
        expeditionState.DiscoveredIds.Add("blueprint_rocket_pad_basic");
        expeditionState.DiscoveredIds.Add("ruin_signal_cache");
        expeditionState.MapState.LeftAssetIds.Add("left_storage_cache_ruined_array");
        expeditionState.RocketState.IsOverloaded = rocketCargo.GetTotalWeight(_registry, _session.ItemInstances) > expeditionState.RocketState.CargoWeightLimit;
    }

    private void TransferToRocket(InventoryContainer from, InventoryContainer to, string itemId, int count, ExpeditionState expeditionState)
    {
        InventoryTransferResult result = from.TransferTo(to, itemId, count, _registry, "rocket_cargo_load", expeditionState.ExpeditionId);
        if (result.IsSuccess && result.Transfer is not null)
        {
            _session.InventoryTransfers.Add(result.Transfer);
        }
        else
        {
            GD.PushWarning($"[库存] 火箭装载失败：{result.Message}");
        }
    }

    private void TransferInstanceToRocket(InventoryContainer from, InventoryContainer to, string itemInstanceId, ExpeditionState expeditionState)
    {
        InventoryTransferResult result = from.TransferItemInstanceTo(to, itemInstanceId, _session.ItemInstances, _registry, "rocket_cargo_load", expeditionState.ExpeditionId);
        if (result.IsSuccess && result.Transfer is not null)
        {
            _session.InventoryTransfers.Add(result.Transfer);
        }
        else
        {
            GD.PushWarning($"[库存] 火箭实例装载失败：{result.Message}");
        }
    }

    private InventoryContainer EnsureInventory(string inventoryId, string ownerType, string ownerId, int slotLimit, float weightLimit)
    {
        if (_session.Inventories.TryGetValue(inventoryId, out InventoryContainer? inventory))
        {
            return inventory;
        }

        InventoryContainer created = new()
        {
            InventoryId = inventoryId,
            OwnerType = ownerType,
            OwnerId = ownerId,
            SlotLimit = slotLimit,
            WeightLimit = weightLimit
        };
        _session.Inventories[inventoryId] = created;
        return created;
    }

    private static InventoryContainer CreateCargoInventory(
        string inventoryId,
        string ownerType,
        string ownerId,
        int slotLimit,
        float weightLimit,
        DropPodData pod)
    {
        InventoryContainer inventory = new()
        {
            InventoryId = inventoryId,
            OwnerType = ownerType,
            OwnerId = ownerId,
            SlotLimit = slotLimit,
            WeightLimit = weightLimit
        };
        inventory.AcceptedTags.AddRange(pod.AcceptedTags);
        inventory.BlockedTags.AddRange(pod.BlockedTags);
        return inventory;
    }

    private string FirstDropPodId()
    {
        foreach (string dropPodId in _registry.DropPods.Keys)
        {
            return dropPodId;
        }

        return string.Empty;
    }

    private static int CreateSeed()
    {
        ulong ticks = Time.GetTicksMsec();
        return 460001 + (int)(ticks % 1000000UL);
    }

    private static Vector2I ParseCoordinate(string coordinateId)
    {
        string[] parts = coordinateId.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 &&
            int.TryParse(parts[^2], out int x) &&
            int.TryParse(parts[^1], out int y))
        {
            return new Vector2I(x, y);
        }

        return new Vector2I(184, -72);
    }
}
