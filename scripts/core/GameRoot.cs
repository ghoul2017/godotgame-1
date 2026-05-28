using System.Collections.Generic;
using Godot;

namespace GodotGame;

public partial class GameRoot : Control
{
    private static readonly Vector2I DefaultWindowSize = new(1600, 900);
    private static readonly Vector2I MinimumWindowSize = new(1280, 720);

    private readonly DebugLauncher _debugLauncher = new();
    private GameSession _session = new();
    private readonly DataRegistry _dataRegistry = new();
    private readonly InputIntentController _inputIntentController = new();
    private SceneRouter? _sceneRouter;
    private Control? _mainMenu;
    private Button? _continueButton;
    private Label? _mainMenuStatus;
    private Node? _sceneContainer;

    public GameSession Session => _session;
    public DataRegistry DataRegistry => _dataRegistry;
    public InputIntentController InputIntentController => _inputIntentController;

    public override void _Ready()
    {
        GD.Print("[启动] 主入口初始化开始");
        ConfigureMainWindow();
        InputActions.EnsureConfigured();
        DataLoadReport report = _dataRegistry.LoadBuiltInDefinitions();
        foreach (DataLoadIssue issue in report.Issues)
        {
            string line = $"[数据] {issue.Status}：{issue.Message}";
            if (issue.Status == DefinitionStatus.FatalError)
            {
                GD.PushError(line);
            }
            else
            {
                GD.PushWarning(line);
            }
        }

        BootstrapSessionData();
        Theme = UiAssets.CreateBaseTheme();
        BuildLayout();
        _sceneRouter = new SceneRouter(_sceneContainer!);

        string debugScene = _debugLauncher.GetStartupScene();
        if (_debugLauncher.IsDebugEnabled() && !string.IsNullOrEmpty(debugScene))
        {
            GD.Print($"[调试] 使用启动参数进入场景：{debugScene}");
            ScenePayload payload = debugScene == SceneId.SurfaceExpedition || debugScene == SceneId.ReturnSummary
                ? debugScene == SceneId.SurfaceExpedition
                    ? CreateExpeditionStartPayload(SceneId.Main, true)
                    : CreateDebugReturnSummaryPayload(SceneId.Main)
                : CreateNavigationPayload(SceneId.Main, debugScene, true);
            if (debugScene == SceneId.OrbitStation)
            {
                payload.NavigationData ??= new NavigationPayloadData();
                payload.NavigationData.OrbitPageId = _debugLauncher.GetOrbitPage();
            }

            NavigateTo(debugScene, payload);
            return;
        }

        ShowMainMenu();
        GD.Print("[启动] 主入口初始化完成");
    }

    private static void ConfigureMainWindow()
    {
        if (DisplayServer.GetName() == "headless")
        {
            return;
        }

        int mainWindowId = (int)DisplayServer.MainWindowId;
        DisplayServer.WindowSetMinSize(MinimumWindowSize, mainWindowId);
        Vector2I currentSize = DisplayServer.WindowGetSize(mainWindowId);
        if (currentSize.X < MinimumWindowSize.X || currentSize.Y < MinimumWindowSize.Y)
        {
            DisplayServer.WindowSetSize(DefaultWindowSize, mainWindowId);
        }
    }

    public void ShowMainMenu()
    {
        ClearSceneContainer();
        _session.CurrentState = "main_menu";
        RefreshContinueButton();
        _mainMenu!.Visible = true;
    }

    public void NavigateTo(string targetScene, ScenePayload payload)
    {
        _mainMenu!.Visible = false;
        payload.TargetScene = targetScene;
        _session.CurrentState = targetScene;
        _sceneRouter!.ChangeScene(targetScene, payload);
    }

