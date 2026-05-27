using System.Collections.Generic;
using Godot;

namespace GodotGame;

public sealed class DataRegistry
{
    private const string DataAssetRoot = "res://assets/data";
    private readonly Dictionary<string, ItemData> _items = new();
    private readonly Dictionary<string, UnitData> _units = new();
    private readonly Dictionary<string, BuildingData> _buildings = new();
    private readonly Dictionary<string, RecipeData> _recipes = new();
    private readonly Dictionary<string, SkillData> _skills = new();
    private readonly Dictionary<string, EventData> _events = new();
    private readonly Dictionary<string, DropPodData> _dropPods = new();
    private readonly Dictionary<string, KnownCoordinate> _knownCoordinates = new();

    public IReadOnlyDictionary<string, ItemData> Items => _items;
    public IReadOnlyDictionary<string, UnitData> Units => _units;
    public IReadOnlyDictionary<string, BuildingData> Buildings => _buildings;
    public IReadOnlyDictionary<string, RecipeData> Recipes => _recipes;
    public IReadOnlyDictionary<string, SkillData> Skills => _skills;
    public IReadOnlyDictionary<string, EventData> Events => _events;
    public IReadOnlyDictionary<string, DropPodData> DropPods => _dropPods;
    public IReadOnlyDictionary<string, KnownCoordinate> KnownCoordinates => _knownCoordinates;

    public DataLoadReport LoadBuiltInDefinitions()
    {
        _items.Clear();
        _units.Clear();
        _buildings.Clear();
        _recipes.Clear();
        _skills.Clear();
        _events.Clear();
        _dropPods.Clear();
        _knownCoordinates.Clear();

        AddItems();
        AddUnits();
        AddBuildings();
        AddRecipes();
        AddSkills();
        AddEvents();
        AddDropPods();
        AddKnownCoordinates();

        return ValidateDefinitions();
    }

    public bool TryGetItem(string itemId, out ItemData? itemData)
    {
        return _items.TryGetValue(itemId, out itemData);
    }

    public bool TryGetUnit(string unitId, out UnitData? unitData)
    {
        return _units.TryGetValue(unitId, out unitData);
    }

    public bool TryGetBuilding(string buildingId, out BuildingData? buildingData)
    {
        return _buildings.TryGetValue(buildingId, out buildingData);
    }

    public bool TryGetDropPod(string dropPodId, out DropPodData? dropPodData)
    {
        return _dropPods.TryGetValue(dropPodId, out dropPodData);
    }

    public bool TryGetKnownCoordinate(string coordinateId, out KnownCoordinate? coordinate)
    {
        return _knownCoordinates.TryGetValue(coordinateId, out coordinate);
    }

    public string GetItemName(string itemId)
    {
        return _items.TryGetValue(itemId, out ItemData? itemData) ? itemData.DisplayName : itemId;
    }

    public float GetStackWeight(ItemStack stack)
    {
        return _items.TryGetValue(stack.ItemId, out ItemData? itemData) ? itemData.UnitWeight * stack.Count : 0f;
    }

