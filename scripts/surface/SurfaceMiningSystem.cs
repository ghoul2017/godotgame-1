using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGame;

public sealed class SurfaceMiningSystem
{
    private readonly GameSession _session;
    private readonly DataRegistry _registry;

    public SurfaceMiningSystem(GameSession session, DataRegistry registry)
    {
        _session = session;
        _registry = registry;
    }

    public bool TryGather(
        ExpeditionState expeditionState,
        IReadOnlyList<string> selectedUnitInstanceIds,
        string mineralDepositInstanceId,
        out GatherRecord gatherRecord,
        out string message)
    {
        gatherRecord = new GatherRecord
        {
            ExpeditionId = expeditionState.ExpeditionId,
            MineralDepositInstanceId = mineralDepositInstanceId,
            CreatedAt = Time.GetUnixTimeFromSystem()
        };

        if (!expeditionState.MineralDepositStates.TryGetValue(mineralDepositInstanceId, out MineralDepositInstance? mineralInstance))
        {
            return Fail(expeditionState, gatherRecord, $"找不到矿产点实例：{mineralDepositInstanceId}", out message);
        }

        gatherRecord.Position = mineralInstance.Position;
        if (!_registry.TryGetMineralDeposit(mineralInstance.MineralDepositId, out MineralDepositData? mineralData) || mineralData is null)
        {
            return Fail(expeditionState, gatherRecord, $"矿产点定义缺失：{mineralInstance.MineralDepositId}", out message);
        }

        gatherRecord.ItemId = mineralData.YieldItemId;
        if (!_registry.TryGetItem(mineralData.YieldItemId, out ItemData? _))
        {
            return Fail(expeditionState, gatherRecord, $"矿产点产出道具缺失：{mineralData.YieldItemId}", out message);
        }

        if (!mineralInstance.IsDiscovered || !expeditionState.MapState.DiscoveredMineralDepositIds.Contains(mineralInstance.MineralDepositInstanceId))
        {
            return Fail(expeditionState, gatherRecord, "矿产点尚未发现，不能采集。", out message);
        }

        if (mineralInstance.IsDepleted || mineralInstance.RemainingYield <= 0)
        {
            return Fail(expeditionState, gatherRecord, "矿产点已经耗尽。", out message);
        }

        if (!TrySelectGatherUnit(expeditionState, selectedUnitInstanceIds, mineralData, out UnitInstance? unitInstance, out UnitData? unitData, out string unitMessage) ||
            unitInstance is null ||
            unitData is null)
        {
            return Fail(expeditionState, gatherRecord, unitMessage, out message);
        }

        InventoryContainer unitInventory = EnsureUnitInventory(expeditionState, unitInstance, unitData);
        int count = Mathf.Min(mineralData.BaseYield, mineralInstance.RemainingYield);
        gatherRecord.UnitInstanceId = unitInstance.UnitInstanceId;
        gatherRecord.Count = count;

        ItemStack stack = new()
        {
            ItemId = mineralData.YieldItemId,
            Count = count
        };
        InventoryTransferResult inventoryResult = unitInventory.AddStack(stack, _registry);
        if (inventoryResult.IsSuccess && inventoryResult.Transfer is not null)
        {
            InventoryTransfer transfer = inventoryResult.Transfer;
            transfer.FromInventoryId = $"mineral_deposit:{mineralInstance.MineralDepositInstanceId}";
            transfer.ToInventoryId = unitInventory.InventoryId;
            transfer.Reason = "surface_gather";
            transfer.ExpeditionId = expeditionState.ExpeditionId;
            _session.InventoryTransfers.Add(transfer);

            gatherRecord.TargetLocationType = "unit_inventory";
            gatherRecord.TargetId = unitInstance.UnitInstanceId;
            gatherRecord.DestinationInventoryId = unitInventory.InventoryId;
            gatherRecord.TransferId = transfer.TransferId;
            gatherRecord.Result = "inventory";
        }
        else
        {
            GroundItemState groundItem = CreateGroundItem(expeditionState, mineralInstance, stack);
            gatherRecord.TargetLocationType = "ground_item";
            gatherRecord.TargetId = groundItem.GroundItemStateId;
            gatherRecord.GroundItemStateId = groundItem.GroundItemStateId;
            gatherRecord.Result = "ground_item";
            gatherRecord.FailureReason = inventoryResult.Message;
        }

        mineralInstance.RemainingYield = Mathf.Max(0, mineralInstance.RemainingYield - count);
        mineralInstance.IsDepleted = mineralInstance.RemainingYield <= 0;
        expeditionState.GatherRecords.Add(gatherRecord);
        unitInstance.CurrentCommand = $"gather:{gatherRecord.GatherRecordId}";
        message = gatherRecord.Result == "inventory"
            ? $"采集完成：{unitData.DisplayName} 获得 {mineralData.YieldItemId} x{count}，进入单位背包。"
            : $"采集完成：{mineralData.YieldItemId} x{count} 已落地，原因：{gatherRecord.FailureReason}";
        GD.Print($"[矿产] 采集完成：{gatherRecord.GatherRecordId} {mineralData.YieldItemId} x{count} -> {gatherRecord.TargetLocationType}");
        return true;
    }

