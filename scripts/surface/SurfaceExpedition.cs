using Godot;

namespace GodotGame;

public partial class SurfaceExpedition : Node2D, ScenePayloadReceiver
{
    private Label? _statusLabel;
    private Label? _manifestLabel;
    private ScenePayload? _payload;

    public override void _UnhandledInput(InputEvent @event)
    {
        InputIntentController? inputController = FindGameRoot()?.InputIntentController;
        if (inputController is null)
        {
            return;
        }

        InputIntentController.SurfaceIntent intent = inputController.GetSurfaceIntent(@event);
        if (intent == InputIntentController.SurfaceIntent.SelectPrimary)
        {
            GD.Print("[输入] 地表选择意图");
            GetViewport().SetInputAsHandled();
        }
        else if (intent == InputIntentController.SurfaceIntent.CommandContext)
        {
            GD.Print("[输入] 地表上下文指令意图");
            GetViewport().SetInputAsHandled();
        }
        else if (intent == InputIntentController.SurfaceIntent.Cancel)
        {
            GD.Print("[输入] 地表取消意图");
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _ExitTree()
    {
        FindGameRoot()?.InputIntentController.SetUiBlocked(false);
    }

    public override void _Ready()
    {
        BuildUi();
    }

    public void ReceivePayload(ScenePayload payload)
    {
        _payload = payload;
        ExpeditionStartPayloadData? expeditionData = payload.ExpeditionStartData;
        if (_statusLabel is not null && expeditionData is not null)
        {
            _statusLabel.Text = $"远征 {expeditionData.ExpeditionId}  |  种子 {expeditionData.Seed}  |  空投坐标 {expeditionData.DropPosition.X},{expeditionData.DropPosition.Y}";
        }

        if (_manifestLabel is not null && expeditionData is not null)
        {
            string units = expeditionData.InitialUnits.Count == 0 ? "无" : string.Join("  ", expeditionData.InitialUnits.ConvertAll(unit => $"{unit.UnitId} x{unit.Count} [{unit.ConfigId}]"));
            string items = expeditionData.InitialItems.Count == 0 ? "无" : string.Join("  ", expeditionData.InitialItems.ConvertAll(item => $"{item.ItemId} x{item.Count}"));
            _manifestLabel.Text = $"初始单位：{units}\n携带物资：{items}";
        }

        GD.Print($"[远征] 进入地表远征，种子：{payload.Seed}");
    }

    private void BuildUi()
    {
        Node2D mapLayer = new()
        {
            Name = "MapLayer"
        };
        AddChild(mapLayer);

        Node2D unitLayer = new()
        {
            Name = "UnitLayer"
        };
        AddChild(unitLayer);

        Node2D buildingLayer = new()
        {
            Name = "BuildingLayer"
        };
        AddChild(buildingLayer);

        Node2D effectLayer = new()
        {
            Name = "ProjectileEffectLayer"
        };
        AddChild(effectLayer);

        CanvasLayer uiLayer = new()
        {
            Name = "SurfaceUi"
        };
        AddChild(uiLayer);

        TextureRect background = UiAssets.CreateTextureRect("SurfaceBackground", UiAssets.SurfaceBackground);
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        uiLayer.AddChild(background);

        Control root = new()
        {
            Name = "SurfaceLayout"
        };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        uiLayer.AddChild(root);

        VBoxContainer topBar = new()
        {
            Name = "TopResourceBar"
        };
        topBar.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        RegisterUiInputBlocker(topBar);
        root.AddChild(topBar);

        _statusLabel = new Label
        {
            Text = "地表远征",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        topBar.AddChild(_statusLabel);

        Label resourceBar = new()
        {
            Text = "金属 0  |  硅 0  |  稀土 0  |  能源块 0  |  废料 0",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        topBar.AddChild(resourceBar);

        PanelContainer minimapPanel = new()
        {
            Name = "MinimapPanel",
            CustomMinimumSize = new Vector2(240, 180)
        };
        minimapPanel.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        minimapPanel.OffsetLeft = 16;
        minimapPanel.OffsetBottom = -16;
        minimapPanel.OffsetTop = -196;
        root.AddChild(minimapPanel);
        RegisterUiInputBlocker(minimapPanel);

        VBoxContainer minimapBox = new();
        minimapPanel.AddChild(minimapBox);
        TextureRect minimapIcon = UiAssets.CreateTextureRect("MinimapIcon", UiAssets.IconMinimap);
        minimapIcon.CustomMinimumSize = new Vector2(48, 48);
        minimapBox.AddChild(minimapIcon);
        minimapBox.AddChild(UiAssets.CreateSectionLabel("区域扫描图"));

        PanelContainer bottomPanel = new()
        {
            Name = "SelectionPanel",
            CustomMinimumSize = new Vector2(560, 150)
        };
        bottomPanel.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        bottomPanel.OffsetLeft = 280;
        bottomPanel.OffsetRight = -280;
        bottomPanel.OffsetBottom = -16;
        bottomPanel.OffsetTop = -166;
        root.AddChild(bottomPanel);
        RegisterUiInputBlocker(bottomPanel);

        VBoxContainer selectionBox = new();
        bottomPanel.AddChild(selectionBox);
        selectionBox.AddChild(UiAssets.CreateSectionLabel("单位 / 建筑信息"));
        _manifestLabel = new Label
        {
            Text = "初始单位：等待远征载荷\n携带物资：等待远征载荷",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        selectionBox.AddChild(_manifestLabel);

        GridContainer commandPanel = new()
        {
            Name = "CommandPanel",
            Columns = 2
        };
        commandPanel.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        commandPanel.OffsetLeft = -250;
        commandPanel.OffsetBottom = -16;
        commandPanel.OffsetTop = -166;
        commandPanel.OffsetRight = -16;
        root.AddChild(commandPanel);
        RegisterUiInputBlocker(commandPanel);

        AddCommandButton(commandPanel, "回归", UiAssets.IconCommand, () =>
        {
            GameRoot? gameRoot = FindGameRoot();
            if (gameRoot is null)
            {
                return;
            }

            ScenePayload returnPayload = CreateReturnPayload(gameRoot);
            gameRoot.NavigateTo(SceneId.ReturnSummary, returnPayload);
        });

        AddCommandButton(commandPanel, "取消", UiAssets.IconCommand, () => GD.Print("[输入] 命令区取消"));

        VBoxContainer messagePanel = new()
        {
            Name = "MessagePanel"
        };
        messagePanel.SetAnchorsPreset(Control.LayoutPreset.CenterRight);
        messagePanel.OffsetLeft = -280;
        messagePanel.OffsetRight = -16;
        messagePanel.OffsetTop = -120;
        messagePanel.OffsetBottom = 120;
        root.AddChild(messagePanel);
        RegisterUiInputBlocker(messagePanel);
        messagePanel.AddChild(UiAssets.CreateSectionLabel("消息和事件"));
        messagePanel.AddChild(new Label { Text = "当前没有必须立即响应的事件。", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        Button backButton = new()
        {
            Text = "返回主入口"
        };
        backButton.Pressed += () => FindGameRoot()?.ShowMainMenu();
        topBar.AddChild(backButton);

        CanvasLayer debugLayer = new()
        {
            Name = "DebugOverlay"
        };
        AddChild(debugLayer);
    }

    private ScenePayload CreateReturnPayload(GameRoot gameRoot)
    {
        ScenePayload returnPayload = new()
        {
            FromScene = SceneId.SurfaceExpedition,
            TargetScene = SceneId.ReturnSummary,
            PayloadType = "surface_return_summary",
            DebugEnabled = _payload?.DebugEnabled ?? false,
            Seed = _payload?.Seed ?? gameRoot.Session.ActiveExpedition?.Seed ?? 0
        };

        ExpeditionState? expeditionState = gameRoot.Session.ActiveExpedition;
        ReturnSummaryPayloadData summaryData = new();
        if (expeditionState is not null)
        {
            summaryData.ExpeditionId = expeditionState.ExpeditionId;
            summaryData.BroughtItems.AddRange(expeditionState.InitialItems);
            summaryData.ReturnCargo.AddRange(expeditionState.RocketState.CargoItems);
            summaryData.ReturnedAwakenedUnitIds.AddRange(expeditionState.RocketState.ReturningAwakenedUnitIds);
            summaryData.ReturnedChipIds.AddRange(expeditionState.RocketState.ReturningChipIds);
            summaryData.ReturnedBlueprintIds.AddRange(expeditionState.RocketState.ReturningBlueprintIds);
            summaryData.LeftSurfaceAssetIds.AddRange(expeditionState.MapState.LeftAssetIds);
            summaryData.DiscoveredIds.AddRange(expeditionState.RocketState.ReturningBlueprintIds);
        }

        returnPayload.ReturnSummaryData = summaryData;
        return returnPayload;
    }

    private void RegisterUiInputBlocker(Control control)
    {
        control.MouseFilter = Control.MouseFilterEnum.Stop;
        control.MouseEntered += () => FindGameRoot()?.InputIntentController.SetUiBlocked(true);
        control.MouseExited += () => FindGameRoot()?.InputIntentController.SetUiBlocked(false);
        control.GuiInput += inputEvent =>
        {
            if (inputEvent is InputEventMouseButton)
            {
                GetViewport().SetInputAsHandled();
            }
        };
    }

    private static void AddCommandButton(GridContainer commandPanel, string text, string iconPath, System.Action pressed)
    {
        Button button = new()
        {
            Text = text,
            Icon = UiAssets.LoadTexture(iconPath),
            CustomMinimumSize = new Vector2(110, 56)
        };
        button.Pressed += pressed;
        commandPanel.AddChild(button);
    }

    private GameRoot? FindGameRoot()
    {
        Node? node = this;
        while (node is not null)
        {
            if (node is GameRoot gameRoot)
            {
                return gameRoot;
            }

            node = node.GetParent();
        }

        return null;
    }
}