    private void AddItems()
    {
        AddItem("metal", "金属", "basic_mineral", "ore", "可回收结构金属，是多数建筑和平台的基础材料。", 1f, 2, 999, string.Empty, "mineral", "drop_allowed", "metallic", "build_material");
        AddItem("silicon", "硅", "basic_mineral", "mineral", "用于电子元件和传感器阵列的半导体材料。", 0.18f, 3, "mineral", "electronics");
        AddItem("rare_earth", "稀土", "basic_mineral", "mineral", "高性能电机和合金不可缺少的稀有矿物。", 0.2f, 8, "mineral", "advanced");
        AddItem("energy_cell", "能源块", "basic_mineral", "power", "稳定封装能源，用于空投、生产和火箭回归。", 2f, 6, 200, string.Empty, "mineral", "drop_allowed", "energy");
        AddItem("scrap", "废料", "basic_mineral", "salvage", "旧世界机械残骸，可回收为金属和零件。", 1f, 1, 999, string.Empty, "mineral", "drop_allowed", "salvage", "recycle_material");
        AddItem("alloy", "合金", "processed_item", "material", "承力结构和火箭部件使用的强化材料。", 0.28f, 12, "material", "advanced");
        AddItem("electronic_parts", "电子元件", "processed_item", "component", "组装机、扫描器和机器人平台的基础控制组件。", 0.12f, 10, "material", "electronics");
        AddItem("clean_data", "纯净数据", "processed_item", "data", "从污染存储器中整理出的可用旧世界数据。", 0.02f, 18, "material", "data");
        AddItem("data_core", "数据核心", "data_core", "quest", "传闻保存旧人类智慧和权限指令的核心目标。", 1.2f, 500, "quest", "data");
        _items["data_core"].IsUnique = true;
        _items["data_core"].IsQuestItem = true;
        _items["data_core"].CanDiscard = false;
        AddItem("ai_chip_basic", "通用 AI 芯片", "ai_chip", "brain", "可承载基础行为协议的集成智能核心。", 2f, 60, 1, string.Empty, "chip", "drop_allowed", "upgrade", "instance_item");
        AddItem("scanner_basic", "简易扫描器", "tool", "scanner", "用于短距扫描矿产点和废墟信号的手持工具。", 5f, 35, 1, string.Empty, "tool", "drop_allowed", "scan", "instance_item");
        AddItem("repair_tool_basic", "简易维修工具", "tool", "repair", "服务型单位使用的基础维修工具。", 6f, 30, 1, string.Empty, "tool", "drop_allowed", "repair", "instance_item");
        AddItem("rifle_basic", "简易实弹枪", "weapon", "ballistic", "旧式弹道武器，适合服务型平台应急防御。", 8f, 45, 1, string.Empty, "weapon", "drop_allowed", "ballistic", "instance_item");
        AddItem("servo_mod_basic", "基础伺服改装件", "mod_part", "mobility", "提高民用平台关节输出的通用改装件。", 0.45f, 40, "mod_part", "instance_item");
        AddItem("service_bot_platform", "服务型量产机平台", "unit_platform", "humanoid", "可组装服务型量产机的基础机体。", 18f, 85, 10, "service_bot", "unit_platform", "drop_allowed", "service_bot");
        AddItem("light_cargo_drone_platform", "轻型运输无人机平台", "unit_platform", "drone", "可组装轻型运输无人机的空投平台。", 14f, 80, 10, "light_cargo_drone", "unit_platform", "drop_allowed", "drone", "scout");
        AddItem("heavy_cargo_spider_platform", "重型运输机器人平台", "unit_platform", "heavy", "可组装重型运输机器人的多足平台。", 34f, 140, 5, "heavy_cargo_spider", "unit_platform", "drop_allowed", "heavy", "cargo");
        AddItem("rocket_part", "火箭部件", "building_module", "rocket", "火箭组装坪用于推进舱和货舱结构的模块。", 4.0f, 120, "building_module");
    }

    private void AddUnits()
    {
        AddUnit("dexter", "灵巧", "觉醒的服务型家政机器人，擅长设施互动和多类装备使用。", "hero_service", "ground", 120, 100, 12, 24f, true, "tool", "weapon", "utility");
        AddUnit("service_bot", "服务型量产机", "可执行建造、维修和基础战斗任务的民用人型平台。", "worker", "ground", 90, 80, 8, 18f, false, "tool", "weapon");
        AddUnit("light_cargo_drone", "轻型运输无人机", "快速侦察和轻量搬运平台，适合早期扩展视野。", "scout_transport", "air", 55, 70, 6, 12f, false, "cargo");
        AddUnit("heavy_cargo_spider", "重型运输机器人", "多足重载平台，可作为移动掩体和大型设备载体。", "heavy_transport", "ground", 180, 120, 14, 42f, false, "heavy", "cargo");
        AddUnit("rockbreaker", "碎石", "矿工觉醒者，重型运输特化，熟悉废土矿脉和旧设施。", "hero_miner", "ground", 210, 110, 12, 38f, true, "mining", "heavy");
    }

    private void AddBuildings()
    {
        AddBuilding("repair_station", "维修站", "提供单位维修和工具维护的基础设施。", new Vector2I(2, 2), 18f, 0, 8, 12, "repair");
        AddBuilding("assembler_basic", "基础组装机", "执行早期材料转换和平台组装。", new Vector2I(3, 2), 24f, 0, 12, 8, "crafting");
        AddBuilding("storage_box", "仓库", "保存地表采集和生产物资的标准库存节点。", new Vector2I(2, 2), 14f, 0, 2, 24, "storage");
        AddBuilding("fluid_tank", "储罐", "保存液体燃料和可压缩资源的设施接口。", new Vector2I(2, 2), 20f, 0, 3, 10, "fluid_storage");
        AddBuilding("solar_panel", "太阳能板", "稳定但受环境影响的基础发电设施。", new Vector2I(2, 2), 16f, 14, 0, 0, "power");
        AddBuilding("rocket_pad", "火箭组装坪", "固定大型建筑，用于组装、装载并发射回归火箭。", new Vector2I(5, 5), 90f, 0, 24, 18, "rocket", "cargo");
    }

