using Godot;

namespace GodotGame;

public partial class ReturnSummary : Control, ScenePayloadReceiver
{
    private Label? _summaryLabel;
    private VBoxContainer? _cargoList;
    private VBoxContainer? _lossList;
    private VBoxContainer? _discoveryList;
    private VBoxContainer? _chipList;
    private VBoxContainer? _blueprintList;
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
            _summaryLabel.Text = $"回归结算  |  来源 {payload.FromScene}  |  种子 {payload.Seed}";
        }

        ReturnSummaryPayloadData? summaryData = payload.ReturnSummaryData;
        if (summaryData is null)
        {
            FillItemList(_cargoList, new[] { "无带回物资" });
            FillItemList(_lossList, new[] { "无单位和装备损失" });
            FillItemList(_discoveryList, new[] { "无新增发现" });
            FillItemList(_chipList, new[] { "无带回芯片" });
            FillItemList(_blueprintList, new[] { "无带回蓝图" });
            return;
        }

        FillItemList(_cargoList, summaryData.ReturnCargo.Count == 0 ? new[] { "无带回物资" } : summaryData.ReturnCargo.ConvertAll(item => $"{item.ItemId} x{item.Count}"));
        FillItemList(_lossList, summaryData.LostUnits.Count == 0 ? new[] { "无单位和装备损失" } : summaryData.LostUnits);
        FillItemList(_discoveryList, summaryData.DiscoveredIds.Count == 0 ? new[] { "无新增发现" } : summaryData.DiscoveredIds);
        FillItemList(_chipList, summaryData.ReturnedChipIds.Count == 0 ? new[] { "无带回芯片" } : summaryData.ReturnedChipIds);
        FillItemList(_blueprintList, summaryData.ReturnedBlueprintIds.Count == 0 ? new[] { "无带回蓝图" } : summaryData.ReturnedBlueprintIds);
        GD.Print("[结算] 打开回归结算");
    }

    private void BuildUi()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        TextureRect background = UiAssets.CreateTextureRect("SummaryBackground", UiAssets.SummaryBackground);
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(background);

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

        HBoxContainer lists = new()
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        root.AddChild(lists);

        _cargoList = AddSummaryColumn(lists, "带回物资", UiAssets.IconCargo);
        _lossList = AddSummaryColumn(lists, "损失列表", UiAssets.IconLoss);
        _discoveryList = AddSummaryColumn(lists, "发现内容", UiAssets.IconDiscovery);
        _chipList = AddSummaryColumn(lists, "芯片", UiAssets.IconResearch);
        _blueprintList = AddSummaryColumn(lists, "蓝图", UiAssets.IconDrop);

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

    private static VBoxContainer AddSummaryColumn(HBoxContainer parent, string title, string iconPath)
    {
        PanelContainer panel = new()
        {
            CustomMinimumSize = new Vector2(280, 220),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        parent.AddChild(panel);

        VBoxContainer column = new();
        panel.AddChild(column);
        TextureRect icon = UiAssets.CreateTextureRect($"{title}Icon", iconPath);
        icon.CustomMinimumSize = new Vector2(48, 48);
        column.AddChild(icon);
        column.AddChild(UiAssets.CreateSectionLabel(title));
        return column;
    }

    private static void FillItemList(VBoxContainer? list, System.Collections.Generic.IReadOnlyList<string> values)
    {
        if (list is null)
        {
            return;
        }

        while (list.GetChildCount() > 2)
        {
            Node child = list.GetChild(2);
            list.RemoveChild(child);
            child.QueueFree();
        }

        foreach (string value in values)
        {
            list.AddChild(new Label
            {
                Text = value,
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            });
        }
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
