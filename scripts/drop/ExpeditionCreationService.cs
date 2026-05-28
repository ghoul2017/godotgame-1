using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGame;

public sealed class ExpeditionCreationService
{
    private const string DefaultCoordinateId = "coord_scrap_plain_01";
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
            SelectedCoordinateId = FirstKnownCoordinateId(),
            SelectedDropPodId = FirstUnlockedDropPodId(),
            Seed = seed > 0 ? seed : CreateSeed()
        };

        AddDefaultAwakenedUnit(config);
        AddDefaultMassUnits(config);
        AddDefaultStack(config, "metal", 20);
        AddDefaultStack(config, "silicon", 8);
        AddDefaultStack(config, "scrap", 20);
        AddDefaultStack(config, "energy_cell", 6);
        AddDefaultStack(config, "electronic_parts", 8);
        AddDefaultItemInstances(config);
        ValidateDropConfig(config);
        return config;
    }

    public bool IsDropPodUnlocked(DropPodData pod)
    {
        bool hasBlueprint = string.IsNullOrEmpty(pod.RequiresBlueprintId) ||
            _session.OrbitState.UnlockedBlueprints.Contains(pod.RequiresBlueprintId);
        bool hasProtocols = pod.RequiresProtocolIds.All(protocolId => _session.OrbitState.UnlockedProtocols.Contains(protocolId));
        return hasBlueprint && hasProtocols;
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

        if (config.SourceOrbitStateId != _session.OrbitState.OrbitStateId)
        {
            config.ValidationErrors.Add("空投配置来源与当前轨道状态不一致。");
        }

        if (string.IsNullOrWhiteSpace(config.SelectedCoordinateId) ||
            !_session.OrbitState.KnownCoordinates.Contains(config.SelectedCoordinateId) ||
            !_registry.TryGetKnownCoordinate(config.SelectedCoordinateId, out KnownCoordinate? coordinate) ||
            coordinate is null)
        {
            config.ValidationErrors.Add($"坐标不在轨道已知坐标中：{config.SelectedCoordinateId}");
        }

        if (!_registry.TryGetDropPod(config.SelectedDropPodId, out DropPodData? pod) || pod is null)
        {
            config.ValidationErrors.Add($"找不到空投舱定义：{config.SelectedDropPodId}");
            return false;
        }

        if (!IsDropPodUnlocked(pod))
        {
            config.ValidationErrors.Add($"空投舱未解锁：{pod.DisplayName}");
        }

        if (config.UsedWeight > pod.WeightLimit)
        {
            config.ValidationErrors.Add($"空投计划超重：{config.UsedWeight:0.0}/{pod.WeightLimit:0.0}");
        }

        if (config.UsedSlots > pod.SlotLimit)
        {
            config.ValidationErrors.Add($"空投货舱格位不足：{config.UsedSlots}/{pod.SlotLimit}");
        }

        if (config.UsedUnitCapacity > pod.UnitCapacity)
        {
            config.ValidationErrors.Add($"空投单位容量不足：{config.UsedUnitCapacity}/{pod.UnitCapacity}");
        }

        ValidateSelectedUnits(config);
        ValidateSelectedCargo(config, pod);
        AddWarnings(config);
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
        string expeditionId = UniqueId("expedition", seed, id => _session.ActiveExpedition?.ExpeditionId == id);
        string dropPlanId = UniqueId("drop_plan", seed, _session.DropPlans.ContainsKey);
        string dropCargoId = UniqueId("drop_pod_cargo", seed, _session.Inventories.ContainsKey);

        DropPlan dropPlan = CreateDropPlan(config, pod, dropPlanId, seed);
        InventoryContainer dropCargo = CreateCargoInventory(dropCargoId, "drop_pod_cargo", pod.Id, pod.SlotLimit, pod.WeightLimit, pod);
        _session.Inventories[dropCargo.InventoryId] = dropCargo;

        List<string> createdUnitInstanceIds = new();
        List<InventoryTransfer> transfers = new();
        if (!ApplyDropTransaction(dropPlan, dropCargo, expeditionId, seed, createdUnitInstanceIds, transfers, out message))
        {
            _session.Inventories.Remove(dropCargo.InventoryId);
            return false;
        }

        dropPlan.CreatedUnitInstanceIds.AddRange(createdUnitInstanceIds);
        ExpeditionState expeditionState = CreateExpeditionState(config, expeditionId, dropPlanId, dropCargoId, seed, createdUnitInstanceIds);
        _session.InventoryTransfers.AddRange(transfers);
        _session.DropPlans[dropPlan.DropPlanId] = dropPlan;
        _session.ActiveExpedition = expeditionState;

        ExpeditionStartPayloadData expeditionData = new()
        {
            ExpeditionId = expeditionState.ExpeditionId,
            DropPlanId = dropPlan.DropPlanId,
            TargetCoordinateId = expeditionState.TargetCoordinateId,
            DropPodCargoInventoryId = dropCargo.InventoryId,
            Seed = seed,
            DropPosition = expeditionState.DropPosition
        };
        expeditionData.ActiveUnitInstanceIds.AddRange(expeditionState.ActiveUnitInstanceIds);
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
        string debugPrefix = debugEnabled ? "[调试]" : "[空投]";
        GD.Print($"{debugPrefix} 远征创建完成：{expeditionId}，坐标 {config.SelectedCoordinateId}，种子 {seed}");
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
        if (!_registry.TryGetDropPod(config.SelectedDropPodId, out DropPodData? pod) || pod is null)
        {
            return;
        }

        int usedCapacity = config.SelectedAwakenedUnitInstanceIds.Sum(GetUnitCapacityCost);
        foreach (string unitInstanceId in _session.OrbitState.AvailableMassUnitInstanceIds)
        {
            if (!_session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? instance) ||
                instance.IsAwakened ||
                instance.Durability <= 0 ||
                !_registry.TryGetUnit(instance.UnitId, out UnitData? unitData) ||
                unitData is null)
            {
                continue;
            }

            int cost = GetUnitCapacityCost(unitData, instance.UnitId);
            if (usedCapacity + cost > pod.UnitCapacity)
            {
                continue;
            }

            config.SelectedMassUnitInstanceIds.Add(unitInstanceId);
            usedCapacity += cost;
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
        foreach (string itemInstanceId in new[] { "repair_tool_basic_001", "scanner_basic_001", "rifle_basic_001", "ai_chip_basic_001" })
        {
            if (orbitInventory.ItemInstanceIds.Contains(itemInstanceId))
            {
                config.SelectedItemInstanceIds.Add(itemInstanceId);
            }
        }
    }

    private void RecalculateConfig(DropConfigSession config)
    {
        config.TargetCoordinate = ResolveCoordinate(config.SelectedCoordinateId);
        config.UsedWeight = 0f;
        config.UsedSlots = 0;
        config.WeightLimit = 0f;
        config.SlotLimit = 0;
        config.UnitCapacity = 0;
        config.UsedUnitCapacity = 0;

        if (_registry.TryGetDropPod(config.SelectedDropPodId, out DropPodData? pod) && pod is not null)
        {
            config.WeightLimit = pod.WeightLimit;
            config.SlotLimit = pod.SlotLimit;
            config.UnitCapacity = pod.UnitCapacity;
        }

        foreach (string unitInstanceId in config.SelectedAwakenedUnitInstanceIds.Concat(config.SelectedMassUnitInstanceIds))
        {
            if (_session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? instance) &&
                _registry.TryGetUnit(instance.UnitId, out UnitData? unitData) &&
                unitData is not null)
            {
                config.UsedUnitCapacity += GetUnitCapacityCost(unitData, instance.UnitId);
                config.UsedWeight += GetUnitDropWeight(instance.UnitId);
            }
        }

        foreach (SelectedUnitPlatformItem platformItem in config.SelectedUnitPlatformItems)
        {
            if (_registry.TryGetItem(platformItem.ItemId, out ItemData? itemData) && itemData is not null)
            {
                config.UsedWeight += itemData.UnitWeight * platformItem.Count;
            }

            if (_registry.TryGetUnit(platformItem.TargetUnitId, out UnitData? unitData) && unitData is not null)
            {
                config.UsedUnitCapacity += GetUnitCapacityCost(unitData, platformItem.TargetUnitId) * platformItem.Count;
            }
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
                unitInstance.IsLocked ||
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
                unitInstance.IsLocked ||
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

        ValidateStackCargo(config, orbitInventory, simulatedDropCargo);
        ValidatePlatformCargo(config, orbitInventory, simulatedDropCargo);
        ValidateInstanceCargo(config, orbitInventory, simulatedDropCargo);
        config.UsedSlots = simulatedDropCargo.ItemStacks.Count + simulatedDropCargo.ItemInstanceIds.Count;
    }

    private void ValidateStackCargo(DropConfigSession config, InventoryContainer orbitInventory, InventoryContainer simulatedDropCargo)
    {
        Dictionary<string, int> requiredStacks = new();
        foreach (ItemStack stack in config.SelectedStackItems)
        {
            if (stack.Count <= 0)
            {
                config.ValidationErrors.Add($"携带物资数量必须大于 0：{stack.ItemId}");
                continue;
            }

            if (!_registry.TryGetItem(stack.ItemId, out ItemData? itemData) || itemData is null)
            {
                config.ValidationErrors.Add($"找不到携带物资定义：{stack.ItemId}");
                continue;
            }

            if (itemData.RequiresInstance || itemData.Category == "unit_platform")
            {
                config.ValidationErrors.Add($"该道具不能作为普通堆叠物资携带：{stack.ItemId}");
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
    }

    private void ValidatePlatformCargo(DropConfigSession config, InventoryContainer orbitInventory, InventoryContainer simulatedDropCargo)
    {
        Dictionary<string, int> requiredPlatforms = new();
        foreach (SelectedUnitPlatformItem platformItem in config.SelectedUnitPlatformItems)
        {
            if (platformItem.Count <= 0)
            {
                config.ValidationErrors.Add($"单位平台数量必须大于 0：{platformItem.ItemId}");
                continue;
            }

            if (!_registry.TryGetItem(platformItem.ItemId, out ItemData? itemData) ||
                itemData is null ||
                itemData.Category != "unit_platform")
            {
                config.ValidationErrors.Add($"单位平台道具定义无效：{platformItem.ItemId}");
                continue;
            }

            if (!string.Equals(itemData.TargetUnitId, platformItem.TargetUnitId, StringComparison.Ordinal))
            {
                config.ValidationErrors.Add($"单位平台目标单位不匹配：{platformItem.ItemId}");
                continue;
            }

            if (!_registry.TryGetUnit(platformItem.TargetUnitId, out UnitData? unitData) || unitData is null)
            {
                config.ValidationErrors.Add($"单位平台目标单位缺失：{platformItem.TargetUnitId}");
                continue;
            }

            requiredPlatforms.TryGetValue(platformItem.ItemId, out int currentCount);
            requiredPlatforms[platformItem.ItemId] = currentCount + platformItem.Count;
        }

        foreach (KeyValuePair<string, int> requiredPlatform in requiredPlatforms)
        {
            if (orbitInventory.GetItemCount(requiredPlatform.Key) < requiredPlatform.Value)
            {
                config.ValidationErrors.Add($"轨道库存缺少单位平台：{requiredPlatform.Key}");
                continue;
            }

            InventoryTransferResult fitResult = simulatedDropCargo.AddStack(
                new ItemStack { ItemId = requiredPlatform.Key, Count = requiredPlatform.Value },
                _registry);
            if (!fitResult.IsSuccess)
            {
                config.ValidationErrors.Add($"单位平台装载容量校验失败：{fitResult.Message}");
            }
        }
    }

    private void ValidateInstanceCargo(DropConfigSession config, InventoryContainer orbitInventory, InventoryContainer simulatedDropCargo)
    {
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

            if (!string.IsNullOrEmpty(itemInstance.BoundUnitInstanceId))
            {
                config.ValidationErrors.Add($"实例道具已绑定，不能装入空投：{itemInstanceId}");
                continue;
            }

            InventoryTransferResult fitResult = simulatedDropCargo.AddItemInstance(itemInstanceId, _session.ItemInstances, _registry);
            if (!fitResult.IsSuccess)
            {
                config.ValidationErrors.Add($"空投实例容量校验失败：{fitResult.Message}");
            }
        }
    }

    private void AddWarnings(DropConfigSession config)
    {
        bool hasRepairTool = ContainsSelectedItem(config, "repair_tool_basic");
        bool hasScanner = ContainsSelectedItem(config, "scanner_basic");
        bool hasEnergy = config.SelectedStackItems.Any(stack => stack.ItemId == "energy_cell" && stack.Count > 0);
        bool hasMassUnit = config.SelectedMassUnitInstanceIds.Count > 0 || config.SelectedUnitPlatformItems.Count > 0;
        if (!hasRepairTool)
        {
            config.ValidationWarnings.Add("未携带维修工具。");
        }

        if (!hasScanner)
        {
            config.ValidationWarnings.Add("未携带扫描器。");
        }

        if (!hasEnergy)
        {
            config.ValidationWarnings.Add("未携带基础能源块。");
        }

        if (!hasMassUnit)
        {
            config.ValidationWarnings.Add("未携带量产单位。");
        }

        if (config.WeightLimit > 0f && config.WeightLimit - config.UsedWeight < 10f)
        {
            config.ValidationWarnings.Add("剩余载重很低。");
        }
    }

    private bool ContainsSelectedItem(DropConfigSession config, string itemId)
    {
        if (config.SelectedStackItems.Any(stack => stack.ItemId == itemId && stack.Count > 0))
        {
            return true;
        }

        foreach (string itemInstanceId in config.SelectedItemInstanceIds)
        {
            if (_session.ItemInstances.TryGetValue(itemInstanceId, out ItemInstance? instance) && instance.ItemId == itemId)
            {
                return true;
            }
        }

        return false;
    }

    private DropPlan CreateDropPlan(DropConfigSession config, DropPodData pod, string dropPlanId, int seed)
    {
        DropPlan plan = new()
        {
            DropPlanId = dropPlanId,
            DropPodId = pod.Id,
            TargetCoordinateId = config.SelectedCoordinateId,
            TargetCoordinate = config.TargetCoordinate,
            Seed = seed,
            WeightLimit = pod.WeightLimit,
            UsedWeight = config.UsedWeight,
            SlotLimit = pod.SlotLimit,
            UsedSlots = config.UsedSlots,
            UnitCapacity = pod.UnitCapacity,
            UsedUnitCapacity = config.UsedUnitCapacity,
            CreatedFromOrbitStateId = _session.OrbitState.OrbitStateId,
            CreatedAt = Time.GetUnixTimeFromSystem()
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
            plan.SelectedStackItems.Add(CopyStack(stack));
        }

        plan.SelectedItemInstanceIds.AddRange(config.SelectedItemInstanceIds);
        return plan;
    }

    private ExpeditionState CreateExpeditionState(
        DropConfigSession config,
        string expeditionId,
        string dropPlanId,
        string dropCargoId,
        int seed,
        IReadOnlyList<string> createdUnitInstanceIds)
    {
        ExpeditionState expeditionState = new()
        {
            ExpeditionId = expeditionId,
            Seed = seed,
            DropPlanId = dropPlanId,
            TargetCoordinateId = config.SelectedCoordinateId,
            DropPodCargoInventoryId = dropCargoId,
            DropPosition = config.TargetCoordinate,
            CreatedAtRunTime = Time.GetUnixTimeFromSystem()
        };
        expeditionState.LocationInventoryIds.Add(dropCargoId);
        expeditionState.MapState.TargetCoordinateId = config.SelectedCoordinateId;
        expeditionState.MapState.Seed = seed;
        expeditionState.MapState.DropPosition = config.TargetCoordinate;

        foreach (string unitInstanceId in config.SelectedAwakenedUnitInstanceIds)
        {
            AddUnitToExpedition(expeditionState, unitInstanceId);
        }

        foreach (string unitInstanceId in config.SelectedMassUnitInstanceIds)
        {
            AddUnitToExpedition(expeditionState, unitInstanceId);
        }

        foreach (string unitInstanceId in createdUnitInstanceIds)
        {
            AddUnitToExpedition(expeditionState, unitInstanceId);
        }

        foreach (ItemStack stack in config.SelectedStackItems)
        {
            expeditionState.InitialItems.Add(CopyStack(stack));
        }

        return expeditionState;
    }

    private void AddUnitToExpedition(ExpeditionState expeditionState, string unitInstanceId)
    {
        if (!_session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance))
        {
            return;
        }

        unitInstance.LockedByExpeditionId = expeditionState.ExpeditionId;
        unitInstance.CurrentCommand = $"expedition:{expeditionState.ExpeditionId}";
        EnsureUnitInventoryForExpedition(expeditionState, unitInstance);
        expeditionState.ActiveUnitInstanceIds.Add(unitInstanceId);
        expeditionState.InitialUnits.Add(new UnitStack
        {
            UnitId = unitInstance.UnitId,
            Count = 1,
            ConfigId = unitInstanceId
        });
    }

    private bool ApplyDropTransaction(
        DropPlan plan,
        InventoryContainer dropCargo,
        string expeditionId,
        int seed,
        List<string> createdUnitInstanceIds,
        List<InventoryTransfer> pendingTransfers,
        out string message)
    {
        InventoryContainer orbitInventory = EnsureInventory(_session.OrbitState.InventoryId, "orbit_inventory", _session.OrbitState.OrbitStateId, 64, 2000f);
        List<ItemStack> movedStacks = new();
        List<string> movedInstances = new();
        List<ItemStack> consumedPlatforms = new();

        foreach (ItemStack stack in plan.SelectedStackItems)
        {
            InventoryTransferResult result = orbitInventory.TransferTo(dropCargo, stack.ItemId, stack.Count, _registry, "drop_plan_load", expeditionId);
            if (result.IsSuccess && result.Transfer is not null)
            {
                pendingTransfers.Add(result.Transfer);
                plan.RelatedTransferIds.Add(result.Transfer.TransferId);
                movedStacks.Add(CopyStack(stack));
                continue;
            }

            RollbackDropTransaction(orbitInventory, dropCargo, movedStacks, movedInstances, consumedPlatforms, createdUnitInstanceIds);
            message = $"空投装载失败：{result.Message}";
            GD.PushWarning($"[库存] {message}");
            return false;
        }

        foreach (string itemInstanceId in plan.SelectedItemInstanceIds)
        {
            InventoryTransferResult result = orbitInventory.TransferItemInstanceTo(dropCargo, itemInstanceId, _session.ItemInstances, _registry, "drop_plan_load", expeditionId);
            if (result.IsSuccess && result.Transfer is not null)
            {
                pendingTransfers.Add(result.Transfer);
                plan.RelatedTransferIds.Add(result.Transfer.TransferId);
                movedInstances.Add(itemInstanceId);
                continue;
            }

            RollbackDropTransaction(orbitInventory, dropCargo, movedStacks, movedInstances, consumedPlatforms, createdUnitInstanceIds);
            message = $"空投实例装载失败：{result.Message}";
            GD.PushWarning($"[库存] {message}");
            return false;
        }

        foreach (SelectedUnitPlatformItem platformItem in plan.SelectedUnitPlatformItems)
        {
            if (!ConsumePlatformAndCreateUnits(orbitInventory, platformItem, expeditionId, seed, createdUnitInstanceIds, pendingTransfers, plan, out message))
            {
                RollbackDropTransaction(orbitInventory, dropCargo, movedStacks, movedInstances, consumedPlatforms, createdUnitInstanceIds);
                GD.PushWarning($"[库存] {message}");
                return false;
            }

            consumedPlatforms.Add(new ItemStack { ItemId = platformItem.ItemId, Count = platformItem.Count });
        }

        message = string.Empty;
        return true;
    }

    private bool ConsumePlatformAndCreateUnits(
        InventoryContainer orbitInventory,
        SelectedUnitPlatformItem platformItem,
        string expeditionId,
        int seed,
        List<string> createdUnitInstanceIds,
        List<InventoryTransfer> pendingTransfers,
        DropPlan plan,
        out string message)
    {
        if (!_registry.TryGetUnit(platformItem.TargetUnitId, out UnitData? unitData) || unitData is null)
        {
            message = $"单位平台目标单位缺失：{platformItem.TargetUnitId}";
            return false;
        }

        if (!orbitInventory.RemoveStack(platformItem.ItemId, platformItem.Count))
        {
            message = $"轨道库存缺少单位平台：{platformItem.ItemId}";
            return false;
        }

        InventoryTransfer transfer = new()
        {
            TransferId = Guid.NewGuid().ToString("N"),
            FromInventoryId = orbitInventory.InventoryId,
            ToInventoryId = $"unit_creation:{expeditionId}",
            ItemId = platformItem.ItemId,
            Count = platformItem.Count,
            Reason = "drop_platform_assemble",
            ExpeditionId = expeditionId
        };
        pendingTransfers.Add(transfer);
        plan.RelatedTransferIds.Add(transfer.TransferId);

        for (int index = 0; index < platformItem.Count; index++)
        {
            string unitInstanceId = UniqueId($"unit_{platformItem.TargetUnitId}", seed + index, _session.UnitInstances.ContainsKey);
            UnitInstance unitInstance = new()
            {
                UnitInstanceId = unitInstanceId,
                UnitId = platformItem.TargetUnitId,
                DisplayNameOverride = $"{unitData.DisplayName} {seed % 10000:0000}-{index + 1}",
                IsAwakened = false,
                Durability = unitData.BaseDurability,
                Energy = unitData.BaseEnergy,
                LockedByExpeditionId = expeditionId,
                BehaviorMode = unitData.DefaultBehaviorMode,
                CurrentCommand = $"expedition:{expeditionId}"
            };
            _session.UnitInstances[unitInstance.UnitInstanceId] = unitInstance;
            createdUnitInstanceIds.Add(unitInstance.UnitInstanceId);
        }

        message = string.Empty;
        return true;
    }

    private InventoryContainer EnsureUnitInventoryForExpedition(ExpeditionState expeditionState, UnitInstance unitInstance)
    {
        if (!_registry.TryGetUnit(unitInstance.UnitId, out UnitData? unitData) || unitData is null)
        {
            return EnsureInventory($"unit_inventory_{expeditionState.ExpeditionId}_{unitInstance.UnitInstanceId}", "unit_inventory", unitInstance.UnitInstanceId, 8, 40f);
        }

        if (!string.IsNullOrEmpty(unitInstance.InventoryId) &&
            _session.Inventories.TryGetValue(unitInstance.InventoryId, out InventoryContainer? existingInventory) &&
            existingInventory.OwnerType == "unit_inventory" &&
            existingInventory.OwnerId == unitInstance.UnitInstanceId &&
            expeditionState.LocationInventoryIds.Contains(existingInventory.InventoryId))
        {
            return existingInventory;
        }

        string inventoryId = UniqueId(
            $"unit_inventory_{expeditionState.ExpeditionId}_{unitInstance.UnitInstanceId}",
            expeditionState.Seed,
            _session.Inventories.ContainsKey);
        InventoryContainer unitInventory = new()
        {
            InventoryId = inventoryId,
            OwnerType = "unit_inventory",
            OwnerId = unitInstance.UnitInstanceId,
            SlotLimit = unitData.InventoryCapacity,
            WeightLimit = unitData.CarryWeightLimit
        };
        _session.Inventories[unitInventory.InventoryId] = unitInventory;
        unitInstance.InventoryId = unitInventory.InventoryId;
        if (!expeditionState.LocationInventoryIds.Contains(unitInventory.InventoryId))
        {
            expeditionState.LocationInventoryIds.Add(unitInventory.InventoryId);
        }

        GD.Print($"[库存] 创建远征单位背包：{unitInventory.InventoryId}");
        return unitInventory;
    }

    private void RollbackDropTransaction(
        InventoryContainer orbitInventory,
        InventoryContainer dropCargo,
        IReadOnlyList<ItemStack> movedStacks,
        IReadOnlyList<string> movedInstances,
        IReadOnlyList<ItemStack> consumedPlatforms,
        IReadOnlyList<string> createdUnitInstanceIds)
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

        for (int index = consumedPlatforms.Count - 1; index >= 0; index--)
        {
            orbitInventory.AddStack(consumedPlatforms[index], _registry);
        }

        foreach (string unitInstanceId in createdUnitInstanceIds)
        {
            _session.UnitInstances.Remove(unitInstanceId);
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

    private string FirstKnownCoordinateId()
    {
        foreach (string coordinateId in _session.OrbitState.KnownCoordinates)
        {
            if (_registry.KnownCoordinates.ContainsKey(coordinateId))
            {
                return coordinateId;
            }
        }

        return _registry.KnownCoordinates.ContainsKey(DefaultCoordinateId) ? DefaultCoordinateId : string.Empty;
    }

    private string FirstUnlockedDropPodId()
    {
        if (_registry.TryGetDropPod(DefaultDropPodId, out DropPodData? defaultPod) && defaultPod is not null && IsDropPodUnlocked(defaultPod))
        {
            return DefaultDropPodId;
        }

        foreach (DropPodData pod in _registry.DropPods.Values)
        {
            if (IsDropPodUnlocked(pod))
            {
                return pod.Id;
            }
        }

        return _registry.DropPods.Keys.FirstOrDefault() ?? string.Empty;
    }

    private Vector2I ResolveCoordinate(string coordinateId)
    {
        return _registry.TryGetKnownCoordinate(coordinateId, out KnownCoordinate? coordinate) && coordinate is not null
            ? coordinate.DropPosition
            : Vector2I.Zero;
    }

    private int GetUnitCapacityCost(string unitInstanceId)
    {
        if (_session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? instance) &&
            _registry.TryGetUnit(instance.UnitId, out UnitData? unitData) &&
            unitData is not null)
        {
            return GetUnitCapacityCost(unitData, instance.UnitId);
        }

        return 1;
    }

    private static int GetUnitCapacityCost(UnitData unitData, string unitId)
    {
        return unitId is "heavy_cargo_spider" or "rockbreaker" || unitData.Tags.Contains("heavy") ? 2 : 1;
    }

    private static float GetUnitDropWeight(string unitId)
    {
        return unitId switch
        {
            "dexter" => 24f,
            "service_bot" => 18f,
            "light_cargo_drone" => 8f,
            "heavy_cargo_spider" => 20f,
            "rockbreaker" => 20f,
            _ => 12f
        };
    }

    private static ItemStack CopyStack(ItemStack stack)
    {
        return new ItemStack
        {
            ItemId = stack.ItemId,
            Count = stack.Count
        };
    }

    private static string UniqueId(string prefix, int seed, Func<string, bool> exists)
    {
        string candidate = $"{prefix}_{seed}";
        if (!exists(candidate))
        {
            return candidate;
        }

        int index = 1;
        do
        {
            candidate = $"{prefix}_{seed}_{index}";
            index++;
        }
        while (exists(candidate));

        return candidate;
    }

    private static int CreateSeed()
    {
        ulong ticks = Time.GetTicksMsec();
        return 460001 + (int)(ticks % 1000000UL);
    }
}
