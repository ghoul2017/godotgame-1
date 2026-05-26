using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGame;

public partial class OrbitStation
{
    private void BuildDropPage()
    {
        SetPageTitle("空投 | 后续配置入口");
        AddHintFilter("只展示准备状态，不创建远征");

        List<OrbitInfoRow> rows = BuildDropRows();
        if (string.IsNullOrEmpty(_selectedId) || rows.All(row => row.Id != _selectedId))
        {
            _selectedId = rows[0].Id;
        }

        foreach (OrbitInfoRow row in rows)
        {
            Button button = CreateRowButton(
                row.Id,
                $"{row.Title}\n{row.Subtitle}",
                row.IconPath,
                () =>
                {
                    _selectedId = row.Id;
                    PlayAudio(UiAssets.OrbitAudioSelect);
                    RefreshCurrentPage();
                });
            button.ButtonPressed = row.Id == _selectedId;
            _listContainer?.AddChild(button);
        }

        OrbitInfoRow selected = rows.First(row => row.Id == _selectedId);
        ShowInfoDetail(selected);
        ConfigureAction("进入空投配置", false, "空投配置模块尚未接入", () => { });
    }

    private List<OrbitInfoRow> BuildDropRows()
    {
        List<OrbitInfoRow> rows = new();
        if (_gameRoot is null)
        {
            return rows;
        }

        GameSession session = _gameRoot.Session;
        DataRegistry registry = _gameRoot.DataRegistry;
        string coordinates = session.OrbitState.KnownCoordinates.Count == 0 ? "暂无已知坐标" : string.Join(", ", session.OrbitState.KnownCoordinates);
        rows.Add(new OrbitInfoRow(
            "drop:coordinates",
            "已知空投坐标",
            $"坐标数量 {session.OrbitState.KnownCoordinates.Count}",
            UiAssets.IconDrop,
            $"已知坐标：{coordinates}\n坐标只作为后续空投配置输入，本页不会创建远征。"));

        foreach (DropPodData pod in registry.DropPods.Values)
        {
            rows.Add(new OrbitInfoRow(
                $"drop_pod:{pod.Id}",
                pod.DisplayName,
                $"空投舱  载重 {pod.WeightLimit:0.#}  格位 {pod.SlotLimit}  单位容量 {pod.UnitCapacity}",
                pod.IconPath,
                $"{pod.Description}\n可接受标签：{string.Join(", ", pod.AcceptedTags)}\n第四步会根据蓝图和协议决定可用类型与容量。"));
        }

        int awakenedCount = session.OrbitState.AwakenedUnits.Count(unitInstanceId =>
            session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance) && unitInstance.Durability > 0);
        int platformCount = CountInventoryCategory("unit_platform");
        int equipmentCount = CountInventoryCategories("weapon", "tool", "ai_chip", "mod_part");
        rows.Add(new OrbitInfoRow(
            "drop:readiness",
            "准备状态概览",
            $"觉醒者 {awakenedCount}  单位平台 {platformCount}  装备/芯片 {equipmentCount}",
            UiAssets.OrbitIconAvailable,
            string.Join("\n", new[]
            {
                $"可用觉醒者：{awakenedCount}",
                $"量产机平台道具：{platformCount}",
                $"可携带装备、工具、芯片和改装件：{equipmentCount}",
                $"已解锁蓝图：{FormatIds(session.OrbitState.UnlockedBlueprints)}",
                $"已解锁协议：{FormatIds(session.OrbitState.UnlockedProtocols)}",
                "进入空投配置按钮保持锁定：空投配置模块尚未接入。"
            })));

        return rows;
    }

    private int CountInventoryCategory(string category)
    {
        if (_gameRoot is null ||
            !_gameRoot.Session.Inventories.TryGetValue(_gameRoot.Session.OrbitState.InventoryId, out InventoryContainer? inventory))
        {
            return 0;
        }

        int total = 0;
        foreach (ItemStack stack in inventory.ItemStacks)
        {
            if (_gameRoot.DataRegistry.TryGetItem(stack.ItemId, out ItemData? itemData) && itemData?.Category == category)
            {
                total += stack.Count;
            }
        }

        return total;
    }

    private int CountInventoryCategories(params string[] categories)
    {
        if (_gameRoot is null ||
            !_gameRoot.Session.Inventories.TryGetValue(_gameRoot.Session.OrbitState.InventoryId, out InventoryContainer? inventory))
        {
            return 0;
        }

        HashSet<string> categorySet = new(categories);
        int total = 0;
        foreach (ItemStack stack in inventory.ItemStacks)
        {
            if (_gameRoot.DataRegistry.TryGetItem(stack.ItemId, out ItemData? itemData) && itemData is not null && categorySet.Contains(itemData.Category))
            {
                total += stack.Count;
            }
        }

        foreach (string itemInstanceId in inventory.ItemInstanceIds)
        {
            if (_gameRoot.Session.ItemInstances.TryGetValue(itemInstanceId, out ItemInstance? instance) &&
                _gameRoot.DataRegistry.TryGetItem(instance.ItemId, out ItemData? itemData) &&
                itemData is not null &&
                categorySet.Contains(itemData.Category))
            {
                total += 1;
            }
        }

        return total;
    }
}
