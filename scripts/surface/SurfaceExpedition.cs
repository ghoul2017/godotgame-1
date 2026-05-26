using Godot;

namespace GodotGame;

public partial class SurfaceExpedition : Node2D, ScenePayloadReceiver
{
    private Label? _statusLabel;

    public override void _Ready()
    {
        BuildUi();
    }

    public void ReceivePayload(ScenePayload payload)
    {
        if (_statusLabel is not null)
        {
            string expeditionId = payload.Data.TryGetValue("expedition_id", out Variant id) ? id.AsString() : "unknown";
            _statusLabel.Text = $"地表远征\n远征：{expeditionId}\n种子：{payload.Seed}";
        }

        GD.Print($"[远征] 进入地表远征，种子：{payload.Seed}");
    }

    private void BuildUi()
    {
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
        root.AddChild(panel);

        _statusLabel = new Label
        {
            Text = "地表远征"
        };
        panel.AddChild(_statusLabel);

        Label layoutHint = new()
        {
            Text = "UI 壳层：资源栏 / 小地图 / 单位面板 / 命令区"
        };
        panel.AddChild(layoutHint);

        Button returnButton = new()
        {
            Text = "模拟火箭回归"
        };
        returnButton.Pressed += () =>
        {
            GameRoot? gameRoot = FindGameRoot();
            gameRoot?.NavigateTo(SceneId.ReturnSummary, gameRoot.CreateDefaultPayload(SceneId.SurfaceExpedition, SceneId.ReturnSummary));
        };
        panel.AddChild(returnButton);

        Button backButton = new()
        {
            Text = "返回主入口"
        };
        backButton.Pressed += () => FindGameRoot()?.ShowMainMenu();
        panel.AddChild(backButton);
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
