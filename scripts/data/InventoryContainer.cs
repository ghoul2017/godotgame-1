using System;
using System.Collections.Generic;
using System.Linq;

namespace GodotGame;

public sealed class InventoryContainer
{
    public string InventoryId { get; set; } = string.Empty;
    public string OwnerType { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public int SlotLimit { get; set; }
    public float WeightLimit { get; set; }
    public List<string> AcceptedTags { get; } = new();
    public List<string> BlockedTags { get; } = new();
    public List<ItemStack> ItemStacks { get; } = new();
    public List<string> ItemInstanceIds { get; } = new();

    public int GetItemCount(string itemId)
    {
        return ItemStacks.Where(stack => stack.ItemId == itemId).Sum(stack => stack.Count);
    }

    public float GetTotalWeight(DataRegistry registry)
    {
        float totalWeight = 0f;
        foreach (ItemStack stack in ItemStacks)
        {
            if (registry.TryGetItem(stack.ItemId, out ItemData? itemData) && itemData is not null)
            {
                totalWeight += itemData.UnitWeight * stack.Count;
            }
        }

        return totalWeight;
    }

    public float GetTotalWeight(DataRegistry registry, IReadOnlyDictionary<string, ItemInstance> itemInstances)
    {
        float totalWeight = GetTotalWeight(registry);
        foreach (string itemInstanceId in ItemInstanceIds)
        {
            if (itemInstances.TryGetValue(itemInstanceId, out ItemInstance? itemInstance) &&
                registry.TryGetItem(itemInstance.ItemId, out ItemData? itemData) &&
                itemData is not null)
            {
                totalWeight += itemData.UnitWeight;
            }
        }

        return totalWeight;
    }

    public InventoryTransferResult AddStack(ItemStack stack, DataRegistry registry)
    {
        if (stack.Count <= 0)
        {
            return InventoryTransferResult.Fail(InventoryTransferStatus.InvalidQuantity, "道具数量必须大于 0");
        }

        if (!registry.TryGetItem(stack.ItemId, out ItemData? itemData) || itemData is null)
        {
            return InventoryTransferResult.Fail(InventoryTransferStatus.MissingDefinition, $"找不到道具定义：{stack.ItemId}");
        }

        if (itemData.RequiresInstance)
        {
            return InventoryTransferResult.Fail(InventoryTransferStatus.InvalidQuantity, $"实例道具不能通过堆叠接口加入：{stack.ItemId}");
        }

        if (!Accepts(itemData))
        {
            return InventoryTransferResult.Fail(InventoryTransferStatus.TargetRejected, $"库存不接受道具：{stack.ItemId}");
        }

        if (!CanFit(stack, itemData, registry))
        {
            return InventoryTransferResult.Fail(InventoryTransferStatus.TargetCapacityExceeded, $"库存容量不足：{InventoryId}");
        }

        AddWithoutCapacityCheck(stack, itemData);
        return InventoryTransferResult.Success(new InventoryTransfer
        {
            TransferId = Guid.NewGuid().ToString("N"),
            ToInventoryId = InventoryId,
            ItemId = stack.ItemId,
            Count = stack.Count,
            Reason = "add_stack"
        });
    }

    public bool RemoveStack(string itemId, int count)
    {
        if (count <= 0 || GetItemCount(itemId) < count)
        {
            return false;
        }

        int remaining = count;
        for (int index = ItemStacks.Count - 1; index >= 0 && remaining > 0; index--)
        {
            ItemStack stack = ItemStacks[index];
            if (stack.ItemId != itemId)
            {
                continue;
            }

            int removed = Math.Min(stack.Count, remaining);
            stack.Count -= removed;
            remaining -= removed;
            if (stack.Count <= 0)
            {
                ItemStacks.RemoveAt(index);
            }
        }

        return true;
    }

    public InventoryTransferResult TransferTo(InventoryContainer target, string itemId, int count, DataRegistry registry, string reason, string expeditionId = "")
    {
        if (count <= 0)
        {
            return InventoryTransferResult.Fail(InventoryTransferStatus.InvalidQuantity, "转移数量必须大于 0");
        }

        if (!registry.TryGetItem(itemId, out ItemData? itemData) || itemData is null)
        {
            return InventoryTransferResult.Fail(InventoryTransferStatus.MissingDefinition, $"找不到道具定义：{itemId}");
        }

        if (GetItemCount(itemId) < count)
        {
            return InventoryTransferResult.Fail(InventoryTransferStatus.NotEnoughQuantity, $"库存数量不足：{itemId}");
        }

        if (itemData.RequiresInstance)
        {
            return InventoryTransferResult.Fail(InventoryTransferStatus.InvalidQuantity, $"实例道具不能通过堆叠接口转移：{itemId}");
        }

        ItemStack transferStack = new()
        {
            ItemId = itemId,
            Count = count
        };
        if (!target.Accepts(itemData))
        {
            return InventoryTransferResult.Fail(InventoryTransferStatus.TargetRejected, $"目标库存不接受道具：{itemId}");
        }

        if (!target.CanFit(transferStack, itemData, registry))
        {
            return InventoryTransferResult.Fail(InventoryTransferStatus.TargetCapacityExceeded, $"目标库存容量不足：{target.InventoryId}");
        }

        RemoveStack(itemId, count);
        target.AddWithoutCapacityCheck(transferStack, itemData);
        InventoryTransfer transfer = new()
        {
            TransferId = Guid.NewGuid().ToString("N"),
            FromInventoryId = InventoryId,
            ToInventoryId = target.InventoryId,
            ItemId = itemId,
            Count = count,
            Reason = reason,
            ExpeditionId = expeditionId
        };
        return InventoryTransferResult.Success(transfer);
    }

    public InventoryTransferResult AddItemInstance(string itemInstanceId, IReadOnlyDictionary<string, ItemInstance> itemInstances, DataRegistry registry)
    {
        if (!itemInstances.TryGetValue(itemInstanceId, out ItemInstance? itemInstance))
        {
            return InventoryTransferResult.Fail(InventoryTransferStatus.ItemNotFound, $"找不到道具实例：{itemInstanceId}");
        }

        if (!registry.TryGetItem(itemInstance.ItemId, out ItemData? itemData) || itemData is null)
        {
            return InventoryTransferResult.Fail(InventoryTransferStatus.MissingDefinition, $"找不到道具定义：{itemInstance.ItemId}");
        }

        if (!Accepts(itemData))
        {
            return InventoryTransferResult.Fail(InventoryTransferStatus.TargetRejected, $"库存不接受道具实例：{itemInstanceId}");
        }

        if (ItemInstanceIds.Contains(itemInstanceId))
        {
            return InventoryTransferResult.Fail(InventoryTransferStatus.ItemLocked, $"库存已拥有道具实例：{itemInstanceId}");
        }

        if (SlotLimit > 0 && ItemStacks.Count + ItemInstanceIds.Count + 1 > SlotLimit)
        {
            return InventoryTransferResult.Fail(InventoryTransferStatus.TargetCapacityExceeded, $"库存格位不足：{InventoryId}");
        }

        if (WeightLimit > 0f && GetTotalWeight(registry, itemInstances) + itemData.UnitWeight > WeightLimit)
        {
            return InventoryTransferResult.Fail(InventoryTransferStatus.TargetCapacityExceeded, $"库存重量容量不足：{InventoryId}");
        }

        ItemInstanceIds.Add(itemInstanceId);
        InventoryTransfer transfer = new()
        {
            TransferId = Guid.NewGuid().ToString("N"),
            ToInventoryId = InventoryId,
            ItemId = itemInstance.ItemId,
            Count = 1,
            Reason = "add_instance"
        };
        transfer.ItemInstanceIds.Add(itemInstanceId);
        return InventoryTransferResult.Success(transfer);
    }

    public InventoryTransferResult TransferItemInstanceTo(InventoryContainer target, string itemInstanceId, IReadOnlyDictionary<string, ItemInstance> itemInstances, DataRegistry registry, string reason, string expeditionId = "")
    {
        if (!ItemInstanceIds.Contains(itemInstanceId))
        {
            return InventoryTransferResult.Fail(InventoryTransferStatus.ItemNotFound, $"源库存没有道具实例：{itemInstanceId}");
        }

        InventoryTransferResult addResult = target.AddItemInstance(itemInstanceId, itemInstances, registry);
        if (!addResult.IsSuccess)
        {
            return addResult;
        }

        ItemInstanceIds.Remove(itemInstanceId);
        InventoryTransfer transfer = addResult.Transfer ?? new InventoryTransfer();
        transfer.FromInventoryId = InventoryId;
        transfer.ToInventoryId = target.InventoryId;
        transfer.Reason = reason;
        transfer.ExpeditionId = expeditionId;
        return InventoryTransferResult.Success(transfer);
    }

    private bool Accepts(ItemData itemData)
    {
        if (BlockedTags.Any(itemData.Tags.Contains))
        {
            return false;
        }

        return AcceptedTags.Count == 0 || AcceptedTags.Any(itemData.Tags.Contains) || AcceptedTags.Contains(itemData.Category);
    }

    private bool CanFit(ItemStack stack, ItemData itemData, DataRegistry registry)
    {
        int usedSlots = ItemStacks.Count;
        int remaining = stack.Count;
        if (itemData.CanStack)
        {
            foreach (ItemStack existing in ItemStacks.Where(existing => existing.ItemId == stack.ItemId))
            {
                remaining -= Math.Max(0, itemData.MaxStack - existing.Count);
                if (remaining <= 0)
                {
                    break;
                }
            }
        }

        int extraSlots = itemData.CanStack ? Math.Max(0, (int)Math.Ceiling(remaining / (float)itemData.MaxStack)) : stack.Count;
        if (SlotLimit > 0 && usedSlots + extraSlots > SlotLimit)
        {
            return false;
        }

        float addedWeight = itemData.UnitWeight * stack.Count;
        return WeightLimit <= 0f || GetTotalWeight(registry) + addedWeight <= WeightLimit;
    }

    private void AddWithoutCapacityCheck(ItemStack stack, ItemData itemData)
    {
        if (!itemData.CanStack)
        {
            ItemStacks.Add(new ItemStack { ItemId = stack.ItemId, Count = stack.Count });
            return;
        }

        int remaining = stack.Count;
        foreach (ItemStack existing in ItemStacks.Where(existing => existing.ItemId == stack.ItemId))
        {
            int available = Math.Max(0, itemData.MaxStack - existing.Count);
            int moved = Math.Min(available, remaining);
            existing.Count += moved;
            remaining -= moved;
            if (remaining == 0)
            {
                return;
            }
        }

        while (remaining > 0)
        {
            int stackCount = Math.Min(itemData.MaxStack, remaining);
            ItemStacks.Add(new ItemStack { ItemId = stack.ItemId, Count = stackCount });
            remaining -= stackCount;
        }
    }
}
