using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGame;

public partial class DropConfig : Control, ScenePayloadReceiver
{
    private const string AudioCoordinateSelect = "res://assets/audio/ui/drop/coordinate_select.wav";
    private const string AudioPodSelect = "res://assets/audio/ui/drop/pod_select.wav";
    private const string AudioCargoAdd = "res://assets/audio/ui/drop/cargo_add.wav";
    private const string AudioCargoRemove = "res://assets/audio/ui/drop/cargo_remove.wav";
    private const string AudioValidationFailure = "res://assets/audio/ui/drop/validation_failure.wav";
    private const string AudioDropConfirm = "res://assets/audio/ui/drop/drop_confirm.wav";
    private const string AudioEnterSurface = "res://assets/audio/ui/drop/enter_surface.wav";

    private GameRoot? _gameRoot;
    private ScenePayload? _payload;
    private DropConfigSession? _config;
    private bool _isRefreshing;
    private bool _refreshPending;
    private Label? _payloadLabel;
    private Label? _summaryLabel;
    private VBoxContainer? _coordinateList;
    private VBoxContainer? _podList;
    private VBoxContainer? _unitList;
    private VBoxContainer? _cargoList;
    private VBoxContainer? _manifestList;
    private Label? _validationLabel;
    private Label? _feedbackLabel;
    private Button? _confirmButton;
    private AudioStreamPlayer? _audioPlayer;

    public override void _Ready()
    {
        _gameRoot = FindGameRoot();
        Theme = UiAssets.CreateBaseTheme();
        _gameRoot?.InputIntentController.SetUiBlocked(true);
        BuildUi();
        _refreshPending = true;
    }

    public override void _Process(double delta)
    {
        if (!_refreshPending)
        {
            return;
        }

        _refreshPending = false;
        RefreshCurrentNow();
    }

    public override void _ExitTree()
    {
        _gameRoot?.InputIntentController.SetUiBlocked(false);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        {
            ReturnToOrbitDropPage();
            GetViewport().SetInputAsHandled();
        }
    }

    public void ReceivePayload(ScenePayload payload)
    {
        _payload = payload;
        if (_payloadLabel is not null)
        {
            _payloadLabel.Text = $"来源：{payload.FromScene}  种子：{(payload.Seed > 0 ? payload.Seed.ToString() : "自动")}";
        }

        _refreshPending = false;
        RefreshConfig();
    }

    private void BuildUi()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        TextureRect background = UiAssets.CreateTextureRect("DropConfigBackground", UiAssets.OrbitBackground);
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(background);