    public ScenePayload CreateNavigationPayload(string fromScene, string targetScene, bool debugEnabled = false)
    {
        return new ScenePayload
        {
            FromScene = fromScene,
            TargetScene = targetScene,
            PayloadType = "navigation",
            NavigationData = new NavigationPayloadData(),
            DebugEnabled = debugEnabled,
            Seed = debugEnabled ? _debugLauncher.GetSeed() : 0
        };
    }

    public ScenePayload CreateExpeditionStartPayload(string fromScene, bool debugEnabled = false)
    {
        ExpeditionCreationService service = new(_session, _dataRegistry);
        DropConfigSession config = service.CreateDefaultDropConfig(debugEnabled ? _debugLauncher.GetSeed() : 0);
        if (service.TryCreateExpeditionStartPayload(config, fromScene, debugEnabled, out ScenePayload payload, out string message))
        {
            return payload;
        }

        GD.PushError($"[空投] {message}");
        return CreateNavigationPayload(fromScene, SceneId.OrbitStation, debugEnabled);
    }

    public bool TryStartExpeditionFromDropConfig(DropConfigSession config, string fromScene, bool debugEnabled, out string message)
    {
        ExpeditionCreationService service = new(_session, _dataRegistry);
        if (!service.TryCreateExpeditionStartPayload(config, fromScene, debugEnabled, out ScenePayload payload, out message))
        {
            GD.PushWarning($"[空投] {message}");
            return false;
        }

        NavigateTo(SceneId.SurfaceExpedition, payload);
        return true;
    }

    private ScenePayload CreateDebugReturnSummaryPayload(string fromScene)
    {
        int seed = _debugLauncher.GetSeed();
        ReturnSummaryPayloadData summaryData = new()
        {
            ExpeditionId = $"debug_return_{seed}"
        };
        summaryData.BroughtItems.Add(new ItemStack { ItemId = "metal", Count = 100 });
        summaryData.ReturnCargo.Add(new ItemStack { ItemId = "metal", Count = 25 });
        summaryData.ReturnCargo.Add(new ItemStack { ItemId = "energy_cell", Count = 10 });
        summaryData.ReturnedChipIds.Add("ai_chip_basic");
        summaryData.ReturnedBlueprintIds.Add("blueprint_rocket_pad_basic");
        summaryData.DiscoveredIds.Add("blueprint_rocket_pad_basic");

        return new ScenePayload
        {
            FromScene = fromScene,
            TargetScene = SceneId.ReturnSummary,
            PayloadType = "debug_return_summary",
            ReturnSummaryData = summaryData,
            DebugEnabled = true,
            Seed = seed
        };
    }

    private void BuildLayout()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        TextureRect background = UiAssets.CreateTextureRect("MainBackground", UiAssets.OrbitBackground);
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(background);

        VBoxContainer rootLayout = new()
        {
            Name = "RootLayout"
        };
        rootLayout.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(rootLayout);

        _mainMenu = BuildMainMenu();
        rootLayout.AddChild(_mainMenu);

        _sceneContainer = new Control
        {
            Name = "SceneContainer",
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        rootLayout.AddChild(_sceneContainer);
    }