    private void AddRecipes()
    {
        AddRecipe("recycle_scrap_to_metal", "废料回收金属", new[] { Stack("scrap", 4) }, new[] { Stack("metal", 3) }, 8f, 2, "crafting", "engineering");
        AddRecipe("craft_electronic_parts", "制造电子元件", new[] { Stack("metal", 2), Stack("silicon", 3) }, new[] { Stack("electronic_parts", 1) }, 12f, 5, "crafting", "engineering");
        AddRecipe("craft_alloy", "制造合金", new[] { Stack("metal", 3), Stack("rare_earth", 1) }, new[] { Stack("alloy", 1) }, 14f, 7, "crafting", "engineering");
        AddRecipe("craft_service_platform", "制造基础量产机平台", new[] { Stack("metal", 8), Stack("electronic_parts", 3), Stack("energy_cell", 2) }, new[] { Stack("service_bot_platform", 1) }, 24f, 10, "crafting", "engineering");
        AddRecipe("craft_rocket_part", "制造火箭部件", new[] { Stack("metal", 10), Stack("alloy", 4), Stack("electronic_parts", 2), Stack("energy_cell", 3) }, new[] { Stack("rocket_part", 1) }, 36f, 16, "rocket", "engineering");
    }

    private void AddSkills()
    {
        AddSkill("engineering", "工程", "影响建造、维修、组装和火箭制造效率。", "build", "repair", "rocket");
        AddSkill("shooting", "射击", "影响实弹和能量武器的命中稳定性。", "weapon", "combat");
        AddSkill("mining", "挖矿", "影响矿物采集、探矿和资源回收效率。", "mining", "salvage");
        AddSkill("control", "控制", "影响骇入、通信、扫描和协议操作。", "scan", "hack", "command");
    }

    private void AddEvents()
    {
        EventData eventData = new()
        {
            Id = "ruin_signal_cache",
            DisplayName = "废墟信号缓存",
            Description = "扫描到低功耗数据缓存，可能包含蓝图碎片或污染数据。",
            RiskLevel = 1,
            IconPath = $"{DataAssetRoot}/events/ruin_signal_cache.png"
        };
        eventData.TriggerTags.AddRange(new[] { "ruin", "scan" });
        eventData.RewardTables.Add("early_ruin_data");
        _events.Add(eventData.Id, eventData);
    }

    private void AddDropPods()
    {
        DropPodData pod = new()
        {
            Id = "drop_pod_single_use",
            DisplayName = "单程勘探舱",
            Description = "早期远征使用的基础空投舱，载荷有限但稳定。",
            WeightLimit = 120f,
            SlotLimit = 12,
            UnitCapacity = 2,
            IconPath = "res://assets/ui/drop/pods/drop_pod_single_use.png",
            SpritePath = "res://assets/sprites/drop_pods/drop_pod_single_use.png"
        };
        pod.AcceptedTags.AddRange(new[] { "drop_allowed", "mineral", "material", "tool", "weapon", "chip", "mod_part", "unit_platform", "building_module" });
        _dropPods.Add(pod.Id, pod);

        DropPodData cargoPod = new()
        {
            Id = "drop_pod_cargo_1",
            DisplayName = "轻型货运舱",
            Description = "面向正式远征的轻型货运舱，允许携带更多物资和量产平台。",
            WeightLimit = 220f,
            SlotLimit = 20,
            UnitCapacity = 4,
            IconPath = "res://assets/ui/drop/pods/drop_pod_cargo_1.png",
            SpritePath = "res://assets/sprites/drop_pods/drop_pod_cargo_1.png",
            RequiresBlueprintId = "blueprint_drop_pod_capacity_1"
        };
        cargoPod.RequiresProtocolIds.Add("protocol_drop_mass_audit_1");
        cargoPod.AcceptedTags.AddRange(new[] { "drop_allowed", "mineral", "material", "tool", "weapon", "chip", "mod_part", "unit_platform", "building_module" });
        _dropPods.Add(cargoPod.Id, cargoPod);
    }

