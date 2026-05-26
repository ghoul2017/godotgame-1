using Godot;

namespace GodotGame;

public partial class OrbitStation : Control, ScenePayloadReceiver
{
    [Signal]
    public delegate void PageChangedEventHandler(string pageId);

    private Label? _pageTitle;
    private Label? _payloadLabel;

    public override void _Ready()
    {
        BuildUi();
        OpenPage("inventory");
    }

    public void ReceivePayload(ScenePayload payload)
    {
        if (_payloadLabel is not null)
        {
            _payloadLabel.Text = $"来源：{payload.FromScene}  种子：{payload.Seed}";
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

        GD.Print($"[轨道] 打开页面：{pageId}");
        EmitSignal(SignalName.PageChanged, pageId);
    }

    private void BuildUi()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

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

        AddTabButton(tabs, "库存", "inventory");
        AddTabButton(tabs, "交易", "trade");
        AddTabButton(tabs, "研发", "research");
        AddTabButton(tabs, "角色", "characters");
        AddTabButton(tabs, "空投", "drop");

        _pageTitle = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        root.AddChild(_pageTitle);

        _payloadLabel = new Label
        {
            Text = "来源：无"
        };
        root.AddChild(_payloadLabel);

        Button startExpeditionButton = new()
        {
            Text = "创建远征并进入地表"
        };
        startExpeditionButton.Pressed += () =>
        {
            GameRoot? gameRoot = FindGameRoot();
            gameRoot?.NavigateTo(SceneId.SurfaceExpedition, gameRoot.CreateDefaultPayload(SceneId.OrbitStation, SceneId.SurfaceExpedition));
        };
        root.AddChild(startExpeditionButton);

        Button backButton = new()
        {
            Text = "返回主入口"
        };
        backButton.Pressed += () => FindGameRoot()?.ShowMainMenu();
        root.AddChild(backButton);
    }

    private void AddTabButton(HBoxContainer tabs, string text, string pageId)
    {
        Button button = new()
        {
            Text = text
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
}
