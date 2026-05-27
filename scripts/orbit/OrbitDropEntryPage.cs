using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGame;

public partial class OrbitStation
{
    private void BuildDropPage()
    {
        SetPageTitle("空投 | 配置入口");
        AddHintFilter("进入空投配置后确认坐标、空投舱、单位和物资");

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
        bool canEnterDropConfig = _gameRoot is not null && _gameRoot.Session.ActiveExpedition is null;
        ConfigureAction("进入空投配置", canEnterDropConfig, "已有进行中的远征", () =>
        {
            if (_gameRoot is null)
            {
                return;
            }

            ScenePayload payload = _gameRoot.CreateNavigationPayload(SceneId.OrbitStation, SceneId.DropConfig);
            _gameRoot.NavigateTo(SceneId.DropConfig, payload);
        });
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
        string coordinates = session.OrbitState.KnownCoordinates.Count == 0
            ? "暂无已知坐标"
            : string.Join(", ", session.OrbitState.KnownCoordinates.Select(coordinateId =>
                registry.TryGetKnownCoordinate(coordinateId, out KnownCoordinate? coordinate) && coordinate is not null
                    ? $"{coordinate.DisplayName} [{coordinateId}]"
                    : coordinateId));
        rows.Add(new OrbitInfoRow(
            "drop:coordinates",
            "已知空投坐标",
            $"坐标数量 {session.OrbitState.KnownCoordinates.Count}",
            UiAssets.IconDrop,
            $"已知坐标：{coordinates}\n坐标由空投配置确认后写入远征状态。"));

        foreach (DropPodData pod in registry.DropPods.Values)
        {
            bool unlocked = (string.IsNullOrEmpty(pod.RequiresBlueprintId) || session.OrbitState.UnlockedBlueprints.Contains(pod.RequiresBlueprintId)) &&
                pod.RequiresProtocolIds.All(protocolId => session.OrbitState.UnlockedProtocols.Contains(protocolId));
            rows.Add(new OrbitInfoRow(
                $"drop_pod:{pod.Id}",
                pod.DisplayName,
                $"空投舱  载重 {pod.WeightLimit:0.#}  格位 {pod.SlotLimit}  单位容量 {pod.UnitCapacity}  {(unlocked ? "可用" : "未解锁")}",
                pod.IconPath,
                $"{pod.Description}\n可接受标签：{string.Join(", ", pod.AcceptedTags)}\n前置蓝图：{(string.IsNullOrEmpty(pod.RequiresBlueprintId) ? "无" : pod.RequiresBlueprintId)}\n前置协议：{FormatIds(pod.RequiresProtocolIds)}"));
        }

        int awakenedCount = session.OrbitState.AwakenedUnits.Count(unitInstanceId =>
            session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance) && unitInstance.Durability > 0 && !unitInstance.IsLocked);
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
                session.ActiveExpedition is null ? "当前可进入空投配置。" : "已有进行中的远征，不能重复创建。"
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
