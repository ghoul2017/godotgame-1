using Godot;

namespace GodotGame;

public partial class GameRoot : Control
{
    private readonly DebugLauncher _debugLauncher = new();
    private GameSession _session = new();
    private readonly InputIntentController _inputIntentController = new();
    private SceneRouter? _sceneRouter;
    private Control? _mainMenu;
    private Button? _continueButton;
    private Node? _sceneContainer;

    public GameSession Session => _session;
    public InputIntentController InputIntentController => _inputIntentController;

    public override void _Ready()
    {
        GD.Print("[启动] 主入口初始化开始");
        InputActions.EnsureConfigured();
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
            else if (debugScene == SceneId.Prologue)
            {
                payload.NavigationData ??= new NavigationPayloadData();
                payload.NavigationData.PrologueNodeId = _debugLauncher.GetPrologueNode();
            }

            NavigateTo(debugScene, payload);
            return;
        }

        ShowMainMenu();
        GD.Print("[启动] 主入口初始化完成");
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
        int seed = debugEnabled ? _debugLauncher.GetSeed() : 460001;
        ExpeditionState expeditionState = new()
        {
            ExpeditionId = $"expedition_{seed}",
            Seed = seed,
            DropPosition = new Vector2I(184, -72)
        };
        expeditionState.InitialUnits.Add(new UnitStack { UnitId = "unit_ling_qiao", Count = 1, ConfigId = "hero_ling_qiao_default" });
        expeditionState.InitialUnits.Add(new UnitStack { UnitId = "unit_light_cargo_drone", Count = 2, ConfigId = "cargo_drone_scout_loadout" });
        expeditionState.InitialItems.Add(new ItemStack { ItemId = "metal", Count = 100 });
        expeditionState.InitialItems.Add(new ItemStack { ItemId = "energy_cell", Count = 50 });
        expeditionState.InitialItems.Add(new ItemStack { ItemId = "field_repair_kit", Count = 3 });
        expeditionState.MapState.ExploredRegionIds.Add("drop_zone_ruined_array");
        _session.ActiveExpedition = expeditionState;

        ExpeditionStartPayloadData expeditionData = new()
        {
            ExpeditionId = expeditionState.ExpeditionId,
            Seed = seed,
            DropPosition = expeditionState.DropPosition
        };
        expeditionData.InitialUnits.AddRange(expeditionState.InitialUnits);
        expeditionData.InitialItems.AddRange(expeditionState.InitialItems);

        ScenePayload payload = new()
        {
            FromScene = fromScene,
            TargetScene = SceneId.SurfaceExpedition,
            PayloadType = "expedition_start",
            ExpeditionStartData = expeditionData,
            DebugEnabled = debugEnabled,
            Seed = seed
        };

        return payload;
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
        summaryData.ReturnedChipIds.Add("chip_recovery_protocol_mk1");
        summaryData.ReturnedBlueprintIds.Add("blueprint_basic_rocket_pad");
        summaryData.DiscoveredIds.Add("blueprint_basic_rocket_pad");

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

        AddMenuButton(menu, "开始序章", SceneId.Prologue, false);
        AddMenuButton(menu, "进入轨道站", SceneId.OrbitStation, false);

        _continueButton = new Button
        {
            Text = _session.RunRecords.Count == 0 ? "继续当前存档（无可继续记录）" : "继续当前存档",
            Disabled = _session.RunRecords.Count == 0
        };
        _continueButton.Pressed += () => NavigateTo(SceneId.OrbitStation, CreateNavigationPayload(SceneId.Main, SceneId.OrbitStation));
        menu.AddChild(_continueButton);

        if (_debugLauncher.IsDebugEnabled())
        {
            AddMenuButton(menu, "调试：进入地表远征", SceneId.SurfaceExpedition, true);
            AddMenuButton(menu, "调试：打开回归结算", SceneId.ReturnSummary, true);
        }

        Button quitButton = new()
        {
            Text = "退出游戏"
        };
        quitButton.Pressed += () => GetTree().Quit();
        menu.AddChild(quitButton);

        return panel;
    }

    private void RefreshContinueButton()
    {
        if (_continueButton is null)
        {
            return;
        }

        _continueButton.Text = _session.RunRecords.Count == 0 ? "继续当前存档（无可继续记录）" : "继续当前存档";
        _continueButton.Disabled = _session.RunRecords.Count == 0;
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

        RunRecord record = new()
        {
            ExpeditionId = summaryData.ExpeditionId,
            Seed = payload.Seed
        };
        record.BroughtItems.AddRange(summaryData.BroughtItems);
        record.ReturnedItems.AddRange(summaryData.ReturnCargo);
        record.ReturnedAwakenedUnitIds.AddRange(summaryData.ReturnedAwakenedUnitIds);
        record.ReturnedChipIds.AddRange(summaryData.ReturnedChipIds);
        record.ReturnedBlueprintIds.AddRange(summaryData.ReturnedBlueprintIds);
        record.LostUnits.AddRange(summaryData.LostUnits);
        record.LeftSurfaceAssetIds.AddRange(summaryData.LeftSurfaceAssetIds);
        record.DiscoveredIds.AddRange(summaryData.DiscoveredIds);

        foreach (ItemStack item in record.ReturnedItems)
        {
            _session.OrbitState.Inventory.TryGetValue(item.ItemId, out int currentCount);
            _session.OrbitState.Inventory[item.ItemId] = currentCount + item.Count;
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

        _session.RunRecords.Add(record);
        _session.ActiveExpedition = null;
        GD.Print($"[结算] 回归结果已写入轨道库存：{record.ExpeditionId}");
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