    private void AddKnownCoordinates()
    {
        KnownCoordinate coordinate = new()
        {
            CoordinateId = "coord_scrap_plain_01",
            DisplayName = "废料平原 01",
            RegionType = "基础废料区",
            SeedHint = 460001,
            RiskLevel = 1,
            DropPosition = new Vector2I(184, -72),
            IsRevisitable = true,
            IconPath = "res://assets/ui/drop/coordinates/coord_scrap_plain_01.png"
        };
        coordinate.MineralTags.AddRange(new[] { "scrap", "metal", "silicon" });
        _knownCoordinates.Add(coordinate.CoordinateId, coordinate);
    }

    private DataLoadReport ValidateDefinitions()
    {
        DataLoadReport report = new();
        ValidateItemReferences(report);
        ValidateRecipeReferences(report);
        ValidateDropPods(report);
        ValidateKnownCoordinates(report);
        return report;
    }

    private void ValidateItemReferences(DataLoadReport report)
    {
        foreach (ItemData item in _items.Values)
        {
            ValidateAsset(report, item.IconPath, $"道具 {item.Id} 缺少图标");
            ValidateAsset(report, item.WorldSpritePath, $"道具 {item.Id} 缺少地表资源");
            if (item.Category == "unit_platform")
            {
                if (string.IsNullOrWhiteSpace(item.TargetUnitId))
                {
                    report.Add(DefinitionStatus.FatalError, $"单位平台 {item.Id} 缺少目标单位");
                }
                else if (!_units.ContainsKey(item.TargetUnitId))
                {
                    report.Add(DefinitionStatus.FatalError, $"单位平台 {item.Id} 引用缺失目标单位：{item.TargetUnitId}");
                }
            }
        }

        foreach (UnitData unit in _units.Values)
        {
            ValidateAsset(report, unit.IconPath, $"单位 {unit.Id} 缺少图标");
            ValidateAsset(report, unit.SpritePath, $"单位 {unit.Id} 缺少地表精灵");
            ValidateAsset(report, unit.PortraitPath, $"单位 {unit.Id} 缺少头像");
        }

        foreach (BuildingData building in _buildings.Values)
        {
            ValidateAsset(report, building.IconPath, $"建筑 {building.Id} 缺少图标");
            ValidateAsset(report, building.SpritePath, $"建筑 {building.Id} 缺少地表精灵");
        }

        foreach (SkillData skill in _skills.Values)
        {
            ValidateAsset(report, skill.IconPath, $"技能 {skill.Id} 缺少图标");
        }

        foreach (EventData eventData in _events.Values)
        {
            ValidateAsset(report, eventData.IconPath, $"事件 {eventData.Id} 缺少图标");
        }

        foreach (KnownCoordinate coordinate in _knownCoordinates.Values)
        {
            ValidateAsset(report, coordinate.IconPath, $"坐标 {coordinate.CoordinateId} 缺少图标");
        }
    }

    private void ValidateRecipeReferences(DataLoadReport report)
    {
        foreach (RecipeData recipe in _recipes.Values)
        {
            foreach (ItemStack stack in recipe.InputItems)
            {
                if (!_items.ContainsKey(stack.ItemId))
                {
                    report.Add(DefinitionStatus.FatalError, $"配方 {recipe.Id} 输入引用缺失道具：{stack.ItemId}");
                }
            }

            foreach (ItemStack stack in recipe.OutputItems)
            {
                if (!_items.ContainsKey(stack.ItemId))
                {
                    report.Add(DefinitionStatus.FatalError, $"配方 {recipe.Id} 输出引用缺失道具：{stack.ItemId}");
                }
            }
        }
    }

    private void ValidateDropPods(DataLoadReport report)
    {
        foreach (DropPodData pod in _dropPods.Values)
        {
            ValidateAsset(report, pod.IconPath, $"空投舱 {pod.Id} 缺少图标");
            ValidateAsset(report, pod.SpritePath, $"空投舱 {pod.Id} 缺少地表精灵");
            if (pod.WeightLimit <= 0f)
            {
                report.Add(DefinitionStatus.FatalError, $"空投舱 {pod.Id} 缺少有效载重");
            }
        }
    }

    private void ValidateKnownCoordinates(DataLoadReport report)
    {
        foreach (KnownCoordinate coordinate in _knownCoordinates.Values)
        {
            if (coordinate.RiskLevel < 0)
            {
                report.Add(DefinitionStatus.RecoverableError, $"坐标 {coordinate.CoordinateId} 风险等级非法");
            }
        }
    }

    private static void ValidateAsset(DataLoadReport report, string path, string message)
    {
        if (string.IsNullOrWhiteSpace(path) || !FileAccess.FileExists(path))
        {
            report.Add(DefinitionStatus.RecoverableError, message);
        }
    }

