using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGame;

public partial class OrbitStation
{
    private void BuildInventoryPage()
    {
        SetPageTitle("库存 | 轨道永久资产");
        AddInventoryFilters();

        if (_gameRoot is null)
        {
            AddEmptyListMessage("轨道永久库存等待会话状态。");
            ShowEmptyDetail("库存不可用", "主入口尚未提供 GameSession。");
            return;
        }

        List<InventoryDisplayRow> rows = BuildInventoryRows();
        rows = rows.Where(row => InventoryFilterMatches(row.FilterKey)).ToList();
        if (rows.Count == 0)
        {
            AddEmptyListMessage("当前轨道库存无可显示物资。需要通过远征回归、交易或研发获得资源。");
            ShowEmptyDetail("库存为空", "库存页不会用假物品填充列表；回归结算写入后会直接读取同一份轨道库存。");
            return;
        }

        if (string.IsNullOrEmpty(_selectedId) || rows.All(row => row.Id != _selectedId))
        {
            _selectedId = rows[0].Id;
        }

        foreach (InventoryDisplayRow row in rows)
        {
            Button button = CreateRowButton(
                row.Id,
                $"{row.DisplayName}\n{row.CategoryText}  数量 {row.Count}  单重 {row.UnitWeight:0.##}  总重 {row.TotalWeight:0.##}  价值 {row.BaseValue}",
                row.IconPath,
                () =>
                {
                    _selectedId = row.Id;
                    _pendingActionId = string.Empty;
                    PlayAudio(UiAssets.OrbitAudioSelect);
                    RefreshCurrentPage();
                });
            button.ButtonPressed = row.Id == _selectedId;
            _listContainer?.AddChild(button);
        }

        InventoryDisplayRow selected = rows.First(row => row.Id == _selectedId);
        ShowInventoryDetail(selected);
    }

    private void AddInventoryFilters()
    {
        HBoxContainer filters = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _filterContainer?.AddChild(filters);

        AddFilterButton(filters, "全部", "all", UiAssets.OrbitCategoryAll);
        AddFilterButton(filters, "资源", "resource", UiAssets.OrbitCategoryResource);
        AddFilterButton(filters, "装备", "equipment", UiAssets.OrbitCategoryEquipment);
        AddFilterButton(filters, "芯片", "chip", UiAssets.OrbitCategoryChip);
        AddFilterButton(filters, "单位平台", "unit_platform", UiAssets.OrbitCategoryUnitPlatform);
        AddFilterButton(filters, "蓝图", "blueprint", UiAssets.OrbitCategoryBlueprint);
        AddFilterButton(filters, "关键物", "key", UiAssets.OrbitCategoryKeyItem);
    }

    private void AddFilterButton(HBoxContainer filters, string text, string filterKey, string iconPath)
    {
        Button button = new()
        {
            Text = text,
            Icon = UiAssets.LoadTexture(iconPath),
            ToggleMode = true,
            ButtonPressed = _inventoryFilter == filterKey,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        button.Pressed += () =>
        {
            _inventoryFilter = filterKey;
            _selectedId = string.Empty;
            RefreshCurrentPage();
            PlayAudio(UiAssets.OrbitAudioSelect);
        };
        filters.AddChild(button);
    }

    private void ShowInventoryDetail(InventoryDisplayRow row)
    {
        SetDetail(row.DisplayName, row.IconPath, string.Join("\n", new[]
        {
            row.Description,
            $"分类：{row.CategoryText} / {row.SubCategory}",
            $"数量：{row.Count}  单重：{row.UnitWeight:0.##}  总重：{row.TotalWeight:0.##}  基础价值：{row.BaseValue}",
            $"标签：{string.Join(", ", row.Tags)}",
            $"可交易：{(row.IsQuestItem ? "否" : "是")}  可研发消耗：{(row.IsResearchMaterial ? "是" : "否")}  可空投携带：{(row.CanDrop ? "是" : "否")}",
            row.IsInstance ? $"实例 ID：{row.InstanceId}" : "堆叠道具：由统一库存保存数量",
            ResourceExists(row.IconPath) ? $"资源引用：{row.IconPath}" : $"资源引用缺失：{row.IconPath}"
        }));
        ResetAction();
    }

    private List<InventoryDisplayRow> BuildInventoryRows()
    {
        List<InventoryDisplayRow> rows = new();
        if (_gameRoot is null)
        {
            return rows;
        }

        GameSession session = _gameRoot.Session;
        DataRegistry registry = _gameRoot.DataRegistry;
        if (!session.Inventories.TryGetValue(session.OrbitState.InventoryId, out InventoryContainer? inventory))
        {
            return rows;
        }

        foreach (ItemStack stack in inventory.ItemStacks)
        {
            if (!registry.TryGetItem(stack.ItemId, out ItemData? itemData) || itemData is null)
            {
                rows.Add(InventoryDisplayRow.Missing(stack.ItemId, stack.Count));
                continue;
            }

            rows.Add(InventoryDisplayRow.FromStack(itemData, stack));
        }

        foreach (string itemInstanceId in inventory.ItemInstanceIds)
        {
            if (!session.ItemInstances.TryGetValue(itemInstanceId, out ItemInstance? instance) ||
                !registry.TryGetItem(instance.ItemId, out ItemData? itemData) ||
                itemData is null)
            {
                rows.Add(InventoryDisplayRow.Missing(itemInstanceId, 1));
                continue;
            }

            rows.Add(InventoryDisplayRow.FromInstance(itemData, instance));
        }

        return rows.OrderBy(row => row.FilterKey).ThenBy(row => row.DisplayName).ToList();
    }

    private bool InventoryFilterMatches(string filterKey)
    {
        return _inventoryFilter == "all" || _inventoryFilter == filterKey;
    }
}
