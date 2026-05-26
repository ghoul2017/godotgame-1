using Godot;

namespace GodotGame;

public partial class ReturnSummary : Control, ScenePayloadReceiver
{
    private Label? _summaryLabel;
    private ScenePayload? _payload;

    public override void _Ready()
    {
        BuildUi();
    }

    public void ReceivePayload(ScenePayload payload)
    {
        _payload = payload;
        if (_summaryLabel is not null)
        {
            string cargoText = payload.ReturnCargo.Count == 0 ? "无" : string.Join(", ", payload.ReturnCargo.ConvertAll(item => $"{item.ItemId} x{item.Count}"));
            string lostText = payload.LostUnits.Count == 0 ? "无" : string.Join(", ", payload.LostUnits);
            string discoveredText = payload.DiscoveredIds.Count == 0 ? "无" : string.Join(", ", payload.DiscoveredIds);
            _summaryLabel.Text = $"回归结算\n来源：{payload.FromScene}\n种子：{payload.Seed}\n带回物资：{cargoText}\n损失列表：{lostText}\n发现内容：{discoveredText}";
        }

        GD.Print("[结算] 打开回归结算");
    }

    private void BuildUi()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        VBoxContainer root = new()
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(root);

        _summaryLabel = new Label
        {
            Text = "回归结算",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        root.AddChild(_summaryLabel);

        Button orbitButton = new()
        {
            Text = "写入轨道并返回轨道站"
        };
        orbitButton.Pressed += () =>
        {
            GameRoot? gameRoot = FindGameRoot();
            if (gameRoot is null)
            {
                return;
            }

            if (_payload is not null)
            {
                gameRoot.ApplyReturnSummary(_payload);
            }

            gameRoot.NavigateTo(SceneId.OrbitStation, gameRoot.CreateNavigationPayload(SceneId.ReturnSummary, SceneId.OrbitStation));
        };
        root.AddChild(orbitButton);

        Button backButton = new()
        {
            Text = "返回主入口"
        };
        backButton.Pressed += () => FindGameRoot()?.ShowMainMenu();
        root.AddChild(backButton);
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
