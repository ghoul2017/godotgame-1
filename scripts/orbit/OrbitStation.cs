using Godot;

namespace GodotGame;

public partial class OrbitStation : Control, ScenePayloadReceiver
{
    [Signal]
    public delegate void PageChangedEventHandler(string pageId);

    private Label? _pageTitle;
    private Label? _pageBody;
    private Label? _payloadLabel;
    private ScenePayload? _payload;

    public override void _Ready()
    {
        BuildUi();
        OpenPage("inventory");
    }

    public void ReceivePayload(ScenePayload payload)
    {
        _payload = payload;
        if (_payloadLabel is not null)
        {
            _payloadLabel.Text = $"来源：{payload.FromScene}  种子：{payload.Seed}";
        }

        if (!string.IsNullOrEmpty(payload.NavigationData?.OrbitPageId))
        {
            OpenPage(payload.NavigationData.OrbitPageId);
        }
    }

    public void OpenPage(string pageId)
    {
        if (_pageTitle is not null)
        {
            _pageTitle.Text = pageId switch
            {
                "inventory" => "库存",
                "trade" => "交易",
                "research" => "研发",
                "characters" => "角色",
                "drop" => "空投",
                _ => pageId
            };
        }

        if (_pageBody is not null)
        {
            _pageBody.Text = pageId switch
            {
                "inventory" => BuildInventoryText(),
                "trade" => "交易频道已接入轨道站页面结构。正式交易条目将在轨道交易步骤解锁。",
                "research" => "研发页已保留蓝图和协议入口。未带回蓝图前显示锁定状态。",
                "characters" => "觉醒者舱位已建立。序章和远征回归会写入觉醒者实例 ID。",
                "drop" => "空投配置已接入创建远征流程。首批正式远征数据使用稳定单位和物资 ID。",
                _ => "未知页面"
            };
        }

        GD.Print($"[轨道] 打开页面：{pageId}");
        EmitSignal(SignalName.PageChanged, pageId);
    }

    private void BuildUi()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        TextureRect background = UiAssets.CreateTextureRect("OrbitBackground", UiAssets.OrbitBackground);
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(background);

        VBoxContainer root = new()
        {
            Name = "OrbitLayout"
        };
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(root);

        Label title = new()
        {
            Text = "轨道站",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        root.AddChild(title);

        HBoxContainer tabs = new()
        {
            Name = "Tabs"
        };
        root.AddChild(tabs);

        AddTabButton(tabs, "库存", "inventory", UiAssets.IconInventory);
        AddTabButton(tabs, "交易", "trade", UiAssets.IconTrade);
        AddTabButton(tabs, "研发", "research", UiAssets.IconResearch);
        AddTabButton(tabs, "角色", "characters", UiAssets.IconCharacters);
        AddTabButton(tabs, "空投", "drop", UiAssets.IconDrop);

        _pageTitle = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        root.AddChild(_pageTitle);

        _pageBody = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        root.AddChild(_pageBody);

        _payloadLabel = new Label
        {
            Text = "来源：无"
        };
        root.AddChild(_payloadLabel);

        if (!string.IsNullOrEmpty(_payload?.NavigationData?.OrbitPageId))
        {
            OpenPage(_payload.NavigationData.OrbitPageId);
        }

        Button startExpeditionButton = new()
        {
            Text = "确认空投配置"
        };
        startExpeditionButton.Pressed += () =>
        {
            GameRoot? gameRoot = FindGameRoot();
            gameRoot?.NavigateTo(SceneId.SurfaceExpedition, gameRoot.CreateExpeditionStartPayload(SceneId.OrbitStation));
        };
        root.AddChild(startExpeditionButton);

        Button backButton = new()
        {
            Text = "返回主入口"
        };
        backButton.Pressed += () => FindGameRoot()?.ShowMainMenu();
        root.AddChild(backButton);
    }

    private void AddTabButton(HBoxContainer tabs, string text, string pageId, string iconPath)
    {
        Button button = new()
        {
            Text = text,
            Icon = UiAssets.LoadTexture(iconPath)
        };
        button.Pressed += () => OpenPage(pageId);
        tabs.AddChild(button);
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

    private string BuildInventoryText()
    {
        GameRoot? gameRoot = FindGameRoot();
        if (gameRoot is null)
        {
            return "轨道永久库存等待会话状态。";
        }

        OrbitState orbitState = gameRoot.Session.OrbitState;
        if (orbitState.Inventory.Count == 0 && orbitState.StoredChipIds.Count == 0 && orbitState.UnlockedBlueprints.Count == 0)
        {
            return "轨道永久库存当前为空。回归结算写入后会在这里显示正式库存条目。";
        }

        System.Collections.Generic.List<string> lines = new();
        foreach (System.Collections.Generic.KeyValuePair<string, int> item in orbitState.Inventory)
        {
            lines.Add($"{item.Key} x{item.Value}");
        }

        foreach (string chipId in orbitState.StoredChipIds)
        {
            lines.Add($"芯片：{chipId}");
        }

        foreach (string blueprintId in orbitState.UnlockedBlueprints)
        {
            lines.Add($"蓝图：{blueprintId}");
        }

        return string.Join("\n", lines);
    }
}
