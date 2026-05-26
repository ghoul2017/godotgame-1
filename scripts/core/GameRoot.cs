using Godot;

namespace GodotGame;

public partial class GameRoot : Control
{
    private readonly DebugLauncher _debugLauncher = new();
    private GameSession _session = new();
    private readonly InputIntentController _inputIntentController = new();
    private SceneRouter? _sceneRouter;
    private Control? _mainMenu;
    private Node? _sceneContainer;

    public GameSession Session => _session;
    public InputIntentController InputIntentController => _inputIntentController;

    public override void _Ready()
    {
        GD.Print("[启动] 主入口初始化开始");
        InputActions.EnsureConfigured();
        BuildLayout();
        _sceneRouter = new SceneRouter(_sceneContainer!);

        string debugScene = _debugLauncher.GetStartupScene();
        if (_debugLauncher.IsDebugEnabled() && !string.IsNullOrEmpty(debugScene))
        {
            GD.Print($"[调试] 使用启动参数进入场景：{debugScene}");
            ScenePayload payload = debugScene == SceneId.SurfaceExpedition || debugScene == SceneId.ReturnSummary
                ? CreateDefaultPayload(SceneId.Main, debugScene, true)
                : CreateNavigationPayload(SceneId.Main, debugScene, true);
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
            DebugEnabled = debugEnabled,
            Seed = debugEnabled ? _debugLauncher.GetSeed() : 0
        };
    }

    public ScenePayload CreateDefaultPayload(string fromScene, string targetScene, bool debugEnabled = false)
    {
        int seed = debugEnabled ? _debugLauncher.GetSeed() : 460001;
        ExpeditionState expeditionState = new()
        {
            ExpeditionId = $"expedition_{seed}",
            Seed = seed,
            DropPosition = new Vector2I(0, 0)
        };
        expeditionState.InitialUnits.Add(new UnitStack { UnitId = "unit_ling_qiao", Count = 1, ConfigId = "hero_ling_qiao_default" });
        expeditionState.InitialItems.Add(new ItemStack { ItemId = "metal", Count = 100 });
        expeditionState.InitialItems.Add(new ItemStack { ItemId = "energy_cell", Count = 50 });
        _session.ActiveExpedition = expeditionState;

        ScenePayload payload = new()
        {
            FromScene = fromScene,
            TargetScene = targetScene,
            PayloadType = "default_navigation",
            DebugEnabled = debugEnabled,
            Seed = seed
        };
        payload.Data["expedition_id"] = expeditionState.ExpeditionId;
        payload.Data["seed"] = seed;
        payload.Data["drop_x"] = expeditionState.DropPosition.X;
        payload.Data["drop_y"] = expeditionState.DropPosition.Y;
        return payload;
    }

    private void BuildLayout()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

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
            Text = "轨道残骸：远征骨架",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        menu.AddChild(title);

        AddMenuButton(menu, "开始序章", SceneId.Prologue, false);
        AddMenuButton(menu, "进入轨道站", SceneId.OrbitStation, false);

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

    private void AddMenuButton(VBoxContainer menu, string text, string targetScene, bool debugEnabled)
    {
        Button button = new()
        {
            Text = text
        };
        button.Pressed += () =>
        {
            ScenePayload payload = targetScene == SceneId.SurfaceExpedition || targetScene == SceneId.ReturnSummary
                ? CreateDefaultPayload(SceneId.Main, targetScene, debugEnabled)
                : CreateNavigationPayload(SceneId.Main, targetScene, debugEnabled);
            NavigateTo(targetScene, payload);
        };
        menu.AddChild(button);
    }

    public void ApplyReturnSummary(ScenePayload payload)
    {
        RunRecord record = new()
        {
            ExpeditionId = payload.Data.TryGetValue("expedition_id", out Variant expeditionId) ? expeditionId.AsString() : string.Empty,
            Seed = payload.Seed
        };
        record.ReturnedItems.AddRange(payload.ReturnCargo);
        record.LostUnits.AddRange(payload.LostUnits);
        record.DiscoveredIds.AddRange(payload.DiscoveredIds);

        foreach (ItemStack item in record.ReturnedItems)
        {
            _session.OrbitState.Inventory.TryGetValue(item.ItemId, out int currentCount);
            _session.OrbitState.Inventory[item.ItemId] = currentCount + item.Count;
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