        _audioPlayer = new AudioStreamPlayer
        {
            Name = "DropConfigAudio"
        };
        AddChild(_audioPlayer);

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
            Name = "DropConfigLayout",
            CustomMinimumSize = new Vector2(1244, 692)
        };
        margin.AddChild(root);

        HBoxContainer header = new()
        {
            CustomMinimumSize = new Vector2(0, 74)
        };
        root.AddChild(header);

        TextureRect icon = UiAssets.CreateTextureRect("DropConfigIcon", UiAssets.IconDrop);
        icon.CustomMinimumSize = new Vector2(54, 54);
        header.AddChild(icon);

        Label title = new()
        {
            Text = "空投配置",
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(200, 0)
        };
        header.AddChild(title);

        _payloadLabel = CreateTextLabel("来源：轨道站  种子：自动", 0);
        header.AddChild(_payloadLabel);

        _summaryLabel = CreateTextLabel("载荷审计等待轨道状态。", 0);
        header.AddChild(_summaryLabel);

        HBoxContainer content = new()
        {
            Name = "DropConfigContent",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddChild(content);

        _coordinateList = AddScrollablePanel(content, "坐标", 200);
        _podList = AddScrollablePanel(content, "空投舱", 220);
        _unitList = AddScrollablePanel(content, "单位", 230);
        _cargoList = AddScrollablePanel(content, "物资", 230);
        _manifestList = AddScrollablePanel(content, "审计", 300);

        HBoxContainer footer = new()
        {
            CustomMinimumSize = new Vector2(0, 86),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        root.AddChild(footer);

        _validationLabel = CreateTextLabel("校验中", 0);
        footer.AddChild(_validationLabel);

        _confirmButton = new Button
        {
            Text = "确认空投",
            Icon = UiAssets.LoadTexture(UiAssets.OrbitIconAvailable),
            ExpandIcon = true,
            CustomMinimumSize = new Vector2(170, 58)
        };
        _confirmButton.AddThemeConstantOverride("icon_max_width", 36);
        _confirmButton.Pressed += ConfirmDrop;
        footer.AddChild(_confirmButton);

        Button cancelButton = new()
        {
            Text = "返回轨道站",
            Icon = UiAssets.LoadTexture(UiAssets.OrbitIconLocked),
            ExpandIcon = true,
            CustomMinimumSize = new Vector2(170, 58)
        };
        cancelButton.AddThemeConstantOverride("icon_max_width", 36);
        cancelButton.Pressed += ReturnToOrbitDropPage;
        footer.AddChild(cancelButton);

        _feedbackLabel = CreateTextLabel("配置变更只写入临时会话，确认后才会扣除轨道库存。", 0);
        footer.AddChild(_feedbackLabel);
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
        RefreshUi(service);
    }

    private void RefreshUi(ExpeditionCreationService service)
    {
        if (_config is null || _gameRoot is null)
        {
            return;
        }

        _isRefreshing = true;
        service.ValidateDropConfig(_config);
        ClearChildren(_coordinateList);
        ClearChildren(_podList);
        ClearChildren(_unitList);
        ClearChildren(_cargoList);
        ClearChildren(_manifestList);
        BuildCoordinateRows();
        BuildPodRows(service);
        BuildUnitRows();
        BuildCargoRows();
        BuildManifestRows();
        UpdateSummaryAndValidation();
        _isRefreshing = false;
    }

    private void BuildCoordinateRows()
    {
        if (_gameRoot is null || _config is null || _coordinateList is null)
        {
            return;
        }

        foreach (string coordinateId in _gameRoot.Session.OrbitState.KnownCoordinates)
        {
            if (!_gameRoot.DataRegistry.TryGetKnownCoordinate(coordinateId, out KnownCoordinate? coordinate) || coordinate is null)
            {
                continue;
            }

            Button button = CreateToggleButton(
                coordinate.CoordinateId,
                $"{coordinate.DisplayName}\n{coordinate.RegionType}  风险 {coordinate.RiskLevel}  矿产 {string.Join(", ", coordinate.MineralTags)}",
                coordinate.IconPath,
                coordinate.CoordinateId == _config.SelectedCoordinateId,
                true,
                () =>
                {
                    _config.SelectedCoordinateId = coordinate.CoordinateId;
                    PlayAudio(AudioCoordinateSelect);
                    RefreshCurrent();
                });
            _coordinateList.AddChild(button);
        }
    }

    private void BuildPodRows(ExpeditionCreationService service)
    {
        if (_gameRoot is null || _config is null || _podList is null)
        {
            return;
        }

        foreach (DropPodData pod in _gameRoot.DataRegistry.DropPods.Values)
        {
            bool unlocked = service.IsDropPodUnlocked(pod);
            string lockText = unlocked ? "可用" : $"需要 {FormatRequirements(pod)}";
            Button button = CreateToggleButton(
                pod.Id,
                $"{pod.DisplayName}\n载重 {pod.WeightLimit:0.#}  格位 {pod.SlotLimit}  单位 {pod.UnitCapacity}  {lockText}",
                pod.IconPath,
                pod.Id == _config.SelectedDropPodId,
                unlocked,
                () =>
                {
                    _config.SelectedDropPodId = pod.Id;
                    PlayAudio(AudioPodSelect);
                    RefreshCurrent();
                });
            _podList.AddChild(button);
        }
    }

    private void BuildUnitRows()
    {
        if (_gameRoot is null || _config is null || _unitList is null)
        {
            return;
        }

        _unitList.AddChild(UiAssets.CreateSectionLabel("觉醒者"));
        foreach (string unitInstanceId in _gameRoot.Session.OrbitState.AwakenedUnits)
        {
            AddUnitToggle(unitInstanceId, _config.SelectedAwakenedUnitInstanceIds);
        }

        _unitList.AddChild(UiAssets.CreateSectionLabel("量产单位实例"));
        foreach (string unitInstanceId in _gameRoot.Session.OrbitState.AvailableMassUnitInstanceIds)
        {
            AddUnitToggle(unitInstanceId, _config.SelectedMassUnitInstanceIds);
        }

        _unitList.AddChild(UiAssets.CreateSectionLabel("单位平台"));
        InventoryContainer? orbitInventory = GetOrbitInventory();
        if (orbitInventory is null)
        {
            return;
        }

        foreach (ItemStack stack in orbitInventory.ItemStacks)
        {
            if (!_gameRoot.DataRegistry.TryGetItem(stack.ItemId, out ItemData? itemData) ||
                itemData is null ||
                itemData.Category != "unit_platform")
            {
                continue;
            }

            AddPlatformStepper(itemData, stack.Count);
        }
    }

    private void BuildCargoRows()
    {
        if (_gameRoot is null || _config is null || _cargoList is null)
        {
            return;
        }

        InventoryContainer? orbitInventory = GetOrbitInventory();
        if (orbitInventory is null)
        {
            return;
        }

        _cargoList.AddChild(UiAssets.CreateSectionLabel("堆叠物资"));
        foreach (ItemStack stack in orbitInventory.ItemStacks)
        {
            if (!_gameRoot.DataRegistry.TryGetItem(stack.ItemId, out ItemData? itemData) ||
                itemData is null ||
                !IsStackCargoCandidate(itemData))
            {
                continue;
            }

            AddStackStepper(itemData, stack.Count);
        }

        _cargoList.AddChild(UiAssets.CreateSectionLabel("实例道具"));
        foreach (string itemInstanceId in orbitInventory.ItemInstanceIds)
        {
            if (!_gameRoot.Session.ItemInstances.TryGetValue(itemInstanceId, out ItemInstance? instance) ||
                !_gameRoot.DataRegistry.TryGetItem(instance.ItemId, out ItemData? itemData) ||
                itemData is null ||
                !IsInstanceCargoCandidate(itemData))
            {
                continue;
            }

            Button button = CreateToggleButton(
                itemInstanceId,
                $"{itemData.DisplayName}\n实例 {itemInstanceId}  单重 {itemData.UnitWeight:0.#}",
                itemData.IconPath,
                _config.SelectedItemInstanceIds.Contains(itemInstanceId),
                string.IsNullOrEmpty(instance.BoundUnitInstanceId),
                () =>
                {
                    ToggleSelection(_config.SelectedItemInstanceIds, itemInstanceId);
                    PlayAudio(_config.SelectedItemInstanceIds.Contains(itemInstanceId) ? AudioCargoAdd : AudioCargoRemove);
                    RefreshCurrent();
                });
            _cargoList.AddChild(button);
        }
    }

    private void BuildManifestRows()
    {
        if (_gameRoot is null || _config is null || _manifestList is null)
        {
            return;
        }

        AddAuditSection("容量审计");
        AddAuditRow("重量", $"{_config.UsedWeight:0.0} / {_config.WeightLimit:0.0}", FormatFloatRemainder(_config.WeightLimit - _config.UsedWeight), "res://assets/ui/drop/capacity/weight.png");
        AddAuditRow("格位", $"{_config.UsedSlots} / {_config.SlotLimit}", FormatIntRemainder(_config.SlotLimit - _config.UsedSlots), "res://assets/ui/drop/capacity/slots.png");
        AddAuditRow("单位容量", $"{_config.UsedUnitCapacity} / {_config.UnitCapacity}", FormatIntRemainder(_config.UnitCapacity - _config.UsedUnitCapacity), "res://assets/ui/drop/capacity/unit_capacity.png");

        AddAuditSection("来源与目标");
        AddAuditRow("来源", _gameRoot.Session.OrbitState.InventoryId, "轨道库存，确认空投后才扣除");
        AddAuditRow("坐标", FormatCoordinate(_config.SelectedCoordinateId), $"投放点 {_config.TargetCoordinate.X},{_config.TargetCoordinate.Y}");
        AddAuditRow("空投舱", FormatDropPod(_config.SelectedDropPodId), "确认后生成本次空投货舱");
        AddAuditRow("种子", _config.Seed.ToString(), "远征创建参数");

        AddAuditSection("单位审计");
        AddAuditRow("觉醒者", FormatUnits(_config.SelectedAwakenedUnitInstanceIds), FormatUnitCapacity(_config.SelectedAwakenedUnitInstanceIds));
        AddAuditRow("量产单位", FormatUnits(_config.SelectedMassUnitInstanceIds), FormatUnitCapacity(_config.SelectedMassUnitInstanceIds));
        AddAuditRow("平台造单位", FormatPlatformAudit(_config.SelectedUnitPlatformItems), FormatPlatformCapacity(_config.SelectedUnitPlatformItems));

        AddAuditSection("货物审计");
        AddAuditRow("堆叠物资", FormatStackAudit(_config.SelectedStackItems), FormatStackWeight(_config.SelectedStackItems));
        AddAuditRow("实例道具", FormatInstanceAudit(_config.SelectedItemInstanceIds), FormatInstanceWeight(_config.SelectedItemInstanceIds));

        AddAuditSection("校验结果");
        if (_config.ValidationErrors.Count == 0 && _config.ValidationWarnings.Count == 0)
        {
            AddAuditRow("状态", "通过", "没有阻止确认的问题", UiAssets.OrbitIconAvailable);
            return;
        }

        foreach (string error in _config.ValidationErrors)
        {
            AddAuditRow("错误", error, "必须处理后才能确认", "res://assets/ui/drop/validation/error.png");
        }

        foreach (string warning in _config.ValidationWarnings)
        {
            AddAuditRow("提示", warning, "不阻止确认，但会影响远征准备", "res://assets/ui/drop/validation/warning.png");
        }
    }

    private void AddAuditSection(string title)
    {
        _manifestList?.AddChild(UiAssets.CreateSectionLabel(title));
    }

    private void AddAuditRow(string label, string value, string detail = "", string iconPath = "")
    {
        if (_manifestList is null)
        {
            return;
        }

        HBoxContainer row = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
            CustomMinimumSize = new Vector2(0, 50)
        };

        if (!string.IsNullOrEmpty(iconPath))
        {
            TextureRect icon = UiAssets.CreateTextureRect("AuditIcon", iconPath);
            icon.CustomMinimumSize = new Vector2(34, 34);
            icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            row.AddChild(icon);
        }

        VBoxContainer textBox = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin
        };
        row.AddChild(textBox);

        Label main = CreateTextLabel($"{label}：{value}", 0);
        textBox.AddChild(main);

        if (!string.IsNullOrWhiteSpace(detail))
        {
            Label detailLabel = CreateTextLabel(detail, 0);
            detailLabel.Modulate = new Color(0.78f, 0.82f, 0.88f);
            textBox.AddChild(detailLabel);
        }

        _manifestList.AddChild(row);
    }

    private static string FormatFloatRemainder(float remainder)
    {
        return remainder >= 0f ? $"剩余 {remainder:0.0}" : $"超出 {Math.Abs(remainder):0.0}";
    }

    private static string FormatIntRemainder(int remainder)
    {
        return remainder >= 0 ? $"剩余 {remainder}" : $"超出 {Math.Abs(remainder)}";
    }

    private string FormatUnitCapacity(IReadOnlyList<string> unitInstanceIds)
    {
        if (_gameRoot is null || unitInstanceIds.Count == 0)
        {
            return "单位容量 0，重量 0.0";
        }

        int capacity = 0;
        float weight = 0f;
        foreach (string unitInstanceId in unitInstanceIds)
        {
            if (_gameRoot.Session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance) &&
                _gameRoot.DataRegistry.TryGetUnit(unitInstance.UnitId, out UnitData? unitData) &&
                unitData is not null)
            {
                capacity += UnitCapacityCost(unitData, unitInstance.UnitId);
                weight += Math.Max(1f, unitData.CarryWeightLimit);
            }
        }

        return $"单位容量 {capacity}，重量 {weight:0.0}";
    }

    private string FormatPlatformAudit(IReadOnlyList<SelectedUnitPlatformItem> platforms)
    {
        if (_gameRoot is null || platforms.Count == 0)
        {
            return "无";
        }

        List<string> parts = new();
        foreach (SelectedUnitPlatformItem platform in platforms)
        {
            string targetName = _gameRoot.DataRegistry.TryGetUnit(platform.TargetUnitId, out UnitData? unitData) && unitData is not null
                ? unitData.DisplayName
                : platform.TargetUnitId;
            parts.Add($"{_gameRoot.DataRegistry.GetItemName(platform.ItemId)} x{platform.Count} -> {targetName} x{platform.Count}");
        }

        return string.Join("\n", parts);
    }

    private string FormatPlatformCapacity(IReadOnlyList<SelectedUnitPlatformItem> platforms)
    {
        if (_gameRoot is null || platforms.Count == 0)
        {
            return "单位容量 0，重量 0.0";
        }

        int capacity = 0;
        float weight = 0f;
        int slots = 0;
        foreach (SelectedUnitPlatformItem platform in platforms)
        {
            if (_gameRoot.DataRegistry.TryGetItem(platform.ItemId, out ItemData? itemData) && itemData is not null)
            {
                weight += itemData.UnitWeight * platform.Count;
                slots += EstimateStackSlots(platform.Count, itemData);
            }

            if (_gameRoot.DataRegistry.TryGetUnit(platform.TargetUnitId, out UnitData? unitData) && unitData is not null)
            {
                capacity += UnitCapacityCost(unitData, platform.TargetUnitId) * platform.Count;
            }
        }

        return $"单位容量 {capacity}，重量 {weight:0.0}，格位 {slots}，确认后转入单位创建队列";
    }

    private string FormatStackAudit(IReadOnlyList<ItemStack> stacks)
    {
        if (_gameRoot is null || stacks.Count == 0)
        {
            return "无";
        }

        List<string> parts = new();
        foreach (ItemStack stack in stacks)
        {
            if (_gameRoot.DataRegistry.TryGetItem(stack.ItemId, out ItemData? itemData) && itemData is not null)
            {
                float weight = itemData.UnitWeight * stack.Count;
                int slots = EstimateStackSlots(stack.Count, itemData);
                parts.Add($"{itemData.DisplayName} x{stack.Count}  重量 {weight:0.0}  格位 {slots}");
            }
            else
            {
                parts.Add($"{stack.ItemId} x{stack.Count}");
            }
        }

        return string.Join("\n", parts);
    }

    private string FormatStackWeight(IReadOnlyList<ItemStack> stacks)
    {
        if (_gameRoot is null || stacks.Count == 0)
        {
            return "重量 0.0，格位 0";
        }

        float weight = 0f;
        int slots = 0;
        foreach (ItemStack stack in stacks)
        {
            if (_gameRoot.DataRegistry.TryGetItem(stack.ItemId, out ItemData? itemData) && itemData is not null)
            {
                weight += itemData.UnitWeight * stack.Count;
                slots += EstimateStackSlots(stack.Count, itemData);
            }
        }

        return $"重量 {weight:0.0}，格位 {slots}";
    }

    private string FormatInstanceAudit(IReadOnlyList<string> itemInstanceIds)
    {
        if (_gameRoot is null || itemInstanceIds.Count == 0)
        {
            return "无";
        }

        List<string> parts = new();
        foreach (string itemInstanceId in itemInstanceIds)
        {
            if (_gameRoot.Session.ItemInstances.TryGetValue(itemInstanceId, out ItemInstance? itemInstance) &&
                _gameRoot.DataRegistry.TryGetItem(itemInstance.ItemId, out ItemData? itemData) &&
                itemData is not null)
            {
                parts.Add($"{itemData.DisplayName} [{itemInstanceId}]  重量 {itemData.UnitWeight:0.0}");
            }
            else
            {
                parts.Add(itemInstanceId);
            }
        }

        return string.Join("\n", parts);
    }

    private string FormatInstanceWeight(IReadOnlyList<string> itemInstanceIds)
    {
        if (_gameRoot is null || itemInstanceIds.Count == 0)
        {
            return "重量 0.0，格位 0";
        }

        float weight = 0f;
        foreach (string itemInstanceId in itemInstanceIds)
        {
            if (_gameRoot.Session.ItemInstances.TryGetValue(itemInstanceId, out ItemInstance? itemInstance) &&
                _gameRoot.DataRegistry.TryGetItem(itemInstance.ItemId, out ItemData? itemData) &&
                itemData is not null)
            {
                weight += itemData.UnitWeight;
            }
        }

        return $"重量 {weight:0.0}，格位 {itemInstanceIds.Count}";
    }

    private static int EstimateStackSlots(int count, ItemData itemData)
    {
        if (!itemData.CanStack)
        {
            return count;
        }

        int maxStack = Math.Max(1, itemData.MaxStack);
        return Math.Max(0, (int)Math.Ceiling(count / (float)maxStack));
    }

    private void UpdateSummaryAndValidation()
    {
        if (_config is null)
        {
            return;
        }

        if (_summaryLabel is not null)
        {
            _summaryLabel.Text = $"重量 {_config.UsedWeight:0.0}/{_config.WeightLimit:0.0}  格位 {_config.UsedSlots}/{_config.SlotLimit}  单位 {_config.UsedUnitCapacity}/{_config.UnitCapacity}";
        }

        if (_validationLabel is not null)
        {
            List<string> lines = new();
            lines.Add(_config.IsValid ? "状态：可确认空投" : "状态：阻止确认");
            lines.AddRange(_config.ValidationErrors.Select(error => $"错误：{error}"));
            lines.AddRange(_config.ValidationWarnings.Select(warning => $"提示：{warning}"));
            _validationLabel.Text = string.Join("\n", lines);
        }

        if (_confirmButton is not null)
        {
            _confirmButton.Disabled = !_config.IsValid;
            _confirmButton.Icon = UiAssets.LoadTexture(_config.IsValid ? UiAssets.OrbitIconAvailable : UiAssets.OrbitIconInsufficient);
        }
    }

    private void AddUnitToggle(string unitInstanceId, List<string> targetList)
    {
        if (_gameRoot is null || _config is null || _unitList is null)
        {
            return;
        }

        if (!_gameRoot.Session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance) ||
            !_gameRoot.DataRegistry.TryGetUnit(unitInstance.UnitId, out UnitData? unitData) ||
            unitData is null)
        {
            return;
        }

        string displayName = string.IsNullOrEmpty(unitInstance.DisplayNameOverride) ? unitData.DisplayName : unitInstance.DisplayNameOverride;
        Button button = CreateToggleButton(
            unitInstanceId,
            $"{displayName}\n{unitData.UnitRole}  耐久 {unitInstance.Durability}  单位容量 {UnitCapacityCost(unitData, unitInstance.UnitId)}{(unitInstance.IsLocked ? "  远征中" : string.Empty)}",
            unitData.IconPath,
            targetList.Contains(unitInstanceId),
            unitInstance.Durability > 0 && !unitInstance.IsLocked,
            () =>
            {
                ToggleSelection(targetList, unitInstanceId);
                PlayAudio(targetList.Contains(unitInstanceId) ? AudioCargoAdd : AudioCargoRemove);
                RefreshCurrent();
            });
        _unitList.AddChild(button);
    }

    private void AddPlatformStepper(ItemData itemData, int available)
    {
        if (_config is null || _unitList is null)
        {
            return;
        }

        HBoxContainer row = CreateStepperRow(itemData.IconPath, $"{itemData.DisplayName}\n库存 {available}  目标 {itemData.TargetUnitId}  单重 {itemData.UnitWeight:0.#}");
        SpinBox spinBox = CreateCountSpinBox(available, GetPlatformCount(itemData.Id));
        spinBox.ValueChanged += value =>
        {
            if (_isRefreshing)
            {
                return;
            }

            SetPlatformCount(itemData.Id, itemData.TargetUnitId, (int)value);
            PlayAudio(value > 0 ? AudioCargoAdd : AudioCargoRemove);
            RefreshCurrent();
        };
        row.AddChild(spinBox);
        _unitList.AddChild(row);
    }

    private void AddStackStepper(ItemData itemData, int available)
    {
        if (_config is null || _cargoList is null)
        {
            return;
        }

        HBoxContainer row = CreateStepperRow(itemData.IconPath, $"{itemData.DisplayName}\n库存 {available}  单重 {itemData.UnitWeight:0.#}  标签 {string.Join(", ", itemData.Tags)}");
        SpinBox spinBox = CreateCountSpinBox(available, GetStackCount(itemData.Id));
        spinBox.ValueChanged += value =>
        {
            if (_isRefreshing)
            {
                return;
            }

            SetStackCount(itemData.Id, (int)value);
            PlayAudio(value > 0 ? AudioCargoAdd : AudioCargoRemove);
            RefreshCurrent();
        };
        row.AddChild(spinBox);
        _cargoList.AddChild(row);
    }

    private HBoxContainer CreateStepperRow(string iconPath, string text)
    {
        HBoxContainer row = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 78)
        };
        TextureRect icon = UiAssets.CreateTextureRect("RowIcon", iconPath);
        icon.CustomMinimumSize = new Vector2(44, 44);
        row.AddChild(icon);
        Label label = CreateTextLabel(text, 0);
        row.AddChild(label);
        return row;
    }

    private static SpinBox CreateCountSpinBox(int maxValue, int currentValue)
    {
        return new SpinBox
        {
            MinValue = 0,
            MaxValue = maxValue,
            Step = 1,
            Value = currentValue,
            CustomMinimumSize = new Vector2(76, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd
        };
    }

    private Button CreateToggleButton(string rowId, string text, string iconPath, bool selected, bool enabled, Action pressed)
    {
        Button button = new()
        {
            Name = $"DropRow_{rowId}",
            Text = text,
            Icon = UiAssets.LoadTexture(iconPath),
            ExpandIcon = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimWordEllipsis,
            ToggleMode = true,
            ButtonPressed = selected,
            Disabled = !enabled,
            CustomMinimumSize = new Vector2(0, 78),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        button.AddThemeConstantOverride("icon_max_width", 44);
        button.Pressed += pressed;
        return button;
    }

    private void ConfirmDrop()
    {
        if (_gameRoot is null || _config is null)
        {
            SetFeedback("缺少主入口上下文，不能确认空投。");
            PlayAudio(AudioValidationFailure);
            return;
        }

        bool debugEnabled = _payload?.DebugEnabled ?? false;
        if (_gameRoot.TryStartExpeditionFromDropConfig(_config, SceneId.DropConfig, debugEnabled, out string message))
        {
            PlayAudio(AudioDropConfirm);
            PlayAudio(AudioEnterSurface);
            return;
        }

        SetFeedback(message);
        PlayAudio(AudioValidationFailure);
        RefreshCurrent();
    }

    private void ReturnToOrbitDropPage()
    {
        if (_gameRoot is null)
        {
            SetFeedback("缺少主入口上下文，不能返回轨道站。");
            PlayAudio(AudioValidationFailure);
            return;
        }

        ScenePayload payload = _gameRoot.CreateNavigationPayload(SceneId.DropConfig, SceneId.OrbitStation, _payload?.DebugEnabled ?? false);
        payload.NavigationData ??= new NavigationPayloadData();
        payload.NavigationData.OrbitPageId = OrbitPageId.Drop;
        _gameRoot.NavigateTo(SceneId.OrbitStation, payload);
    }

    private void RefreshCurrent()
    {
        if (_gameRoot is null)
        {
            return;
        }

        _refreshPending = true;
    }

    private void RefreshCurrentNow()
    {
        if (_gameRoot is null)
        {
            return;
        }

        if (_config is null)
        {
            RefreshConfig();
            return;
        }

        ExpeditionCreationService service = new(_gameRoot.Session, _gameRoot.DataRegistry);
        RefreshUi(service);
    }

    private void SetStandaloneState()
    {
        ClearChildren(_coordinateList);
        ClearChildren(_podList);
        ClearChildren(_unitList);
        ClearChildren(_cargoList);
        ClearChildren(_manifestList);

        _coordinateList?.AddChild(CreateTextLabel("缺少 GameRoot，无法读取轨道状态。", 0));
        _podList?.AddChild(CreateTextLabel("缺少 GameRoot，无法读取空投舱定义。", 0));
        _unitList?.AddChild(CreateTextLabel("缺少 GameRoot，无法读取单位实例。", 0));
        _cargoList?.AddChild(CreateTextLabel("缺少 GameRoot，无法读取库存。", 0));
        _manifestList?.AddChild(CreateTextLabel("必须从主入口或轨道站进入空投配置。", 0));

        if (_confirmButton is not null)
        {
            _confirmButton.Disabled = true;
        }
    }

    private InventoryContainer? GetOrbitInventory()
    {
        if (_gameRoot is null)
        {
            return null;
        }

        return _gameRoot.Session.Inventories.TryGetValue(_gameRoot.Session.OrbitState.InventoryId, out InventoryContainer? inventory)
            ? inventory
            : null;
    }

    private void SetStackCount(string itemId, int count)
    {
        if (_config is null)
        {
            return;
        }

        _config.SelectedStackItems.RemoveAll(stack => stack.ItemId == itemId);
        if (count > 0)
        {
            _config.SelectedStackItems.Add(new ItemStack { ItemId = itemId, Count = count });
        }
    }

    private int GetStackCount(string itemId)
    {
        return _config?.SelectedStackItems.FirstOrDefault(stack => stack.ItemId == itemId)?.Count ?? 0;
    }

    private void SetPlatformCount(string itemId, string targetUnitId, int count)
    {
        if (_config is null)
        {
            return;
        }

        _config.SelectedUnitPlatformItems.RemoveAll(item => item.ItemId == itemId);
        if (count > 0)
        {
            _config.SelectedUnitPlatformItems.Add(new SelectedUnitPlatformItem
            {
                ItemId = itemId,
                Count = count,
                TargetUnitId = targetUnitId
            });
        }
    }

    private int GetPlatformCount(string itemId)
    {
        return _config?.SelectedUnitPlatformItems.FirstOrDefault(item => item.ItemId == itemId)?.Count ?? 0;
    }

    private static void ToggleSelection(List<string> values, string value)
    {
        if (!values.Remove(value))
        {
            values.Add(value);
        }
    }

    private static bool IsStackCargoCandidate(ItemData itemData)
    {
        return !itemData.RequiresInstance &&
            itemData.Category != "unit_platform" &&
            itemData.Tags.Contains("drop_allowed");
    }

    private static bool IsInstanceCargoCandidate(ItemData itemData)
    {
        return itemData.RequiresInstance && itemData.Tags.Contains("drop_allowed");
    }

    private string FormatCoordinate(string coordinateId)
    {
        if (_gameRoot is not null &&
            _gameRoot.DataRegistry.TryGetKnownCoordinate(coordinateId, out KnownCoordinate? coordinate) &&
            coordinate is not null)
        {
            return $"{coordinate.DisplayName} [{coordinateId}]";
        }

        return string.IsNullOrEmpty(coordinateId) ? "未选择" : coordinateId;
    }

    private string FormatDropPod(string dropPodId)
    {
        if (_gameRoot is not null &&
            _gameRoot.DataRegistry.TryGetDropPod(dropPodId, out DropPodData? pod) &&
            pod is not null)
        {
            return $"{pod.DisplayName} [{dropPodId}]";
        }

        return string.IsNullOrEmpty(dropPodId) ? "未选择" : dropPodId;
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

    private static string FormatRequirements(DropPodData pod)
    {
        List<string> requirements = new();
        if (!string.IsNullOrEmpty(pod.RequiresBlueprintId))
        {
            requirements.Add($"蓝图 {pod.RequiresBlueprintId}");
        }

        requirements.AddRange(pod.RequiresProtocolIds.Select(protocolId => $"协议 {protocolId}"));
        return requirements.Count == 0 ? "无前置" : string.Join(", ", requirements);
    }

    private static int UnitCapacityCost(UnitData unitData, string unitId)
    {
        return unitId is "heavy_cargo_spider" or "rockbreaker" || unitData.Tags.Contains("heavy") ? 2 : 1;
    }

    private void SetFeedback(string message)
    {
        if (_feedbackLabel is not null)
        {
            _feedbackLabel.Text = message;
        }
    }

    private static VBoxContainer AddScrollablePanel(HBoxContainer parent, string title, float minimumWidth)
    {
        PanelContainer panel = new()
        {
            CustomMinimumSize = new Vector2(minimumWidth, 0),
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

        ScrollContainer scroll = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        column.AddChild(scroll);

        VBoxContainer list = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin
        };
        scroll.AddChild(list);
        return list;
    }

    private static Label CreateTextLabel(string text, float width)
    {
        return new Label
        {
            Text = text,
            CustomMinimumSize = new Vector2(width, 0),
            AutowrapMode = TextServer.AutowrapMode.Off,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimWordEllipsis,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin
        };
    }

    private static void ClearChildren(Node? node)
    {
        if (node is null)
        {
            return;
        }

        foreach (Node child in node.GetChildren())
        {
            node.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void PlayAudio(string path)
    {
        if (_audioPlayer is null)
        {
            return;
        }

        if (DisplayServer.GetName() == "headless")
        {
            if (!FileAccess.FileExists(path))
            {
                GD.PushWarning($"[空投] 音频资源缺失：{path}");
            }

            return;
        }

        AudioStream? stream = LoadAudioStream(path);
        if (stream is null)
        {
            GD.PushWarning($"[空投] 音频资源加载失败：{path}");
            return;
        }

        _audioPlayer.Stream = stream;
        _audioPlayer.Play();
    }

    private static AudioStream? LoadAudioStream(string path)
    {
        if (path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) && FileAccess.FileExists(path))
        {
            return LoadWavDirect(path);
        }

        return ResourceLoader.Load<AudioStream>(path);
    }

    private static AudioStream? LoadWavDirect(string path)
    {
        byte[] bytes = FileAccess.GetFileAsBytes(path);
        if (bytes.Length < 44 ||
            bytes[0] != 'R' ||
            bytes[1] != 'I' ||
            bytes[2] != 'F' ||
            bytes[3] != 'F' ||
            bytes[8] != 'W' ||
            bytes[9] != 'A' ||
            bytes[10] != 'V' ||
            bytes[11] != 'E')
        {
            return null;
        }

        short channels = BitConverter.ToInt16(bytes, 22);
        int sampleRate = BitConverter.ToInt32(bytes, 24);
        short bitsPerSample = BitConverter.ToInt16(bytes, 34);
        int dataOffset = -1;
        int dataSize = 0;
        for (int index = 12; index + 8 <= bytes.Length; index++)
        {
            if (bytes[index] == 'd' && bytes[index + 1] == 'a' && bytes[index + 2] == 't' && bytes[index + 3] == 'a')
            {
                dataOffset = index + 8;
                dataSize = BitConverter.ToInt32(bytes, index + 4);
                break;
            }
        }

        if (dataOffset < 0 || dataSize <= 0 || dataOffset + dataSize > bytes.Length || bitsPerSample != 16)
        {
            return null;
        }

        byte[] data = new byte[dataSize];
        Array.Copy(bytes, dataOffset, data, 0, dataSize);
        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = sampleRate,
            Stereo = channels == 2,
            Data = data
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
