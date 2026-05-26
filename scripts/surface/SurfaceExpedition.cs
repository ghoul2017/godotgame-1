using Godot;

namespace GodotGame;

public partial class SurfaceExpedition : Node2D, ScenePayloadReceiver
{
    private Label? _statusLabel;
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
        if (_statusLabel is not null)
        {
            string expeditionId = payload.Data.TryGetValue("expedition_id", out Variant id) ? id.AsString() : "unknown";
            _statusLabel.Text = $"地表远征\n远征：{expeditionId}\n种子：{payload.Seed}";
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

        Control root = new()
        {
            Name = "SurfaceLayout"
        };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        uiLayer.AddChild(root);

        VBoxContainer panel = new()
        {
            Name = "StatusPanel"
        };
        panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        panel.MouseEntered += () => FindGameRoot()?.InputIntentController.SetUiBlocked(true);
        panel.MouseExited += () => FindGameRoot()?.InputIntentController.SetUiBlocked(false);
        root.AddChild(panel);

        _statusLabel = new Label
        {
            Text = "地表远征"
        };
        panel.AddChild(_statusLabel);

        Label resourceBar = new()
        {
            Text = "资源栏"
        };
        panel.AddChild(resourceBar);

        Label minimap = new()
        {
            Text = "小地图"
        };
        panel.AddChild(minimap);

        Label selectionPanel = new()
        {
            Text = "单位 / 建筑信息"
        };
        panel.AddChild(selectionPanel);

        Label commandPanel = new()
        {
            Text = "命令区"
        };
        panel.AddChild(commandPanel);

        Label messagePanel = new()
        {
            Text = "消息和事件"
        };
        panel.AddChild(messagePanel);

        Button returnButton = new()
        {
            Text = "模拟火箭回归"
        };
        returnButton.Pressed += () =>
        {
            GameRoot? gameRoot = FindGameRoot();
            if (gameRoot is null)
            {
                return;
            }

            ScenePayload returnPayload = CreateReturnPayload(gameRoot);
            gameRoot.NavigateTo(SceneId.ReturnSummary, returnPayload);
        };
        panel.AddChild(returnButton);

        Button backButton = new()
        {
            Text = "返回主入口"
        };
        backButton.Pressed += () => FindGameRoot()?.ShowMainMenu();
        panel.AddChild(backButton);

        CanvasLayer debugLayer = new()
        {
            Name = "DebugOverlay"
        };
        AddChild(debugLayer);
    }

    private ScenePayload CreateReturnPayload(GameRoot gameRoot)
    {
        ScenePayload payload = _payload ?? gameRoot.CreateDefaultPayload(SceneId.SurfaceExpedition, SceneId.ReturnSummary);
        ScenePayload returnPayload = new()
        {
            FromScene = SceneId.SurfaceExpedition,
            TargetScene = SceneId.ReturnSummary,
            PayloadType = "surface_return_summary",
            DebugEnabled = payload.DebugEnabled,
            Seed = payload.Seed
        };

        if (payload.Data.TryGetValue("expedition_id", out Variant expeditionId))
        {
            returnPayload.Data["expedition_id"] = expeditionId;
        }

        returnPayload.ReturnCargo.Add(new ItemStack { ItemId = "metal", Count = 25 });
        returnPayload.ReturnCargo.Add(new ItemStack { ItemId = "energy_cell", Count = 10 });
        returnPayload.DiscoveredIds.Add("blueprint_basic_rocket_pad");
        return returnPayload;
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