    private bool TrySelectGatherUnit(
        ExpeditionState expeditionState,
        IReadOnlyList<string> selectedUnitInstanceIds,
        MineralDepositData mineralData,
        out UnitInstance? unitInstance,
        out UnitData? unitData,
        out string message)
    {
        foreach (string unitInstanceId in selectedUnitInstanceIds)
        {
            if (!expeditionState.ActiveUnitInstanceIds.Contains(unitInstanceId) ||
                !_session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? candidateUnit) ||
                !_registry.TryGetUnit(candidateUnit.UnitId, out UnitData? candidateData) ||
                candidateData is null)
            {
                continue;
            }

            if (!candidateData.AvailableCommands.Contains("gather"))
            {
                continue;
            }

            if (!HasRequiredGatherTags(candidateData, mineralData))
            {
                continue;
            }

            unitInstance = candidateUnit;
            unitData = candidateData;
            message = string.Empty;
            return true;
        }

        unitInstance = null;
        unitData = null;
        message = "已选单位没有可执行该矿产点采集的单位。";
        return false;
    }

    private static bool HasRequiredGatherTags(UnitData unitData, MineralDepositData mineralData)
    {
        if (mineralData.RequiredToolTags.Count == 0)
        {
            return true;
        }

        HashSet<string> capabilityTags = new(unitData.Tags);
        foreach (string slot in unitData.EquipmentSlots)
        {
            capabilityTags.Add(slot);
        }

        return mineralData.RequiredToolTags.Any(capabilityTags.Contains);
    }

    private InventoryContainer EnsureUnitInventory(ExpeditionState expeditionState, UnitInstance unitInstance, UnitData unitData)
    {
        if (!string.IsNullOrEmpty(unitInstance.InventoryId) &&
            _session.Inventories.TryGetValue(unitInstance.InventoryId, out InventoryContainer? existingInventory) &&
            existingInventory.OwnerType == "unit_inventory" &&
            existingInventory.OwnerId == unitInstance.UnitInstanceId)
        {
            if (!expeditionState.LocationInventoryIds.Contains(existingInventory.InventoryId))
            {
                expeditionState.LocationInventoryIds.Add(existingInventory.InventoryId);
            }

            return existingInventory;
        }

        string inventoryId = UniqueInventoryId(expeditionState, unitInstance.UnitInstanceId);
        InventoryContainer inventory = new()
        {
            InventoryId = inventoryId,
            OwnerType = "unit_inventory",
            OwnerId = unitInstance.UnitInstanceId,
            SlotLimit = unitData.InventoryCapacity,
            WeightLimit = unitData.CarryWeightLimit
        };
        _session.Inventories[inventory.InventoryId] = inventory;
        unitInstance.InventoryId = inventory.InventoryId;
        expeditionState.LocationInventoryIds.Add(inventory.InventoryId);
        return inventory;
    }

    private GroundItemState CreateGroundItem(ExpeditionState expeditionState, MineralDepositInstance mineralInstance, ItemStack stack)
    {
        string groundItemId = UniqueGroundItemId(expeditionState);
        GroundItemState groundItem = new()
        {
            GroundItemStateId = groundItemId,
            Position = mineralInstance.Position + new Vector2I(30, 24),
            Stack = new ItemStack
            {
                ItemId = stack.ItemId,
                Count = stack.Count
            },
            SourceType = "mineral_deposit",
            SourceId = mineralInstance.MineralDepositInstanceId,
            CreatedAtRunTime = Time.GetUnixTimeFromSystem()
        };
        _session.GroundItems[groundItem.GroundItemStateId] = groundItem;
        expeditionState.GroundItemStateIds.Add(groundItem.GroundItemStateId);
        return groundItem;
    }

    private bool Fail(ExpeditionState expeditionState, GatherRecord gatherRecord, string reason, out string message)
    {
        gatherRecord.Result = "failed";
        gatherRecord.FailureReason = reason;
        expeditionState.GatherRecords.Add(gatherRecord);
        message = reason;
        GD.PushWarning($"[矿产] 采集失败：{reason}");
        return false;
    }

    private string UniqueInventoryId(ExpeditionState expeditionState, string unitInstanceId)
    {
        string baseId = $"unit_inventory_{expeditionState.ExpeditionId}_{unitInstanceId}";
        if (!_session.Inventories.ContainsKey(baseId))
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
        while (_session.Inventories.ContainsKey(candidate));

        return candidate;
    }

    private string UniqueGroundItemId(ExpeditionState expeditionState)
    {
        string baseId = $"ground_item_{expeditionState.ExpeditionId}_{expeditionState.GroundItemStateIds.Count + 1}";
        if (!_session.GroundItems.ContainsKey(baseId))
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
        while (_session.GroundItems.ContainsKey(candidate));

        return candidate;
    }
}
