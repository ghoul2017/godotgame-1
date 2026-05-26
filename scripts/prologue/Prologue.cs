using Godot;

namespace GodotGame;

public partial class Prologue : Control, ScenePayloadReceiver
{
    private Label? _nodeLabel;

    public override void _Ready()
    {
        BuildUi();
    }

    public void ReceivePayload(ScenePayload payload)
    {
        if (_nodeLabel is not null && !string.IsNullOrEmpty(payload.NavigationData?.PrologueNodeId))
        {
            _nodeLabel.Text = $"序章节点：{payload.NavigationData.PrologueNodeId}";
        }

        GD.Print($"[序章] 进入序章，来源：{payload.FromScene}");
    }

    private void BuildUi()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        TextureRect background = UiAssets.CreateTextureRect("PrologueBackground", UiAssets.PrologueBackground);
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(background);

        VBoxContainer root = new()
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(root);

        Label label = new()
        {
            Text = "序章：灵巧获得秘密坐标，准备空投寻找数据核心。",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        root.AddChild(label);

        _nodeLabel = new Label
        {
            Text = "序章节点：opening",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        root.AddChild(_nodeLabel);

        Button startButton = new()
        {
            Text = "进入序章远征"
        };
        startButton.Pressed += () =>
        {
            GameRoot? gameRoot = FindGameRoot();
            gameRoot?.NavigateTo(SceneId.SurfaceExpedition, gameRoot.CreateExpeditionStartPayload(SceneId.Prologue));
        };
        root.AddChild(startButton);

        Button orbitButton = new()
        {
            Text = "进入轨道站"
        };
        orbitButton.Pressed += () =>
        {
            GameRoot? gameRoot = FindGameRoot();
            gameRoot?.NavigateTo(SceneId.OrbitStation, gameRoot.CreateNavigationPayload(SceneId.Prologue, SceneId.OrbitStation));
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
