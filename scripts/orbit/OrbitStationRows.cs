using System.Collections.Generic;

namespace GodotGame;

public partial class OrbitStation
{
    private sealed class InventoryDisplayRow
    {
        public string Id { get; private init; } = string.Empty;
        public string DisplayName { get; private init; } = string.Empty;
        public string CategoryText { get; private init; } = string.Empty;
        public string SubCategory { get; private init; } = string.Empty;
        public string FilterKey { get; private init; } = string.Empty;
        public int Count { get; private init; }
        public float UnitWeight { get; private init; }
        public float TotalWeight { get; private init; }
        public int BaseValue { get; private init; }
        public string IconPath { get; private init; } = string.Empty;
        public string Description { get; private init; } = string.Empty;
        public List<string> Tags { get; } = new();
        public bool IsInstance { get; private init; }
        public string InstanceId { get; private init; } = string.Empty;
        public bool IsQuestItem { get; private init; }
        public bool IsResearchMaterial { get; private init; }
        public bool CanDrop { get; private init; }

        public static InventoryDisplayRow FromStack(ItemData itemData, ItemStack stack)
        {
            return FromItem(itemData, $"stack:{stack.ItemId}", stack.Count, string.Empty, false);
        }

        public static InventoryDisplayRow FromInstance(ItemData itemData, ItemInstance instance)
        {
            return FromItem(itemData, $"instance:{instance.InstanceId}", 1, instance.InstanceId, true);
        }

        public static InventoryDisplayRow Missing(string id, int count)
        {
            return new InventoryDisplayRow
            {
                Id = $"missing:{id}",
                DisplayName = id,
                CategoryText = "定义缺失",
                SubCategory = "missing",
                FilterKey = "key",
                Count = count,
                IconPath = UiAssets.OrbitIconLocked,
                Description = "该库存条目引用的数据定义缺失，需要补齐定义或清理存档。",
                CanDrop = false
            };
        }

        private static InventoryDisplayRow FromItem(ItemData itemData, string rowId, int count, string instanceId, bool isInstance)
        {
            InventoryDisplayRow row = new()
            {
                Id = rowId,
                DisplayName = itemData.DisplayName,
                CategoryText = CategoryDisplayName(itemData.Category),
                SubCategory = itemData.SubCategory,
                FilterKey = FilterFor(itemData),
                Count = count,
                UnitWeight = itemData.UnitWeight,
                TotalWeight = itemData.UnitWeight * count,
                BaseValue = itemData.BaseValue,
                IconPath = itemData.IconPath,
                Description = itemData.Description,
                IsInstance = isInstance,
                InstanceId = instanceId,
                IsQuestItem = itemData.IsQuestItem,
                IsResearchMaterial = itemData.Category is "basic_mineral" or "processed_item" or "building_module" or "blueprint" or "data_core",
                CanDrop = !itemData.IsQuestItem || itemData.Tags.Contains("quest")
            };
            row.Tags.AddRange(itemData.Tags);
            return row;
        }

        private static string FilterFor(ItemData itemData)
        {
            return itemData.Category switch
            {
                "basic_mineral" => "mineral",
                "processed_item" or "building_module" => "material",
                "weapon" or "tool" or "mod_part" => "equipment",
                "ai_chip" => "chip",
                "unit_platform" => "unit_platform",
                "blueprint" => "blueprint",
                "data_core" => "key",
                _ => itemData.IsQuestItem ? "key" : "material"
            };
        }

        private static string CategoryDisplayName(string category)
        {
            return category switch
            {
                "basic_mineral" => "基础矿产",
                "processed_item" => "加工物资",
                "data_core" => "关键数据",
                "ai_chip" => "AI 芯片",
                "mod_part" => "改装件",
                "weapon" => "武器",
                "tool" => "工具",
                "unit_platform" => "单位平台",
                "building_module" => "建筑模块",
                "blueprint" => "蓝图",
                "consumable" => "消耗品",
                _ => category
            };
        }
    }

    private sealed record OrbitInfoRow(string Id, string Title, string Subtitle, string IconPath, string Detail);
}
