using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGame;

public partial class DropConfig : Control, ScenePayloadReceiver
{
    private GameRoot? _gameRoot;
    private ScenePayload? _payload;
    private DropConfigSession? _config;
    private Label? _payloadLabel;
    private Label? _coordinateLabel;
    private Label? _podLabel;
    private Label? _unitLabel;
    private Label? _cargoLabel;
    private Label? _validationLabel;
    private Label? _feedbackLabel;
    private Button? _confirmButton;

    public override void _Ready()
    {
        _gameRoot = FindGameRoot();
        Theme = UiAssets.CreateBaseTheme();
        _gameRoot?.InputIntentController.SetUiBlocked(true);
        BuildUi();
        RefreshConfig();
    }

    public override void _ExitTree()
    {
        _gameRoot?.InputIntentController.SetUiBlocked(false);
    }

    public void ReceivePayload(ScenePayload payload)
    {
        _payload = payload;
        if (_payloadLabel is not null)
        {
            _payloadLabel.Text = $"来源：{payload.FromScene}  种子：{payload.Seed}";
        }

        RefreshConfig();
    }

    private void BuildUi()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        TextureRect background = UiAssets.CreateTextureRect("DropConfigBackground", UiAssets.OrbitBackground);
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(background);

        MarginContainer margin = new()
        {
            Name = "DropConfigMargin"
        };
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 18);
        margin.AddThemeConstantOverride("margin_top", 14);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_bottom", 14);
        AddChild(margin);

        VBoxContainer root = new()
        {
            Name = "DropConfigLayout"
        };
        margin.AddChild(root);

        HBoxContainer header = new()
        {
            CustomMinimumSize = new Vector2(0, 72)
        };
        root.AddChild(header);

        TextureRect icon = UiAssets.CreateTextureRect("DropConfigIcon", UiAssets.IconDrop);
        icon.CustomMinimumSize = new Vector2(54, 54);
        header.AddChild(icon);

        Label title = new()
        {
            Text = "空投配置",
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(220, 0)
        };
        header.AddChild(title);

        _payloadLabel = CreateTextLabel("来源：轨道站  种子：自动", 0);
        _payloadLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(_payloadLabel);

        HBoxContainer content = new()
        {
            Name = "DropConfigContent",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddChild(content);

        VBoxContainer coordinatePanel = AddPanel(content, "坐标与空投舱");
        _coordinateLabel = CreateTextLabel("坐标读取中", 0);
        coordinatePanel.AddChild(_coordinateLabel);
        _podLabel = CreateTextLabel("空投舱读取中", 0);
        coordinatePanel.AddChild(_podLabel);

        VBoxContainer unitPanel = AddPanel(content, "携带单位");
        _unitLabel = CreateTextLabel("单位读取中", 0);
        unitPanel.AddChild(_unitLabel);

        VBoxContainer cargoPanel = AddPanel(content, "装载清单");
        _cargoLabel = CreateTextLabel("物资读取中", 0);
        cargoPanel.AddChild(_cargoLabel);

        VBoxContainer validationPanel = AddPanel(content, "校验与确认");
        _validationLabel = CreateTextLabel("校验中", 0);
        validationPanel.AddChild(_validationLabel);

        _confirmButton = new Button
        {
            Text = "确认空投",
            CustomMinimumSize = new Vector2(0, 58),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _confirmButton.Pressed += ConfirmDrop;
        validationPanel.AddChild(_confirmButton);

        Button cancelButton = new()
        {
            Text = "返回轨道站",
            CustomMinimumSize = new Vector2(0, 58),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        cancelButton.Pressed += ReturnToOrbitDropPage;
        validationPanel.AddChild(cancelButton);

        PanelContainer footer = new()
        {
            CustomMinimumSize = new Vector2(0, 58),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _feedbackLabel = CreateTextLabel("空投配置从轨道永久状态读取坐标、单位、库存和空投舱定义。", 0);
        footer.AddChild(_feedbackLabel);
        root.AddChild(footer);
    }

    private void RefreshConfig()
    {
        if (_gameRoot is null)
        {
            SetStandaloneState();
            return;
        }

        ExpeditionCreationService service = new(_gameRoot.Session, _gameRoot.DataRegistry);
        int seed = _payload?.Seed ?? 0;
        _config = service.CreateDefaultDropConfig(seed);
        UpdateLabels(service);
    }

    private void UpdateLabels(ExpeditionCreationService service)
    {
        if (_config is null || _gameRoot is null)
        {
            return;
        }

        service.ValidateDropConfig(_config);
        if (_coordinateLabel is not null)
        {
            _coordinateLabel.Text =
                $"坐标 ID：{_config.SelectedCoordinateId}\n" +
                $"投放坐标：{_config.TargetCoordinate.X},{_config.TargetCoordinate.Y}\n" +
                $"已知坐标数：{_gameRoot.Session.OrbitState.KnownCoordinates.Count}";
        }

        if (_podLabel is not null)
        {
            string podName = _config.SelectedDropPodId;
            string podDescription = string.Empty;
            if (_gameRoot.DataRegistry.TryGetDropPod(_config.SelectedDropPodId, out DropPodData? pod) && pod is not null)
            {
                podName = pod.DisplayName;
                podDescription = pod.Description;
            }

            _podLabel.Text =
                $"空投舱：{podName}\n" +
                $"重量：{_config.UsedWeight:0.0}/{_config.WeightLimit:0.0}\n" +
                $"格位：{_config.UsedSlots}/{_config.SlotLimit}\n" +
                $"单位容量：{_config.UsedUnitCapacity}/{_config.UnitCapacity}\n" +
                podDescription;
        }

        if (_unitLabel is not null)
        {
            _unitLabel.Text =
                $"觉醒者：{FormatUnits(_config.SelectedAwakenedUnitInstanceIds)}\n" +
                $"量产单位：{FormatUnits(_config.SelectedMassUnitInstanceIds)}\n" +
                "单位将在远征状态中按实例 ID 写入。";
        }

        if (_cargoLabel is not null)
        {
            _cargoLabel.Text =
                $"堆叠物资：{FormatStacks(_config.SelectedStackItems)}\n" +
                $"实例装备：{FormatInstances(_config.SelectedItemInstanceIds)}";
        }

        if (_validationLabel is not null)
        {
            List<string> lines = new();
            lines.Add(_config.IsValid ? "状态：可确认空投" : "状态：需要处理以下问题");
            lines.AddRange(_config.ValidationErrors.Select(error => $"错误：{error}"));
            lines.AddRange(_config.ValidationWarnings.Select(warning => $"提示：{warning}"));
            _validationLabel.Text = string.Join("\n", lines);
        }

        if (_confirmButton is not null)
        {
            _confirmButton.Disabled = !_config.IsValid;
        }
    }

    private void ConfirmDrop()
    {
        if (_gameRoot is null || _config is null)
        {
            SetFeedback("缺少主入口上下文，不能确认空投。");
            return;
        }

        bool debugEnabled = _payload?.DebugEnabled ?? false;
        if (_gameRoot.TryStartExpeditionFromDropConfig(_config, SceneId.DropConfig, debugEnabled, out string message))
        {
            return;
        }

        SetFeedback(message);
        ExpeditionCreationService service = new(_gameRoot.Session, _gameRoot.DataRegistry);
        UpdateLabels(service);
    }

    private void ReturnToOrbitDropPage()
    {
        if (_gameRoot is null)
        {
            SetFeedback("缺少主入口上下文，不能返回轨道站。");
            return;
        }

        ScenePayload payload = _gameRoot.CreateNavigationPayload(SceneId.DropConfig, SceneId.OrbitStation, _payload?.DebugEnabled ?? false);
        payload.NavigationData ??= new NavigationPayloadData();
        payload.NavigationData.OrbitPageId = OrbitPageId.Drop;
        _gameRoot.NavigateTo(SceneId.OrbitStation, payload);
    }

    private void SetStandaloneState()
    {
        if (_coordinateLabel is not null)
        {
            _coordinateLabel.Text = "缺少 GameRoot，无法读取轨道状态。";
        }

        if (_podLabel is not null)
        {
            _podLabel.Text = "缺少 GameRoot，无法读取空投舱定义。";
        }

        if (_unitLabel is not null)
        {
            _unitLabel.Text = "缺少 GameRoot，无法读取单位实例。";
        }

        if (_cargoLabel is not null)
        {
            _cargoLabel.Text = "缺少 GameRoot，无法读取库存。";
        }

        if (_validationLabel is not null)
        {
            _validationLabel.Text = "状态：必须从主入口或轨道站进入空投配置。";
        }

        if (_confirmButton is not null)
        {
            _confirmButton.Disabled = true;
        }
    }

    private string FormatUnits(IReadOnlyList<string> unitInstanceIds)
    {
        if (_gameRoot is null || unitInstanceIds.Count == 0)
        {
            return "无";
        }

        List<string> parts = new();
        foreach (string unitInstanceId in unitInstanceIds)
        {
            if (_gameRoot.Session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance) &&
                _gameRoot.DataRegistry.TryGetUnit(unitInstance.UnitId, out UnitData? unitData) &&
                unitData is not null)
            {
                string displayName = string.IsNullOrEmpty(unitInstance.DisplayNameOverride) ? unitData.DisplayName : unitInstance.DisplayNameOverride;
                parts.Add($"{displayName} [{unitInstanceId}]");
            }
            else
            {
                parts.Add(unitInstanceId);
            }
        }

        return parts.Count == 0 ? "无" : string.Join("\n", parts);
    }

    private string FormatStacks(IReadOnlyList<ItemStack> stacks)
    {
        if (_gameRoot is null || stacks.Count == 0)
        {
            return "无";
        }

        return string.Join("\n", stacks.Select(stack => $"{_gameRoot.DataRegistry.GetItemName(stack.ItemId)} x{stack.Count}"));
    }

    private string FormatInstances(IReadOnlyList<string> itemInstanceIds)
    {
        if (_gameRoot is null || itemInstanceIds.Count == 0)
        {
            return "无";
        }

        List<string> parts = new();
        foreach (string itemInstanceId in itemInstanceIds)
        {
            if (_gameRoot.Session.ItemInstances.TryGetValue(itemInstanceId, out ItemInstance? itemInstance))
            {
                parts.Add($"{_gameRoot.DataRegistry.GetItemName(itemInstance.ItemId)} [{itemInstanceId}]");
            }
            else
            {
                parts.Add(itemInstanceId);
            }
        }

        return parts.Count == 0 ? "无" : string.Join("\n", parts);
    }

    private void SetFeedback(string message)
    {
        if (_feedbackLabel is not null)
        {
            _feedbackLabel.Text = message;
        }
    }

    private static VBoxContainer AddPanel(HBoxContainer parent, string title)
    {
        PanelContainer panel = new()
        {
            CustomMinimumSize = new Vector2(260, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        parent.AddChild(panel);

        VBoxContainer column = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        panel.AddChild(column);
        column.AddChild(UiAssets.CreateSectionLabel(title));
        return column;
    }

    private static Label CreateTextLabel(string text, float width)
    {
        return new Label
        {
            Text = text,
            CustomMinimumSize = new Vector2(width, 0),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
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