    private void AddItem(string id, string displayName, string category, string subCategory, string description, float weight, int value, params string[] tags)
    {
        AddItem(id, displayName, category, subCategory, description, weight, value, 0, string.Empty, tags);
    }

    private void AddItem(string id, string displayName, string category, string subCategory, string description, float weight, int value, int maxStack, string targetUnitId, params string[] tags)
    {
        ItemData item = new()
        {
            Id = id,
            DisplayName = displayName,
            Category = category,
            SubCategory = subCategory,
            Description = description,
            IconPath = $"res://assets/ui/icons/items/{id}.png",
            WorldSpritePath = $"res://assets/sprites/items/{id}.png",
            TargetUnitId = targetUnitId,
            UnitWeight = weight,
            BaseValue = value,
            MaxStack = maxStack > 0 ? maxStack : category is "weapon" or "tool" or "ai_chip" or "data_core" ? 1 : 100
        };
        item.Tags.AddRange(tags);
        item.Tags.Add(category);
        _items.Add(id, item);
    }

    private void AddUnit(string id, string displayName, string description, string role, string movementType, int durability, int energy, int capacity, float weightLimit, bool awakened, params string[] slots)
    {
        UnitData unit = new()
        {
            Id = id,
            DisplayName = displayName,
            Description = description,
            UnitRole = role,
            MovementType = movementType,
            BaseDurability = durability,
            BaseEnergy = energy,
            InventoryCapacity = capacity,
            CarryWeightLimit = weightLimit,
            IsAwakenedCapable = awakened,
            IconPath = $"{DataAssetRoot}/units/{id}.png",
            PortraitPath = $"{DataAssetRoot}/units/{id}.png",
            SpritePath = $"{DataAssetRoot}/units/{id}.png"
        };
        unit.EquipmentSlots.AddRange(slots);
        unit.AvailableCommands.AddRange(new[] { "move", "hold", "gather", "repair" });
        unit.Tags.AddRange(new[] { role, movementType });
        _units.Add(id, unit);
    }

    private void AddBuilding(string id, string displayName, string description, Vector2I footprint, float buildTime, int powerGeneration, int powerConsumption, int storageCapacity, params string[] tags)
    {
        BuildingData building = new()
        {
            Id = id,
            DisplayName = displayName,
            Description = description,
            Footprint = footprint,
            BuildTime = buildTime,
            PowerGeneration = powerGeneration,
            PowerConsumption = powerConsumption,
            StorageCapacity = storageCapacity,
            IconPath = $"{DataAssetRoot}/buildings/{id}.png",
            SpritePath = $"{DataAssetRoot}/buildings/{id}.png",
            PreviewSpritePath = $"{DataAssetRoot}/buildings/{id}.png",
            ConstructionSpritePath = $"{DataAssetRoot}/buildings/{id}.png",
            DamagedSpritePath = $"{DataAssetRoot}/buildings/{id}.png"
        };
        building.BuildCost.AddRange(new[] { Stack("metal", footprint.X * footprint.Y * 4), Stack("electronic_parts", 1) });
        building.FunctionTags.AddRange(tags);
        _buildings.Add(id, building);
    }

    private void AddRecipe(string id, string displayName, IEnumerable<ItemStack> inputs, IEnumerable<ItemStack> outputs, float workTime, int powerCost, string buildingTag, string skillId)
    {
        RecipeData recipe = new()
        {
            Id = id,
            DisplayName = displayName,
            WorkTime = workTime,
            PowerCost = powerCost,
            OperatorSkillId = skillId
        };
        recipe.InputItems.AddRange(inputs);
        recipe.OutputItems.AddRange(outputs);
        recipe.RequiredBuildingTags.Add(buildingTag);
        _recipes.Add(id, recipe);
    }

    private void AddSkill(string id, string displayName, string description, params string[] effectTags)
    {
        SkillData skill = new()
        {
            Id = id,
            DisplayName = displayName,
            Description = description,
            IconPath = $"{DataAssetRoot}/skills/{id}.png"
        };
        skill.ExperienceThresholds.AddRange(new[] { 0, 100, 260, 520, 900, 1400 });
        skill.EffectTags.AddRange(effectTags);
        _skills.Add(id, skill);
    }

    private static ItemStack Stack(string itemId, int count)
    {
        return new ItemStack
        {
            ItemId = itemId,
            Count = count
        };
    }
}
