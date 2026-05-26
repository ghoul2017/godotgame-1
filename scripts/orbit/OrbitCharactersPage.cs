using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGame;

public partial class OrbitStation
{
    private void BuildCharactersPage()
    {
        SetPageTitle("角色 | 觉醒者与量产单位资产");
        AddHintFilter("觉醒者、量产单位和平台道具");

        List<OrbitInfoRow> rows = BuildCharacterRows();
        if (rows.Count == 0)
        {
            AddEmptyListMessage("轨道层暂无可显示角色资产。");
            ShowEmptyDetail("角色为空", "角色页只读取 OrbitState 和单位实例池，不伪造可用角色。");
            return;
        }

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
    }

    private List<OrbitInfoRow> BuildCharacterRows()
    {
        List<OrbitInfoRow> rows = new();
        if (_gameRoot is null)
        {
            return rows;
        }

        GameSession session = _gameRoot.Session;
        DataRegistry registry = _gameRoot.DataRegistry;
        foreach (string unitInstanceId in session.OrbitState.AwakenedUnits)
        {
            if (!session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? instance) ||
                !registry.TryGetUnit(instance.UnitId, out UnitData? unitData) ||
                unitData is null)
            {
                continue;
            }

            string displayName = string.IsNullOrEmpty(instance.DisplayNameOverride) ? unitData.DisplayName : instance.DisplayNameOverride;
            rows.Add(new OrbitInfoRow(
                $"awakened:{unitInstanceId}",
                displayName,
                $"觉醒者  {unitData.DisplayName}  耐久 {instance.Durability}/{unitData.BaseDurability}  能源 {instance.Energy}/{unitData.BaseEnergy}",
                unitData.PortraitPath,
                string.Join("\n", new[]
                {
                    unitData.Description,
                    $"类型：觉醒者 / {unitData.UnitRole}",
                    $"状态：{(instance.Durability <= 0 ? "受损" : "待命")}",
                    $"技能：工程 {SkillLevel(instance, "engineering")}  射击 {SkillLevel(instance, "shooting")}  挖矿 {SkillLevel(instance, "mining")}  控制 {SkillLevel(instance, "control")}",
                    $"装备：{FormatIds(instance.EquipmentInstanceIds)}",
                    $"改装：{FormatIds(instance.ModPartInstanceIds)}",
                    $"可参与空投：{(instance.Durability > 0 ? "是" : "否")}",
                    $"资源引用：{unitData.PortraitPath}"
                })));
        }

        bool hasRockbreaker = session.OrbitState.AwakenedUnits.Any(unitInstanceId =>
            session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? instance) && instance.UnitId == "rockbreaker");
        if (!hasRockbreaker && registry.TryGetUnit("rockbreaker", out UnitData? rockbreaker) && rockbreaker is not null)
        {
            rows.Add(new OrbitInfoRow(
                "locked:rockbreaker",
                "碎石",
                "觉醒者线索  剧情未加入  来源：序章与地表回归",
                rockbreaker.PortraitPath,
                string.Join("\n", new[]
                {
                    rockbreaker.Description,
                    "当前状态：锁定",
                    "来源条件：序章或后续远征回归写入觉醒者实例后解锁。",
                    "此卡片不伪造可用角色，只保留正式角色入口。"
                })));
        }

        foreach (string unitInstanceId in session.OrbitState.AvailableMassUnitInstanceIds)
        {
            if (!session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? instance) ||
                !registry.TryGetUnit(instance.UnitId, out UnitData? unitData) ||
                unitData is null)
            {
                continue;
            }

            rows.Add(new OrbitInfoRow(
                $"mass:{unitInstanceId}",
                string.IsNullOrEmpty(instance.DisplayNameOverride) ? unitData.DisplayName : instance.DisplayNameOverride,
                $"量产单位  {unitData.DisplayName}  耐久 {instance.Durability}/{unitData.BaseDurability}  可空投 {(instance.Durability > 0 ? "是" : "否")}",
                unitData.IconPath,
                string.Join("\n", new[]
                {
                    unitData.Description,
                    $"类型：量产单位 / {unitData.UnitRole}",
                    $"状态：{(instance.Durability <= 0 ? "受损" : "轨道待命")}",
                    "量产单位不可成长，后续空投配置会决定是否带入地表。"
                })));
        }

        InventoryContainer? orbitInventory = session.Inventories.GetValueOrDefault(session.OrbitState.InventoryId);
        if (orbitInventory is not null)
        {
            foreach (ItemStack platformStack in orbitInventory.ItemStacks)
            {
                if (!registry.TryGetItem(platformStack.ItemId, out ItemData? itemData) ||
                    itemData is null ||
                    itemData.Category != "unit_platform")
                {
                    continue;
                }

                rows.Add(new OrbitInfoRow(
                    $"platform:{itemData.Id}",
                    itemData.DisplayName,
                    $"单位平台道具  数量 {platformStack.Count}  后续空投配置可转化为远征单位",
                    itemData.IconPath,
                    string.Join("\n", new[]
                    {
                        itemData.Description,
                        $"分类：{itemData.Category}",
                        $"数量：{platformStack.Count}",
                        "状态：尚未实例化为量产单位。第四步空投配置负责选择和转换。"
                    })));
            }
        }

        return rows;
    }
}