    private Control BuildMainMenu()
    {
        PanelContainer panel = new()
        {
            Name = "MainMenu",
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        VBoxContainer menu = new()
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        panel.AddChild(menu);

        Label title = new()
        {
            Text = "轨道残骸",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        menu.AddChild(title);

        _continueButton = new Button
        {
            Text = "继续游戏",
            Disabled = _session.RunRecords.Count == 0,
            TooltipText = _session.RunRecords.Count == 0 ? "暂无可继续的存档记录" : "读取最近存档"
        };
        _continueButton.Pressed += () => NavigateTo(SceneId.OrbitStation, CreateNavigationPayload(SceneId.Main, SceneId.OrbitStation));
        menu.AddChild(_continueButton);

        Button newGameButton = new()
        {
            Text = "新游戏"
        };
        newGameButton.Pressed += StartNewGame;
        menu.AddChild(newGameButton);

        Button settingsButton = new()
        {
            Text = "设置"
        };
        settingsButton.Pressed += () => ShowMainMenuStatus("设置界面尚未接入。");
        menu.AddChild(settingsButton);

        if (_debugLauncher.IsDebugEnabled())
        {
            AddMenuButton(menu, "调试：轨道站", SceneId.OrbitStation, true);
            AddMenuButton(menu, "调试：进入地表远征", SceneId.SurfaceExpedition, true);
            AddMenuButton(menu, "调试：打开回归结算", SceneId.ReturnSummary, true);
        }

        Button quitButton = new()
        {
            Text = "退出游戏"
        };
        quitButton.Pressed += () => GetTree().Quit();
        menu.AddChild(quitButton);

        _mainMenuStatus = new Label
        {
            Text = string.Empty,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        menu.AddChild(_mainMenuStatus);

        return panel;
    }

    private void RefreshContinueButton()
    {
        if (_continueButton is null)
        {
            return;
        }

        _continueButton.Text = "继续游戏";
        _continueButton.Disabled = _session.RunRecords.Count == 0;
        _continueButton.TooltipText = _session.RunRecords.Count == 0 ? "暂无可继续的存档记录" : "读取最近存档";
    }

    private void StartNewGame()
    {
        ClearSceneContainer();
        _session = new GameSession();
        BootstrapSessionData();
        RefreshContinueButton();
        ScenePayload payload = CreateNavigationPayload(SceneId.Main, SceneId.OrbitStation);
        payload.NavigationData ??= new NavigationPayloadData();
        payload.NavigationData.OrbitPageId = OrbitPageId.Drop;
        GD.Print("[主入口] 新游戏会话已创建，进入轨道站空投页");
        NavigateTo(SceneId.OrbitStation, payload);
    }

    private void ShowMainMenuStatus(string message)
    {
        if (_mainMenuStatus is not null)
        {
            _mainMenuStatus.Text = message;
        }
    }

    private void AddMenuButton(VBoxContainer menu, string text, string targetScene, bool debugEnabled)
    {
        Button button = new()
        {
            Text = text
        };
        button.Pressed += () =>
        {
            ScenePayload payload = targetScene == SceneId.SurfaceExpedition || targetScene == SceneId.ReturnSummary
                ? targetScene == SceneId.SurfaceExpedition
                    ? CreateExpeditionStartPayload(SceneId.Main, debugEnabled)
                    : CreateDebugReturnSummaryPayload(SceneId.Main)
                : CreateNavigationPayload(SceneId.Main, targetScene, debugEnabled);
            NavigateTo(targetScene, payload);
        };
        menu.AddChild(button);
    }

    public void ApplyReturnSummary(ScenePayload payload)
    {
        ReturnSummaryPayloadData? summaryData = payload.ReturnSummaryData;
        if (summaryData is null)
        {
            GD.PushWarning("[结算] 缺少回归结算载荷，已忽略写回");
            return;
        }

        if (payload.DebugEnabled)
        {
            GD.PushWarning("[结算] 调试结算只允许预览，已拒绝写回轨道状态");
            return;
        }

        if (!payload.DebugEnabled)
        {
            if (payload.FromScene != SceneId.SurfaceExpedition ||
                _session.ActiveExpedition is null ||
                _session.ActiveExpedition.ExpeditionId != summaryData.ExpeditionId)
            {
                GD.PushWarning("[结算] 回归载荷来源与当前远征不匹配，已拒绝写回");
                return;
            }

            if (!_session.ActiveExpedition.RocketState.IsReadyToReturn ||
                !_session.ActiveExpedition.RocketState.LaunchConfirmed ||
                string.IsNullOrEmpty(_session.ActiveExpedition.RocketState.CargoInventoryId))
            {
                GD.PushWarning("[结算] 火箭尚未完成发射确认，已拒绝正式回归写回");
                return;
            }

            if (summaryData.ReturnCargo.Count == 0 && summaryData.ReturnedItemInstanceIds.Count == 0 && summaryData.ReturnedAwakenedUnitIds.Count == 0)
            {
                GD.PushWarning("[结算] 正式回归缺少有效货舱或单位内容，已拒绝写回");
                return;
            }

            foreach (string transferId in summaryData.RelatedTransferIds)
            {
                if (!_session.InventoryTransfers.Exists(transfer => transfer.TransferId == transferId))
                {
                    GD.PushWarning($"[结算] 回归载荷引用缺失转移记录，已拒绝写回：{transferId}");
                    return;
                }
            }

            if (!ValidateReturnSummary(summaryData))
            {
                return;
            }
        }

        if (_session.RunRecords.Exists(existing => existing.ExpeditionId == summaryData.ExpeditionId))
        {
            GD.PushWarning($"[结算] 远征已完成结算，拒绝重复写回：{summaryData.ExpeditionId}");
            return;
        }

        RunRecord record = new()
        {
            RunRecordId = string.IsNullOrEmpty(summaryData.RunRecordId) ? System.Guid.NewGuid().ToString("N") : summaryData.RunRecordId,
            ExpeditionId = summaryData.ExpeditionId,
            Seed = payload.Seed,
            ReturnReason = summaryData.ReturnReason
        };
        record.BroughtItems.AddRange(summaryData.BroughtItems);
        record.ReturnedItems.AddRange(summaryData.ReturnCargo);
        record.ReturnedItemInstanceIds.AddRange(summaryData.ReturnedItemInstanceIds);
        record.ReturnedAwakenedUnitIds.AddRange(summaryData.ReturnedAwakenedUnitIds);
        record.ReturnedChipIds.AddRange(summaryData.ReturnedChipIds);
        record.ReturnedBlueprintIds.AddRange(summaryData.ReturnedBlueprintIds);
        record.LostUnits.AddRange(summaryData.LostUnits);
        record.LeftSurfaceAssetIds.AddRange(summaryData.LeftSurfaceAssetIds);
        record.DiscoveredIds.AddRange(summaryData.DiscoveredIds);
        record.RelatedTransferIds.AddRange(summaryData.RelatedTransferIds);
        record.LeftBehindSummary = string.Join(", ", summaryData.LeftSurfaceAssetIds);
        if (_session.ActiveExpedition is not null)
        {
            record.TargetCoordinate = $"{_session.ActiveExpedition.DropPosition.X},{_session.ActiveExpedition.DropPosition.Y}";
            if (_session.DropPlans.TryGetValue(_session.ActiveExpedition.DropPlanId, out DropPlan? dropPlan))
            {
                record.DropPlanSnapshot = dropPlan;
            }
        }

        InventoryContainer orbitInventory = EnsureInventory(_session.OrbitState.InventoryId, "orbit_inventory", _session.OrbitState.OrbitStateId, 64, 2000f);
        InventoryContainer? rocketCargo = null;
        if (_session.ActiveExpedition is not null && !string.IsNullOrEmpty(_session.ActiveExpedition.RocketState.CargoInventoryId))
        {
            _session.Inventories.TryGetValue(_session.ActiveExpedition.RocketState.CargoInventoryId, out rocketCargo);
        }

        foreach (ItemStack item in record.ReturnedItems)
        {
            InventoryTransferResult addResult = rocketCargo is null
                ? orbitInventory.AddStack(item, _dataRegistry)
                : rocketCargo.TransferTo(orbitInventory, item.ItemId, item.Count, _dataRegistry, "return_settlement", record.ExpeditionId);
            if (addResult.IsSuccess && addResult.Transfer is not null)
            {
                addResult.Transfer.Reason = "return_settlement";
                addResult.Transfer.ExpeditionId = record.ExpeditionId;
                addResult.Transfer.RelatedRunRecordId = record.RunRecordId;
                _session.InventoryTransfers.Add(addResult.Transfer);
                record.RelatedTransferIds.Add(addResult.Transfer.TransferId);
            }
            else if (!addResult.IsSuccess)
            {
                GD.PushWarning($"[库存] 回归写入失败：{addResult.Message}");
                return;
            }
        }

        foreach (string itemInstanceId in record.ReturnedItemInstanceIds)
        {
            InventoryTransferResult instanceResult = rocketCargo is null
                ? orbitInventory.AddItemInstance(itemInstanceId, _session.ItemInstances, _dataRegistry)
                : rocketCargo.TransferItemInstanceTo(orbitInventory, itemInstanceId, _session.ItemInstances, _dataRegistry, "return_settlement", record.ExpeditionId);
            if (instanceResult.IsSuccess && instanceResult.Transfer is not null)
            {
                instanceResult.Transfer.Reason = "return_settlement";
                instanceResult.Transfer.ExpeditionId = record.ExpeditionId;
                instanceResult.Transfer.RelatedRunRecordId = record.RunRecordId;
                _session.InventoryTransfers.Add(instanceResult.Transfer);
                record.RelatedTransferIds.Add(instanceResult.Transfer.TransferId);
            }
            else if (!instanceResult.IsSuccess)
            {
                GD.PushWarning($"[库存] 回归实例写入失败：{instanceResult.Message}");
                return;
            }
        }

        foreach (string awakenedUnitId in record.ReturnedAwakenedUnitIds)
        {
            if (!_session.OrbitState.AwakenedUnits.Contains(awakenedUnitId))
            {
                _session.OrbitState.AwakenedUnits.Add(awakenedUnitId);
            }
        }

        foreach (string blueprintId in record.ReturnedBlueprintIds)
        {
            if (!_session.OrbitState.UnlockedBlueprints.Contains(blueprintId))
            {
                _session.OrbitState.UnlockedBlueprints.Add(blueprintId);
            }
        }

        foreach (string chipId in record.ReturnedChipIds)
        {
            if (!_session.OrbitState.StoredChipIds.Contains(chipId))
            {
                _session.OrbitState.StoredChipIds.Add(chipId);
            }
        }

        if (_session.ActiveExpedition is not null)
        {
            foreach (string unitInstanceId in _session.ActiveExpedition.ActiveUnitInstanceIds)
            {
                if (_session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance))
                {
                    unitInstance.LockedByExpeditionId = string.Empty;
                    unitInstance.CurrentCommand = string.Empty;
                }
            }
        }

        _session.RunRecords.Add(record);
        _session.OrbitState.CompletedRunRecordIds.Add(record.RunRecordId);
        _session.ActiveExpedition = null;
        GD.Print($"[结算] 回归结果已写入轨道库存：{record.ExpeditionId}");
    }

    private void BootstrapSessionData()
    {
        _session.OrbitState.Credits = 120;
        if (!_session.OrbitState.KnownCoordinates.Contains("coord_scrap_plain_01"))
        {
            _session.OrbitState.KnownCoordinates.Add("coord_scrap_plain_01");
        }

        InventoryContainer orbitInventory = EnsureInventory(_session.OrbitState.InventoryId, "orbit_inventory", _session.OrbitState.OrbitStateId, 64, 2000f);
        AddBootstrapStack(orbitInventory, "metal", 160);
        AddBootstrapStack(orbitInventory, "silicon", 80);
        AddBootstrapStack(orbitInventory, "energy_cell", 80);
        AddBootstrapStack(orbitInventory, "scrap", 60);
        AddBootstrapStack(orbitInventory, "electronic_parts", 30);
        AddBootstrapStack(orbitInventory, "clean_data", 12);
        AddBootstrapStack(orbitInventory, "alloy", 12);
        AddBootstrapStack(orbitInventory, "service_bot_platform", 2);
        AddBootstrapStack(orbitInventory, "light_cargo_drone_platform", 2);
        AddBootstrapStack(orbitInventory, "heavy_cargo_spider_platform", 1);
        AddBootstrapInstance(orbitInventory, "scanner_basic_001", "scanner_basic", 100, "standard");
        AddBootstrapInstance(orbitInventory, "repair_tool_basic_001", "repair_tool_basic", 100, "standard");
        AddBootstrapInstance(orbitInventory, "rifle_basic_001", "rifle_basic", 100, "worn");
        AddBootstrapInstance(orbitInventory, "ai_chip_basic_001", "ai_chip_basic", 100, "standard");
        AddBootstrapInstance(orbitInventory, "servo_mod_basic_001", "servo_mod_basic", 100, "standard");

        EnsureUnitInstance("unit_dexter", "dexter", "灵巧", true, _session.OrbitState.InventoryId);
        EnsureUnitInstance("unit_service_bot_01", "service_bot", "服务型量产机器人 01", false, string.Empty);
        EnsureUnitInstance("unit_drone_scout_01", "light_cargo_drone", "侦察无人机 01", false, string.Empty);
        EnsureUnitInstance("unit_drone_scout_02", "light_cargo_drone", "侦察无人机 02", false, string.Empty);
        if (!_session.OrbitState.AwakenedUnits.Contains("unit_dexter"))
        {
            _session.OrbitState.AwakenedUnits.Add("unit_dexter");
        }

        AddAvailableMassUnit("unit_service_bot_01");
        AddAvailableMassUnit("unit_drone_scout_01");
        AddAvailableMassUnit("unit_drone_scout_02");
    }

    private void AddAvailableMassUnit(string unitInstanceId)
    {
        if (!_session.OrbitState.AvailableMassUnitInstanceIds.Contains(unitInstanceId))
        {
            _session.OrbitState.AvailableMassUnitInstanceIds.Add(unitInstanceId);
        }
    }

    private void AddBootstrapStack(InventoryContainer inventory, string itemId, int count)
    {
        if (inventory.GetItemCount(itemId) > 0)
        {
            return;
        }

        InventoryTransferResult result = inventory.AddStack(new ItemStack { ItemId = itemId, Count = count }, _dataRegistry);
        if (!result.IsSuccess)
        {
            GD.PushWarning($"[库存] 初始轨道库存添加失败：{result.Message}");
        }
    }

    private void AddBootstrapInstance(InventoryContainer inventory, string instanceId, string itemId, int durability, string quality)
    {
        if (!_session.ItemInstances.ContainsKey(instanceId))
        {
            _session.ItemInstances[instanceId] = new ItemInstance
            {
                InstanceId = instanceId,
                ItemId = itemId,
                Durability = durability,
                Quality = quality
            };
        }

        if (inventory.ItemInstanceIds.Contains(instanceId))
        {
            return;
        }

        InventoryTransferResult result = inventory.AddItemInstance(instanceId, _session.ItemInstances, _dataRegistry);
        if (!result.IsSuccess)
        {
            GD.PushWarning($"[库存] 初始实例道具添加失败：{result.Message}");
        }
    }

    private void EnsureUnitInstance(string instanceId, string unitId, string displayName, bool awakened, string inventoryId)
    {
        if (_session.UnitInstances.ContainsKey(instanceId) || !_dataRegistry.TryGetUnit(unitId, out UnitData? unitData) || unitData is null)
        {
            return;
        }

        UnitInstance instance = new()
        {
            UnitInstanceId = instanceId,
            UnitId = unitId,
            DisplayNameOverride = displayName,
            IsAwakened = awakened,
            Durability = unitData.BaseDurability,
            Energy = unitData.BaseEnergy,
            InventoryId = inventoryId,
            BehaviorMode = unitData.DefaultBehaviorMode
        };
        _session.UnitInstances[instanceId] = instance;
    }

    private bool ValidateReturnSummary(ReturnSummaryPayloadData summaryData)
    {
        if (_session.ActiveExpedition is null ||
            !_session.Inventories.TryGetValue(_session.ActiveExpedition.RocketState.CargoInventoryId, out InventoryContainer? rocketCargo))
        {
            GD.PushWarning("[结算] 找不到当前火箭货舱，已拒绝写回");
            return false;
        }

        InventoryContainer orbitInventory = EnsureInventory(_session.OrbitState.InventoryId, "orbit_inventory", _session.OrbitState.OrbitStateId, 64, 2000f);
        InventoryContainer simulatedOrbit = CloneInventory(orbitInventory, "orbit_return_validation");
        Dictionary<string, int> returnedStacks = new();
        foreach (ItemStack stack in summaryData.ReturnCargo)
        {
            returnedStacks.TryGetValue(stack.ItemId, out int currentCount);
            returnedStacks[stack.ItemId] = currentCount + stack.Count;
        }

        foreach (KeyValuePair<string, int> returnedStack in returnedStacks)
        {
            if (rocketCargo.GetItemCount(returnedStack.Key) < returnedStack.Value)
            {
                GD.PushWarning($"[结算] 火箭货舱缺少返回物资：{returnedStack.Key}");
                return false;
            }

            InventoryTransferResult fitResult = simulatedOrbit.AddStack(new ItemStack { ItemId = returnedStack.Key, Count = returnedStack.Value }, _dataRegistry);
            if (!fitResult.IsSuccess)
            {
                GD.PushWarning($"[结算] 轨道库存容量校验失败：{fitResult.Message}");
                return false;
            }
        }

        foreach (string itemInstanceId in summaryData.ReturnedItemInstanceIds)
        {
            if (!rocketCargo.ItemInstanceIds.Contains(itemInstanceId))
            {
                GD.PushWarning($"[结算] 火箭货舱缺少返回实例：{itemInstanceId}");
                return false;
            }

            InventoryTransferResult fitResult = simulatedOrbit.AddItemInstance(itemInstanceId, _session.ItemInstances, _dataRegistry);
            if (!fitResult.IsSuccess)
            {
                GD.PushWarning($"[结算] 轨道库存实例容量校验失败：{fitResult.Message}");
                return false;
            }
        }

        HashSet<string> returnedAwakenedUnits = new();
        foreach (string unitInstanceId in summaryData.ReturnedAwakenedUnitIds)
        {
            if (!returnedAwakenedUnits.Add(unitInstanceId) ||
                !_session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance) ||
                !unitInstance.IsAwakened ||
                !_session.ActiveExpedition.ActiveUnitInstanceIds.Contains(unitInstanceId) ||
                !_session.ActiveExpedition.RocketState.ReturningAwakenedUnitIds.Contains(unitInstanceId))
            {
                GD.PushWarning($"[结算] 返回觉醒者实例无效：{unitInstanceId}");
                return false;
            }
        }

        return true;
    }

    private static InventoryContainer CloneInventory(InventoryContainer source, string inventoryId)
    {
        InventoryContainer clone = new()
        {
            InventoryId = inventoryId,
            OwnerType = source.OwnerType,
            OwnerId = source.OwnerId,
            SlotLimit = source.SlotLimit,
            WeightLimit = source.WeightLimit
        };
        clone.AcceptedTags.AddRange(source.AcceptedTags);
        clone.BlockedTags.AddRange(source.BlockedTags);
        foreach (ItemStack stack in source.ItemStacks)
        {
            clone.ItemStacks.Add(new ItemStack
            {
                ItemId = stack.ItemId,
                Count = stack.Count
            });
        }

        clone.ItemInstanceIds.AddRange(source.ItemInstanceIds);
        return clone;
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

    private void ClearSceneContainer()
    {
        if (_sceneContainer is null)
        {
            return;
        }

        foreach (Node child in _sceneContainer.GetChildren())
        {
            child.QueueFree();
        }
    }
}
