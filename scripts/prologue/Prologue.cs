using Godot;

namespace GodotGame;

public partial class Prologue : Control, ScenePayloadReceiver
{
    public override void _Ready()
    {
        BuildUi();
    }

    public void ReceivePayload(ScenePayload payload)
    {
        GD.Print($"[序章] 进入序章，来源：{payload.FromScene}");
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

        Label label = new()
        {
            Text = "序章：灵巧获得秘密坐标，准备空投寻找数据核心。",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        root.AddChild(label);

        Button startButton = new()
        {
            Text = "进入序章远征"
        };
        startButton.Pressed += () =>
        {
            GameRoot? gameRoot = FindGameRoot();
            gameRoot?.NavigateTo(SceneId.SurfaceExpedition, gameRoot.CreateDefaultPayload(SceneId.Prologue, SceneId.SurfaceExpedition));
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
