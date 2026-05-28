using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGame;

public partial class SurfaceExpedition : Node2D, ScenePayloadReceiver
{
    private static readonly IReadOnlyDictionary<string, string> CommandIconPaths = new Dictionary<string, string>
    {
        ["move"] = "res://assets/ui/surface/commands/command_move.png",
        ["stop"] = "res://assets/ui/surface/commands/command_stop.png",
        ["gather"] = "res://assets/ui/surface/commands/command_gather.png",
        ["haul"] = "res://assets/ui/surface/commands/command_haul.png",
        ["build"] = "res://assets/ui/surface/commands/command_build.png",
        ["guard"] = "res://assets/ui/surface/commands/command_guard.png",
        ["repair"] = "res://assets/ui/surface/commands/command_repair.png",
        ["attack"] = "res://assets/ui/surface/commands/command_attack.png",
        ["scan"] = "res://assets/ui/surface/commands/command_scan.png",
        ["hack"] = "res://assets/ui/surface/commands/command_hack.png",
        ["scout"] = "res://assets/ui/surface/commands/command_scout.png",
        ["return_to_repair"] = "res://assets/ui/surface/commands/command_return_repair.png"
    };

    private static readonly IReadOnlyDictionary<string, string> CommandDisplayNames = new Dictionary<string, string>
    {
        ["move"] = "移动",
        ["stop"] = "停止",
        ["gather"] = "采集",
        ["haul"] = "搬运",
        ["build"] = "建造",
        ["guard"] = "护卫",
        ["repair"] = "修理",
        ["attack"] = "攻击",
        ["scan"] = "扫描",
        ["hack"] = "骇入",
        ["scout"] = "侦察",
        ["return_to_repair"] = "维修点"
    };

    private const float DragThreshold = 8f;
    private const float CameraSpeed = 520f;
    private const float ZoomStep = 0.08f;
    private const float StructureHitRadius = 72f;
    private const float BuildableRadiusFromDrop = 920f;
    private const float MineralPlacementSpacing = 96f;
    private const float UnitPlacementSpacing = 74f;

    private readonly Dictionary<string, SurfaceUnit> _surfaceUnits = new();
    private readonly Dictionary<string, SurfaceMineralDepositView> _mineralViews = new();
    private readonly SurfaceSelectionState _selectionState = new();
    private readonly Dictionary<string, Button> _commandButtons = new();

    private Label? _statusLabel;
    private Label? _manifestLabel;
    private TextureRect? _selectionPortrait;
    private Label? _selectionLabel;
    private Label? _messageLabel;
    private Button? _behaviorModeButton;
    private Node2D? _unitLayer;
    private Node2D? _mineralLayer;
    private Node2D? _buildingLayer;
    private Node2D? _effectLayer;
    private Camera2D? _camera;
    private TextureRect? _dragSelectFrame;
    private Sprite2D? _moveMarker;
    private Line2D? _pathLine;
    private TextureRect? _groupBadge;
    private TextureRect? _commandFailedIcon;
    private PanelContainer? _buildCatalogPanel;
    private VBoxContainer? _buildCatalogList;
    private AudioStreamPlayer? _audioPlayer;
    private ScenePayload? _payload;
    private string _pendingTargetCommand = string.Empty;
    private string _pendingBuildBuildingId = string.Empty;
    private string _selectedBuildingInstanceId = string.Empty;
    private string _selectedConstructionSiteId = string.Empty;
    private Vector2 _dragStartViewport;
    private Vector2 _dragStartWorld;
    private bool _isDraggingSelection;

    public override void _Ready()
    {
        BuildUi();
    }

    public override void _Process(double delta)
    {
        HandleCameraMotion(delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        InputIntentController? inputController = FindGameRoot()?.InputIntentController;
        if (inputController is null || !inputController.CanHandleSurfaceCommand())
        {
            return;
        }

        if (@event is InputEventMouseButton mouseButton)
        {
            HandleMouseButton(mouseButton);
            return;
        }

        if (@event is InputEventMouseMotion mouseMotion)
        {
            HandleMouseMotion(mouseMotion);
            return;
        }

        if (@event is InputEventKey keyEvent)
        {
            HandleKeyInput(keyEvent);
        }
    }

    public override void _ExitTree()
    {
        FindGameRoot()?.InputIntentController.SetUiBlocked(false);
    }

    public void ReceivePayload(ScenePayload payload)
    {
        _payload = payload;
        ExpeditionStartPayloadData? expeditionData = payload.ExpeditionStartData;
        if (_statusLabel is not null && expeditionData is not null)
        {
            _statusLabel.Text = $"远征 {expeditionData.ExpeditionId}  |  种子 {expeditionData.Seed}  |  坐标 {expeditionData.TargetCoordinateId} ({expeditionData.DropPosition.X},{expeditionData.DropPosition.Y})";
        }

        if (_manifestLabel is not null && expeditionData is not null)
        {
            string units = expeditionData.InitialUnits.Count == 0 ? "无" : string.Join("  ", expeditionData.InitialUnits.ConvertAll(unit => $"{unit.UnitId} x{unit.Count} [{unit.ConfigId}]"));
            string items = expeditionData.InitialItems.Count == 0 ? "无" : string.Join("  ", expeditionData.InitialItems.ConvertAll(item => $"{item.ItemId} x{item.Count}"));
            _manifestLabel.Text = $"空投计划：{expeditionData.DropPlanId}\n空投库存：{expeditionData.DropPodCargoInventoryId}\n初始单位：{units}\n携带物资：{items}";
        }

        InitializeSurfaceState(payload);
        GD.Print($"[远征] 进入地表远征，种子：{payload.Seed}");
    }

    private void InitializeSurfaceState(ScenePayload payload)
    {
        GameRoot? gameRoot = FindGameRoot();
        if (gameRoot is null || _unitLayer is null || _mineralLayer is null)
        {
            return;
        }

        if (!SurfaceExpeditionValidator.TryValidate(payload, gameRoot.Session, out ExpeditionState? expeditionState, out string message) ||
            expeditionState is null)
        {
            GD.PushError($"[远征] {message}");
            SetMessage(message);
            if (HasArgument("--surface-self-test"))
            {
                QuitSelfTestFailure();
            }

            return;
        }

        SurfaceMineralSeeder.EnsureInitialMinerals(expeditionState, gameRoot.DataRegistry);
        RenderMinerals(expeditionState, gameRoot.DataRegistry);

        foreach (SurfaceUnit existingUnit in _surfaceUnits.Values)
        {
            existingUnit.QueueFree();
        }

        _surfaceUnits.Clear();
        SurfaceUnitFactory factory = new(gameRoot.Session, gameRoot.DataRegistry);
        foreach (SurfaceUnit surfaceUnit in factory.CreateUnits(expeditionState, _unitLayer))
        {
            _surfaceUnits[surfaceUnit.UnitInstanceId] = surfaceUnit;
        }

        if (_camera is not null)
        {
            _camera.Position = new Vector2(expeditionState.DropPosition.X, expeditionState.DropPosition.Y);
        }
        SetMessage($"{message} 单位 { _surfaceUnits.Count } 个。");
        GD.Print($"[地表] 单位实例化完成：{_surfaceUnits.Count}");
        RefreshSelectionUi();
        RunDebugSelfTestIfRequested(payload, expeditionState);
        RunGatherSelfTestIfRequested(payload, expeditionState);
        RunEconomySelfTestIfRequested(payload, expeditionState);
        RenderSurfaceStructures(expeditionState, gameRoot.DataRegistry, gameRoot.Session);
    }

    private void BuildUi()
    {
        Node2D mapLayer = new()
        {
            Name = "MapLayer"
        };
        AddChild(mapLayer);

        Sprite2D background = new()
        {
            Name = "SurfaceBackgroundWorld",
            Texture = UiAssets.LoadTexture(UiAssets.SurfaceBackground),
            Centered = true,
            ZIndex = -100
        };
        mapLayer.AddChild(background);

        _unitLayer = new Node2D
        {
            Name = "UnitLayer"
        };
        AddChild(_unitLayer);

        _mineralLayer = new Node2D
        {
            Name = "MineralLayer"
        };
        AddChild(_mineralLayer);

        _buildingLayer = new Node2D
        {
            Name = "BuildingLayer"
        };
        AddChild(_buildingLayer);

        _effectLayer = new Node2D
        {
            Name = "SurfaceEffectLayer"
        };
        AddChild(_effectLayer);
        _moveMarker = new Sprite2D
        {
            Name = "MoveMarker",
            Texture = UiAssets.LoadTexture("res://assets/effects/surface/command_move_marker.png"),
            Visible = false,
            ZIndex = 20
        };
        _effectLayer.AddChild(_moveMarker);

        _pathLine = new Line2D
        {
            Name = "PathLine",
            Width = 4f,
            DefaultColor = new Color(0.36f, 0.86f, 0.76f, 0.72f),
            Texture = UiAssets.LoadTexture("res://assets/effects/surface/path_line.png"),
            TextureMode = Line2D.LineTextureMode.Tile,
            Visible = false,
            ZIndex = 19
        };
        _effectLayer.AddChild(_pathLine);

        _camera = new Camera2D
        {
            Name = "SurfaceCamera",
            Zoom = Vector2.One
        };
        AddChild(_camera);
        _camera.MakeCurrent();

        _audioPlayer = new AudioStreamPlayer
        {
            Name = "SurfaceAudio"
        };
        AddChild(_audioPlayer);

        CanvasLayer uiLayer = new()
        {
            Name = "SurfaceUi",
            Layer = 10
        };
        AddChild(uiLayer);

        Control root = new()
        {
            Name = "SurfaceLayout",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        uiLayer.AddChild(root);

        _dragSelectFrame = UiAssets.CreateTextureRect("DragSelectFrame", "res://assets/ui/surface/selection/drag_select_frame.png", TextureRect.ExpandModeEnum.IgnoreSize);
        _dragSelectFrame.Visible = false;
        root.AddChild(_dragSelectFrame);

        VBoxContainer topBar = new()
        {
            Name = "TopStatusBar"
        };
        topBar.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        RegisterUiInputBlocker(topBar);
        root.AddChild(topBar);

        _statusLabel = new Label
        {
            Text = "地表远征",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        topBar.AddChild(_statusLabel);

        PanelContainer minimapPanel = new()
        {
            Name = "MinimapPanel",
            CustomMinimumSize = new Vector2(240, 180)
        };
        minimapPanel.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        minimapPanel.OffsetLeft = 16;
        minimapPanel.OffsetBottom = -16;
        minimapPanel.OffsetTop = -196;
        root.AddChild(minimapPanel);
        RegisterUiInputBlocker(minimapPanel);

        VBoxContainer minimapBox = new();
        minimapPanel.AddChild(minimapBox);
        TextureRect minimapIcon = UiAssets.CreateTextureRect("MinimapIcon", UiAssets.IconMinimap);
        minimapIcon.CustomMinimumSize = new Vector2(48, 48);
        minimapBox.AddChild(minimapIcon);
        minimapBox.AddChild(UiAssets.CreateSectionLabel("小地图框架"));
        minimapBox.AddChild(new Label { Text = "空投点 / 镜头框\n真实迷雾由第 7 步接管", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        PanelContainer bottomPanel = new()
        {
            Name = "SelectionPanel",
            CustomMinimumSize = new Vector2(610, 160)
        };
        bottomPanel.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        bottomPanel.OffsetLeft = 280;
        bottomPanel.OffsetRight = -300;
        bottomPanel.OffsetBottom = -16;
        bottomPanel.OffsetTop = -176;
        root.AddChild(bottomPanel);
        RegisterUiInputBlocker(bottomPanel);

        VBoxContainer selectionBox = new();
        bottomPanel.AddChild(selectionBox);
        selectionBox.AddChild(UiAssets.CreateSectionLabel("单位 / 建筑信息"));
        HBoxContainer selectionSummary = new()
        {
            Name = "SelectionSummary"
        };
        selectionBox.AddChild(selectionSummary);
        _selectionPortrait = UiAssets.CreateTextureRect("SelectionPortrait", "res://assets/ui/surface/status/command_failed.png", TextureRect.ExpandModeEnum.IgnoreSize);
        _selectionPortrait.CustomMinimumSize = new Vector2(72, 72);
        _selectionPortrait.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        selectionSummary.AddChild(_selectionPortrait);
        _selectionLabel = new Label
        {
            Text = "未选择单位",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        selectionSummary.AddChild(_selectionLabel);
        _behaviorModeButton = new Button
        {
            Text = "切换行为",
            Disabled = true
        };
        _behaviorModeButton.Pressed += CycleSelectedBehaviorMode;
        selectionBox.AddChild(_behaviorModeButton);
        _manifestLabel = new Label
        {
            Text = "初始单位：等待远征载荷\n携带物资：等待远征载荷",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        selectionBox.AddChild(_manifestLabel);

        GridContainer commandPanel = new()
        {
            Name = "CommandPanel",
            Columns = 3
        };
        commandPanel.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        commandPanel.OffsetLeft = -300;
        commandPanel.OffsetBottom = -16;
        commandPanel.OffsetTop = -276;
        commandPanel.OffsetRight = -16;
        root.AddChild(commandPanel);
        RegisterUiInputBlocker(commandPanel);
        BuildCommandButtons(commandPanel);
        BuildCatalogPanel(root);

        VBoxContainer messagePanel = new()
        {
            Name = "MessagePanel"
        };
        messagePanel.SetAnchorsPreset(Control.LayoutPreset.CenterRight);
        messagePanel.OffsetLeft = -300;
        messagePanel.OffsetRight = -16;
        messagePanel.OffsetTop = -140;
        messagePanel.OffsetBottom = 120;
        root.AddChild(messagePanel);
        RegisterUiInputBlocker(messagePanel);
        messagePanel.AddChild(UiAssets.CreateSectionLabel("消息和事件"));
        _messageLabel = new Label
        {
            Text = "当前没有必须立即响应的事件。",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        messagePanel.AddChild(_messageLabel);
        HBoxContainer statusIconRow = new()
        {
            Name = "StatusIconRow"
        };
        messagePanel.AddChild(statusIconRow);
        _groupBadge = UiAssets.CreateTextureRect("GroupBadge", "res://assets/ui/surface/groups/group_badge.png", TextureRect.ExpandModeEnum.IgnoreSize);
        _groupBadge.CustomMinimumSize = new Vector2(42, 42);
        statusIconRow.AddChild(_groupBadge);
        _commandFailedIcon = UiAssets.CreateTextureRect("CommandFailedIcon", "res://assets/ui/surface/status/command_failed.png", TextureRect.ExpandModeEnum.IgnoreSize);
        _commandFailedIcon.CustomMinimumSize = new Vector2(42, 42);
        _commandFailedIcon.Visible = false;
        statusIconRow.AddChild(_commandFailedIcon);

        Button backButton = new()
        {
            Text = "返回主入口"
        };
        backButton.Pressed += () => FindGameRoot()?.ShowMainMenu();
        topBar.AddChild(backButton);

        CanvasLayer debugLayer = new()
        {
            Name = "DebugOverlay",
            Layer = 20
        };
        AddChild(debugLayer);
    }

    private void BuildCommandButtons(GridContainer commandPanel)
    {
        foreach (KeyValuePair<string, string> command in CommandDisplayNames)
        {
            Button button = AddCommandButton(commandPanel, command.Value, CommandIconPaths[command.Key], () => HandleCommandButton(command.Key));
            RegisterCommandCursor(button, command.Key);
            _commandButtons[command.Key] = button;
        }

        AddCommandButton(commandPanel, "回归", UiAssets.IconCommand, () =>
        {
            GameRoot? gameRoot = FindGameRoot();
            if (gameRoot is null)
            {
                return;
            }

            ExpeditionState? expeditionState = gameRoot.Session.ActiveExpedition;
            if (expeditionState is null ||
                !expeditionState.RocketState.IsReadyToReturn ||
                !expeditionState.RocketState.LaunchConfirmed ||
                string.IsNullOrEmpty(expeditionState.RocketState.CargoInventoryId))
            {
                GD.PushWarning("[结算] 火箭尚未完成装载和发射确认，不能回归");
                PlaySurfaceAudio("command_failed");
                ShowCommandFailure("火箭尚未完成装载和发射确认，不能回归。");
                return;
            }

            ScenePayload returnPayload = CreateReturnPayload(gameRoot);
            gameRoot.NavigateTo(SceneId.ReturnSummary, returnPayload);
        });

        AddCommandButton(commandPanel, "取消", UiAssets.IconCommand, () =>
        {
            if (!CancelSelectedConstructionSite())
            {
                ClearSelection();
            }
        });
        RefreshCommandButtons();
    }

    private void BuildCatalogPanel(Control root)
    {
        _buildCatalogPanel = new PanelContainer
        {
            Name = "BuildCatalogPanel",
            Visible = false,
            CustomMinimumSize = new Vector2(304, 320)
        };
        _buildCatalogPanel.SetAnchorsPreset(Control.LayoutPreset.CenterRight);
        _buildCatalogPanel.OffsetLeft = -620;
        _buildCatalogPanel.OffsetRight = -316;
        _buildCatalogPanel.OffsetTop = -190;
        _buildCatalogPanel.OffsetBottom = 190;
        root.AddChild(_buildCatalogPanel);
        RegisterUiInputBlocker(_buildCatalogPanel);

        VBoxContainer box = new()
        {
            Name = "BuildCatalogBox"
        };
        _buildCatalogPanel.AddChild(box);
        box.AddChild(UiAssets.CreateSectionLabel("建筑目录"));

        ScrollContainer scroll = new()
        {
            Name = "BuildCatalogScroll",
            CustomMinimumSize = new Vector2(288, 250)
        };
        box.AddChild(scroll);
        _buildCatalogList = new VBoxContainer
        {
            Name = "BuildCatalogList"
        };
        scroll.AddChild(_buildCatalogList);

        Button closeButton = new()
        {
            Text = "关闭"
        };
        closeButton.Pressed += CancelPendingTargetCommand;
        box.AddChild(closeButton);
    }

    private void HandleMouseButton(InputEventMouseButton mouseButton)
    {
        if (mouseButton.ButtonIndex == MouseButton.WheelUp && mouseButton.Pressed)
        {
            AdjustZoom(-ZoomStep);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.WheelDown && mouseButton.Pressed)
        {
            AdjustZoom(ZoomStep);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (mouseButton.Pressed)
            {
                _isDraggingSelection = true;
                _dragStartViewport = mouseButton.Position;
                _dragStartWorld = GetGlobalMousePosition();
                UpdateDragFrame(mouseButton.Position);
            }
            else if (_isDraggingSelection)
            {
                CompleteSelection(mouseButton.Position);
            }

            GetViewport().SetInputAsHandled();
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
        {
            Vector2 worldPosition = GetGlobalMousePosition();
            if (HandlePendingTargetCommand(worldPosition))
            {
                GetViewport().SetInputAsHandled();
                return;
            }

            SurfaceMineralDepositView? mineralView = FindMineralAt(worldPosition);
            if (mineralView is not null)
            {
                IssueGatherCommand(mineralView.MineralDepositInstanceId);
                GetViewport().SetInputAsHandled();
                return;
            }

            IssueMoveCommand(worldPosition);
            GetViewport().SetInputAsHandled();
        }
    }

    private void HandleMouseMotion(InputEventMouseMotion mouseMotion)
    {
        if (_isDraggingSelection)
        {
            UpdateDragFrame(mouseMotion.Position);
            GetViewport().SetInputAsHandled();
        }
    }

    private void HandleKeyInput(InputEventKey keyEvent)
    {
        if (!keyEvent.Pressed || keyEvent.Echo)
        {
            return;
        }

        int groupIndex = GroupIndexFromKey(keyEvent.Keycode);
        if (groupIndex > 0)
        {
            if (keyEvent.CtrlPressed)
            {
                AssignControlGroup(groupIndex);
            }
            else
            {
                RecallControlGroup(groupIndex);
            }

            GetViewport().SetInputAsHandled();
            return;
        }

        if (keyEvent.Keycode == Key.Escape)
        {
            ClearSelection();
            GetViewport().SetInputAsHandled();
        }
    }

    private void CompleteSelection(Vector2 releaseViewport)
    {
        _isDraggingSelection = false;
        if (_dragSelectFrame is not null)
        {
            _dragSelectFrame.Visible = false;
        }

        Vector2 releaseWorld = GetGlobalMousePosition();
        if (_dragStartViewport.DistanceTo(releaseViewport) <= DragThreshold)
        {
            SelectAtPoint(releaseWorld);
            return;
        }

        Rect2 worldRect = NormalizeRect(_dragStartWorld, releaseWorld);
        List<string> selectedIds = new();
        foreach (SurfaceUnit unit in _surfaceUnits.Values)
        {
            if (worldRect.HasPoint(unit.Position))
            {
                selectedIds.Add(unit.UnitInstanceId);
            }
        }

        ApplySelection(selectedIds);
        if (selectedIds.Count > 0)
        {
            PlaySurfaceAudio("select_group");
            GD.Print($"[输入] 框选单位：{selectedIds.Count}");
        }
    }

    private void SelectAtPoint(Vector2 worldPosition)
    {
        SurfaceUnit? closestUnit = _surfaceUnits.Values
            .Where(unit => unit.ContainsWorldPosition(worldPosition))
            .OrderBy(unit => unit.Position.DistanceTo(worldPosition))
            .FirstOrDefault();
        if (closestUnit is null)
        {
            if (SelectStructureAtPoint(worldPosition))
            {
                return;
            }

            ClearSelection();
            return;
        }

        ApplySelection(new[] { closestUnit.UnitInstanceId });
        PlaySurfaceAudio("select_unit");
        GD.Print($"[输入] 选择单位：{closestUnit.UnitInstanceId}");
    }

    private void ApplySelection(IEnumerable<string> unitInstanceIds)
    {
        CancelPendingTargetCommand();
        ClearStructureSelection();
        List<string> ids = unitInstanceIds.Where(id => _surfaceUnits.ContainsKey(id)).Distinct().ToList();
        _selectionState.SetMany(ids);
        foreach (SurfaceUnit unit in _surfaceUnits.Values)
        {
            unit.SetSelected(_selectionState.Contains(unit.UnitInstanceId));
        }

        RefreshSelectionUi();
        RefreshCommandButtons();
    }

    private void ClearSelection()
    {
        CancelPendingTargetCommand();
        ClearStructureSelection();
        _selectionState.Clear();
        foreach (SurfaceUnit unit in _surfaceUnits.Values)
        {
            unit.SetSelected(false);
        }

        RefreshSelectionUi();
        RefreshCommandButtons();
    }

    private bool SelectStructureAtPoint(Vector2 worldPosition)
    {
        GameRoot? gameRoot = FindGameRoot();
        ExpeditionState? expeditionState = gameRoot?.Session.ActiveExpedition;
        if (gameRoot is null || expeditionState is null)
        {
            return false;
        }

        string constructionSiteId = FindConstructionSiteAt(worldPosition);
        if (!string.IsNullOrEmpty(constructionSiteId))
        {
            SelectStructure(constructionSiteId, string.Empty);
            PlaySurfaceAudio("select_unit");
            GD.Print($"[输入] 选择施工点：{constructionSiteId}");
            return true;
        }

        string buildingInstanceId = FindBuildingAt(worldPosition);
        if (!string.IsNullOrEmpty(buildingInstanceId))
        {
            SelectStructure(string.Empty, buildingInstanceId);
            PlaySurfaceAudio("select_unit");
            GD.Print($"[输入] 选择建筑：{buildingInstanceId}");
            return true;
        }

        return false;
    }

    private void SelectStructure(string constructionSiteId, string buildingInstanceId)
    {
        CancelPendingTargetCommand();
        _selectionState.Clear();
        foreach (SurfaceUnit unit in _surfaceUnits.Values)
        {
            unit.SetSelected(false);
        }

        _selectedConstructionSiteId = constructionSiteId;
        _selectedBuildingInstanceId = buildingInstanceId;
        RefreshSelectionUi();
        RefreshCommandButtons();
    }

    private void ClearStructureSelection()
    {
        _selectedConstructionSiteId = string.Empty;
        _selectedBuildingInstanceId = string.Empty;
    }

    private bool CancelSelectedConstructionSite()
    {
        if (string.IsNullOrEmpty(_selectedConstructionSiteId))
        {
            return false;
        }

        GameRoot? gameRoot = FindGameRoot();
        ExpeditionState? expeditionState = gameRoot?.Session.ActiveExpedition;
        if (gameRoot is null || expeditionState is null)
        {
            return false;
        }

        string constructionSiteId = _selectedConstructionSiteId;
        SurfaceConstructionSystem constructionSystem = new(gameRoot.Session, gameRoot.DataRegistry);
        if (!constructionSystem.TryCancelConstructionSite(
                expeditionState,
                constructionSiteId,
                out List<InventoryTransfer> _,
                out List<GroundItemState> _,
                out string message))
        {
            RecordTargetCommand(expeditionState, "build", "construction_site", constructionSiteId, ConstructionSitePosition(gameRoot.Session, constructionSiteId), "failed", message);
            PlaySurfaceAudio("command_failed");
            ShowCommandFailure(message);
            return true;
        }

        RecordTargetCommand(expeditionState, "build", "construction_site", constructionSiteId, ConstructionSitePosition(gameRoot.Session, constructionSiteId), "cancelled", string.Empty);
        SetMessage(message);
        ClearStructureSelection();
        RefreshSelectionUi();
        RefreshCommandButtons();
        RenderSurfaceStructures(expeditionState, gameRoot.DataRegistry, gameRoot.Session);
        return true;
    }

    private void IssueMoveCommand(Vector2 targetPosition)
    {
        GameRoot? gameRoot = FindGameRoot();
        ExpeditionState? expeditionState = gameRoot?.Session.ActiveExpedition;
        if (gameRoot is null || expeditionState is null || _selectionState.SelectedUnitInstanceIds.Count == 0)
        {
            PlaySurfaceAudio("command_failed");
            ShowCommandFailure("没有可移动的已选单位。");
            return;
        }

        UnitCommand command = new()
        {
            CommandType = "move",
            TargetType = "ground",
            TargetPosition = targetPosition,
            IssuedAt = Time.GetUnixTimeFromSystem(),
            ValidationState = "accepted"
        };
        command.SourceUnitInstanceIds.AddRange(_selectionState.SelectedUnitInstanceIds);

        foreach (string unitInstanceId in _selectionState.SelectedUnitInstanceIds)
        {
            if (_surfaceUnits.TryGetValue(unitInstanceId, out SurfaceUnit? surfaceUnit))
            {
                if (surfaceUnit.RuntimeState is not null)
                {
                    surfaceUnit.RuntimeState.CommandQueue.Clear();
                    surfaceUnit.RuntimeState.CommandQueue.Add(command);
                }

                surfaceUnit.IssueMove(targetPosition, command.CommandId);
            }
        }

        SurfaceCommandRecord record = new()
        {
            ExpeditionId = expeditionState.ExpeditionId,
            CommandId = command.CommandId,
            CommandType = command.CommandType,
            TargetType = command.TargetType,
            TargetPosition = command.TargetPosition,
            Result = "accepted",
            CreatedAt = Time.GetUnixTimeFromSystem()
        };
        record.UnitInstanceIds.AddRange(command.SourceUnitInstanceIds);
        expeditionState.SurfaceCommandRecords.Add(record);

        if (_moveMarker is not null)
        {
            _moveMarker.Position = targetPosition;
            _moveMarker.Visible = true;
        }

        if (_pathLine is not null)
        {
            _pathLine.ClearPoints();
            foreach (string unitInstanceId in _selectionState.SelectedUnitInstanceIds)
            {
                if (_surfaceUnits.TryGetValue(unitInstanceId, out SurfaceUnit? surfaceUnit))
                {
                    _pathLine.AddPoint(surfaceUnit.Position);
                    _pathLine.AddPoint(targetPosition);
                }
            }

            _pathLine.Visible = true;
        }

        PlaySurfaceAudio("command_move");
        SetMessage($"移动指令：{command.SourceUnitInstanceIds.Count} 个单位 -> ({targetPosition.X:0},{targetPosition.Y:0})");
        RefreshSelectionUi();
        GD.Print($"[指令] move {command.CommandId} 单位 {command.SourceUnitInstanceIds.Count} 目标 {targetPosition}");
    }

    private void IssueGatherCommand(string mineralDepositInstanceId)
    {
        GameRoot? gameRoot = FindGameRoot();
        ExpeditionState? expeditionState = gameRoot?.Session.ActiveExpedition;
        if (gameRoot is null || expeditionState is null || _selectionState.SelectedUnitInstanceIds.Count == 0)
        {
            PlaySurfaceAudio("command_failed");
            ShowCommandFailure("没有可执行采集的已选单位。");
            return;
        }

        if (!expeditionState.MineralDepositStates.TryGetValue(mineralDepositInstanceId, out MineralDepositInstance? mineralInstance))
        {
            PlaySurfaceAudio("command_failed");
            ShowCommandFailure($"找不到矿产点：{mineralDepositInstanceId}");
            return;
        }

        UnitCommand command = new()
        {
            CommandType = "gather",
            TargetType = "mineral_deposit",
            TargetId = mineralDepositInstanceId,
            TargetPosition = new Vector2(mineralInstance.Position.X, mineralInstance.Position.Y),
            IssuedAt = Time.GetUnixTimeFromSystem()
        };
        command.SourceUnitInstanceIds.AddRange(_selectionState.SelectedUnitInstanceIds);

        SurfaceMiningSystem miningSystem = new(gameRoot.Session, gameRoot.DataRegistry);
        bool success = miningSystem.TryGather(expeditionState, _selectionState.SelectedUnitInstanceIds, mineralDepositInstanceId, out GatherRecord gatherRecord, out string message);
        command.ValidationState = success ? "accepted" : "rejected";
        command.FailureReason = success ? string.Empty : message;
        SurfaceCommandRecord commandRecord = new()
        {
            ExpeditionId = expeditionState.ExpeditionId,
            CommandId = command.CommandId,
            CommandType = "gather",
            TargetType = "mineral_deposit",
            TargetId = mineralDepositInstanceId,
            TargetPosition = command.TargetPosition,
            Result = success ? gatherRecord.Result : "failed",
            FailureReason = success ? gatherRecord.FailureReason : message,
            CreatedAt = Time.GetUnixTimeFromSystem()
        };
        if (success && !string.IsNullOrEmpty(gatherRecord.UnitInstanceId))
        {
            commandRecord.UnitInstanceIds.Add(gatherRecord.UnitInstanceId);
        }
        else
        {
            commandRecord.UnitInstanceIds.AddRange(_selectionState.SelectedUnitInstanceIds);
        }

        expeditionState.SurfaceCommandRecords.Add(commandRecord);
        if (!success)
        {
            PlaySurfaceAudio("command_failed");
            ShowCommandFailure(message);
            return;
        }

        if (_surfaceUnits.TryGetValue(gatherRecord.UnitInstanceId, out SurfaceUnit? surfaceUnit) &&
            surfaceUnit.RuntimeState is not null)
        {
            surfaceUnit.RuntimeState.CommandQueue.Clear();
            surfaceUnit.RuntimeState.CommandQueue.Add(command);
            surfaceUnit.RuntimeState.CurrentCommandId = command.CommandId;
            surfaceUnit.RuntimeState.CurrentTargetPosition = command.TargetPosition;
            surfaceUnit.RuntimeState.MovementState = "gathering";
        }

        if (_mineralViews.TryGetValue(mineralDepositInstanceId, out SurfaceMineralDepositView? mineralView))
        {
            mineralView.Refresh();
        }

        PlaySurfaceAudio("gather_start", "mining");
        PlaySurfaceAudio("gather_complete", "mining");
        ShowGatherEffect(command.TargetPosition);
        SetMessage(message);
        RefreshSelectionUi();
    }

    private void HandleCommandButton(string commandId)
    {
        CancelPendingTargetCommand();
        if (commandId == "move")
        {
            SetMessage("右键地面下达移动指令。");
            return;
        }

        if (commandId == "stop")
        {
            StopSelectedUnits();
            return;
        }

        if (commandId == "gather")
        {
            SurfaceMineralDepositView? target = FindNearestMineralForSelection();
            if (target is null)
            {
                PlaySurfaceAudio("command_failed");
                ShowCommandFailure("没有可采集的已发现矿产点。");
                return;
            }

            IssueGatherCommand(target.MineralDepositInstanceId);
            return;
        }

        if (commandId == "build")
        {
            ShowBuildCatalog();
            return;
        }

        if (commandId == "haul")
        {
            BeginPendingTargetCommand("haul", string.Empty, "右键施工点创建补料物流并推进施工。");
            return;
        }

        if (commandId == "repair")
        {
            BeginPendingTargetCommand("repair", string.Empty, "右键受损建筑执行维修。");
            return;
        }

        SetMessage($"{CommandDisplayNames[commandId]} 指令入口已接入，具体效果由后续阶段接管。");
        PlaySurfaceAudio("command_failed");
        ShowCommandFailure($"{CommandDisplayNames[commandId]} 指令入口已接入，具体效果由后续阶段接管。");
        GD.Print($"[指令] {commandId} 目标系统未接入");
    }

    private void ShowBuildCatalog()
    {
        GameRoot? gameRoot = FindGameRoot();
        if (gameRoot is null || _buildCatalogPanel is null || _buildCatalogList is null)
        {
            return;
        }

        if (_selectionState.SelectedUnitInstanceIds.Count == 0)
        {
            PlaySurfaceAudio("command_failed");
            ShowCommandFailure("需要先选择执行建造的单位。");
            return;
        }

        foreach (Node child in _buildCatalogList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (BuildingData buildingData in gameRoot.DataRegistry.Buildings.Values.OrderBy(building => building.DisplayName))
        {
            bool hasBlueprint = HasBuildingBlueprint(gameRoot.Session, buildingData);
            Button button = new()
            {
                Text = $"{buildingData.DisplayName}{(hasBlueprint ? string.Empty : "（缺蓝图）")}\n{BuildCostText(buildingData)}",
                Icon = LoadOptionalTexture(buildingData.IconPath),
                CustomMinimumSize = new Vector2(284, 68),
                ExpandIcon = true,
                TooltipText = buildingData.Description
            };
            string buildingId = buildingData.Id;
            button.Pressed += () => SelectBuildCatalogEntry(buildingId);
            _buildCatalogList.AddChild(button);
        }

        _buildCatalogPanel.Visible = true;
        SetMessage("选择建筑后右键地面放置施工点。");
    }

    private void SelectBuildCatalogEntry(string buildingId)
    {
        GameRoot? gameRoot = FindGameRoot();
        if (gameRoot is null ||
            !gameRoot.DataRegistry.TryGetBuilding(buildingId, out BuildingData? buildingData) ||
            buildingData is null)
        {
            PlaySurfaceAudio("command_failed");
            ShowCommandFailure($"找不到建筑定义：{buildingId}");
            return;
        }

        if (!HasBuildingBlueprint(gameRoot.Session, buildingData))
        {
            PlaySurfaceAudio("command_failed");
            ShowCommandFailure($"缺少建筑蓝图：{buildingData.RequiresBlueprintId}");
            return;
        }

        HideBuildCatalog();
        BeginPendingTargetCommand("build", buildingData.Id, $"右键地面选择{buildingData.DisplayName}施工位置。");
    }

    private void HideBuildCatalog()
    {
        if (_buildCatalogPanel is not null)
        {
            _buildCatalogPanel.Visible = false;
        }
    }

    private void CancelPendingTargetCommand()
    {
        ClearPendingTargetCommand();
        HideBuildCatalog();
    }

    private static bool HasBuildingBlueprint(GameSession session, BuildingData buildingData)
    {
        return string.IsNullOrEmpty(buildingData.RequiresBlueprintId) ||
            session.OrbitState.UnlockedBlueprints.Contains(buildingData.RequiresBlueprintId);
    }

    private static string BuildCostText(BuildingData buildingData)
    {
        if (buildingData.BuildCost.Count == 0)
        {
            return "无需材料";
        }

        return string.Join("  ", buildingData.BuildCost.Select(stack => $"{stack.ItemId} x{stack.Count}"));
    }

    private void RenderMinerals(ExpeditionState expeditionState, DataRegistry registry)
    {
        if (_mineralLayer is null)
        {
            return;
        }

        foreach (SurfaceMineralDepositView existingView in _mineralViews.Values)
        {
            existingView.QueueFree();
        }

        _mineralViews.Clear();
        foreach (MineralDepositInstance mineralInstance in expeditionState.MineralDepositStates.Values)
        {
            if (!mineralInstance.IsDiscovered ||
                !registry.TryGetMineralDeposit(mineralInstance.MineralDepositId, out MineralDepositData? mineralData) ||
                mineralData is null)
            {
                continue;
            }

            SurfaceMineralDepositView view = new();
            view.Configure(mineralInstance, mineralData);
            _mineralLayer.AddChild(view);
            _mineralViews[mineralInstance.MineralDepositInstanceId] = view;
        }

        GD.Print($"[矿产] 地表矿产视图刷新：{_mineralViews.Count}");
    }

    private void RenderSurfaceStructures(ExpeditionState expeditionState, DataRegistry registry, GameSession session)
    {
        if (_buildingLayer is null)
        {
            return;
        }

        foreach (Node child in _buildingLayer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (string constructionSiteId in expeditionState.ConstructionSiteIds)
        {
            if (!session.ConstructionSites.TryGetValue(constructionSiteId, out ConstructionSiteState? constructionSite) ||
                !IsActiveConstructionSite(constructionSite) ||
                !registry.TryGetBuilding(constructionSite.BuildingId, out BuildingData? buildingData) ||
                buildingData is null)
            {
                continue;
            }

            AddStructureSprite(
                $"Construction_{constructionSite.ConstructionSiteId}",
                buildingData.ConstructionSpritePath,
                new Vector2(constructionSite.Position.X, constructionSite.Position.Y),
                $"{buildingData.DisplayName} 施工点");
        }

        foreach (string buildingInstanceId in expeditionState.BuildingInstanceIds)
        {
            if (!session.BuildingInstances.TryGetValue(buildingInstanceId, out BuildingInstance? buildingInstance) ||
                !registry.TryGetBuilding(buildingInstance.BuildingId, out BuildingData? buildingData) ||
                buildingData is null)
            {
                continue;
            }

            Node2D root = AddStructureSprite(
                $"Building_{buildingInstance.BuildingInstanceId}",
                buildingData.SpritePath,
                new Vector2(buildingInstance.Position.X, buildingInstance.Position.Y),
                $"{buildingData.DisplayName} {buildingInstance.PowerState}");
            string powerIconPath = $"res://assets/ui/surface/power/power_{buildingInstance.PowerState}.png";
            Texture2D? powerTexture = LoadOptionalTexture(powerIconPath);
            if (powerTexture is not null)
            {
                Sprite2D powerIcon = new()
                {
                    Name = "PowerState",
                    Texture = powerTexture,
                    Position = new Vector2(48f, -48f),
                    Scale = Vector2.One * 0.35f,
                    ZIndex = 12
                };
                root.AddChild(powerIcon);
            }
        }
    }

    private Node2D AddStructureSprite(string name, string texturePath, Vector2 position, string labelText)
    {
        Node2D root = new()
        {
            Name = name,
            Position = position
        };
        _buildingLayer?.AddChild(root);

        Texture2D? texture = UiAssets.LoadTexture(texturePath);
        Sprite2D sprite = new()
        {
            Name = "Sprite",
            Texture = texture,
            ZIndex = -2
        };
        if (texture is not null)
        {
            Vector2 textureSize = texture.GetSize();
            float largestSide = Mathf.Max(textureSize.X, textureSize.Y);
            sprite.Scale = largestSide > 0f ? Vector2.One * (110f / largestSide) : Vector2.One;
        }

        root.AddChild(sprite);
        Label label = new()
        {
            Name = "Label",
            Text = labelText,
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(-96f, 58f),
            CustomMinimumSize = new Vector2(192f, 24f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        root.AddChild(label);
        return root;
    }

    private bool HandlePendingTargetCommand(Vector2 worldPosition)
    {
        if (string.IsNullOrEmpty(_pendingTargetCommand))
        {
            return false;
        }

        string command = _pendingTargetCommand;
        string buildId = _pendingBuildBuildingId;
        ClearPendingTargetCommand();
        if (command == "build")
        {
            if (string.IsNullOrEmpty(buildId))
            {
                PlaySurfaceAudio("command_failed");
                ShowCommandFailure("需要先从建筑目录选择建筑。");
                return true;
            }

            CreateConstructionSiteAtPosition(worldPosition, buildId);
            return true;
        }

        if (command == "haul")
        {
            string constructionSiteId = FindConstructionSiteAt(worldPosition);
            if (string.IsNullOrEmpty(constructionSiteId))
            {
                PlaySurfaceAudio("command_failed");
                ShowCommandFailure("右键位置没有可补料施工点。");
                return true;
            }

            DeliverConstructionSite(constructionSiteId);
            return true;
        }

        if (command == "repair")
        {
            string buildingInstanceId = FindBuildingAt(worldPosition);
            if (string.IsNullOrEmpty(buildingInstanceId))
            {
                PlaySurfaceAudio("command_failed");
                ShowCommandFailure("右键位置没有可维修建筑。");
                return true;
            }

            RepairBuilding(buildingInstanceId);
            return true;
        }

        return false;
    }

    private void BeginPendingTargetCommand(string commandId, string buildBuildingId, string message)
    {
        if (_selectionState.SelectedUnitInstanceIds.Count == 0)
        {
            PlaySurfaceAudio("command_failed");
            ShowCommandFailure("需要先选择执行单位。");
            return;
        }

        _pendingTargetCommand = commandId;
        _pendingBuildBuildingId = buildBuildingId;
        SetMessage(message);
    }

    private void ClearPendingTargetCommand()
    {
        _pendingTargetCommand = string.Empty;
        _pendingBuildBuildingId = string.Empty;
    }

    private void CreateConstructionSiteAtPosition(Vector2 worldPosition, string buildingId)
    {
        GameRoot? gameRoot = FindGameRoot();
        ExpeditionState? expeditionState = gameRoot?.Session.ActiveExpedition;
        if (gameRoot is null || expeditionState is null)
        {
            return;
        }

        string assignedUnit = _selectionState.SelectedUnitInstanceIds.FirstOrDefault() ?? string.Empty;
        Vector2I position = new(Mathf.RoundToInt(worldPosition.X), Mathf.RoundToInt(worldPosition.Y));
        if (!gameRoot.DataRegistry.TryGetBuilding(buildingId, out BuildingData? buildingData) || buildingData is null)
        {
            string missingMessage = $"找不到建筑定义：{buildingId}";
            RecordTargetCommand(expeditionState, "build", "ground", buildingId, worldPosition, "failed", missingMessage);
            PlaySurfaceAudio("command_failed");
            ShowCommandFailure(missingMessage);
            return;
        }

        if (!ValidateBuildPlacement(expeditionState, gameRoot.Session, gameRoot.DataRegistry, buildingData, position, out string placementMessage))
        {
            RecordTargetCommand(expeditionState, "build", "ground", buildingId, worldPosition, "failed", placementMessage);
            PlaySurfaceAudio("command_failed");
            ShowCommandFailure(placementMessage);
            return;
        }

        SurfaceConstructionSystem constructionSystem = new(gameRoot.Session, gameRoot.DataRegistry);
        if (!constructionSystem.TryCreateConstructionSite(expeditionState, buildingId, position, assignedUnit, out ConstructionSiteState site, out string message))
        {
            RecordTargetCommand(expeditionState, "build", "ground", buildingId, worldPosition, "failed", message);
            PlaySurfaceAudio("command_failed");
            ShowCommandFailure(message);
            return;
        }

        RecordTargetCommand(expeditionState, "build", "construction_site", site.ConstructionSiteId, worldPosition, "accepted", string.Empty);
        PlaySurfaceAudio("place_building", "build");
        SetMessage($"{message}，等待物流送入材料。");
        RenderSurfaceStructures(expeditionState, gameRoot.DataRegistry, gameRoot.Session);
        GD.Print($"[建造] build 指令创建施工点：{site.ConstructionSiteId}");
    }

    private bool ValidateBuildPlacement(
        ExpeditionState expeditionState,
        GameSession session,
        DataRegistry registry,
        BuildingData buildingData,
        Vector2I position,
        out string message)
    {
        Vector2 targetPosition = new(position.X, position.Y);
        Vector2 dropPosition = new(expeditionState.DropPosition.X, expeditionState.DropPosition.Y);
        if (targetPosition.DistanceTo(dropPosition) > BuildableRadiusFromDrop)
        {
            message = "建造位置超出当前可操作地表范围。";
            return false;
        }

        float placementRadius = PlacementRadius(buildingData);
        foreach (string siteId in expeditionState.ConstructionSiteIds)
        {
            if (!session.ConstructionSites.TryGetValue(siteId, out ConstructionSiteState? site) ||
                !IsActiveConstructionSite(site))
            {
                continue;
            }

            float otherRadius = registry.TryGetBuilding(site.BuildingId, out BuildingData? otherData) && otherData is not null
                ? PlacementRadius(otherData)
                : StructureHitRadius;
            if (new Vector2(site.Position.X, site.Position.Y).DistanceTo(targetPosition) < placementRadius + otherRadius)
            {
                message = "建造位置与施工点重叠。";
                return false;
            }
        }

        foreach (string buildingInstanceId in expeditionState.BuildingInstanceIds)
        {
            if (!session.BuildingInstances.TryGetValue(buildingInstanceId, out BuildingInstance? buildingInstance))
            {
                continue;
            }

            float otherRadius = registry.TryGetBuilding(buildingInstance.BuildingId, out BuildingData? otherData) && otherData is not null
                ? PlacementRadius(otherData)
                : StructureHitRadius;
            if (new Vector2(buildingInstance.Position.X, buildingInstance.Position.Y).DistanceTo(targetPosition) < placementRadius + otherRadius)
            {
                message = "建造位置与已建建筑重叠。";
                return false;
            }
        }

        foreach (MineralDepositInstance mineral in expeditionState.MineralDepositStates.Values)
        {
            if (!mineral.IsDiscovered || mineral.IsDepleted)
            {
                continue;
            }

            if (new Vector2(mineral.Position.X, mineral.Position.Y).DistanceTo(targetPosition) < placementRadius + MineralPlacementSpacing)
            {
                message = "建造位置阻挡矿产点。";
                return false;
            }
        }

        foreach (SurfaceUnit unit in _surfaceUnits.Values)
        {
            if (unit.Position.DistanceTo(targetPosition) < placementRadius + UnitPlacementSpacing)
            {
                message = "建造位置被单位占用。";
                return false;
            }
        }

        message = string.Empty;
        return true;
    }

    private static float PlacementRadius(BuildingData buildingData)
    {
        float longestSide = Mathf.Max((float)buildingData.Footprint.X, (float)buildingData.Footprint.Y);
        return Mathf.Max(56f, longestSide * 32f);
    }

    private void DeliverConstructionSite(string siteId)
    {
        GameRoot? gameRoot = FindGameRoot();
        ExpeditionState? expeditionState = gameRoot?.Session.ActiveExpedition;
        if (gameRoot is null || expeditionState is null)
        {
            return;
        }

        SurfaceConstructionSystem constructionSystem = new(gameRoot.Session, gameRoot.DataRegistry);
        if (!constructionSystem.TryDeliverConstructionMaterials(expeditionState, siteId, out string deliveryMessage))
        {
            RecordTargetCommand(expeditionState, "haul", "construction_site", siteId, ConstructionSitePosition(gameRoot.Session, siteId), "failed", deliveryMessage);
            PlaySurfaceAudio("command_failed");
            ShowCommandFailure(deliveryMessage);
            return;
        }

        if (!constructionSystem.TryCompleteConstruction(expeditionState, siteId, out BuildingInstance _, out string completeMessage))
        {
            RecordTargetCommand(expeditionState, "haul", "construction_site", siteId, ConstructionSitePosition(gameRoot.Session, siteId), "failed", completeMessage);
            PlaySurfaceAudio("command_failed");
            ShowCommandFailure(completeMessage);
            return;
        }

        RecordTargetCommand(expeditionState, "haul", "construction_site", siteId, ConstructionSitePosition(gameRoot.Session, siteId), "completed", string.Empty);
        constructionSystem.RecalculatePowerNetwork(expeditionState);
        PlaySurfaceAudio("build_complete", "build");
        SetMessage(completeMessage);
        RenderSurfaceStructures(expeditionState, gameRoot.DataRegistry, gameRoot.Session);
    }

    private void RepairBuilding(string buildingInstanceId)
    {
        GameRoot? gameRoot = FindGameRoot();
        ExpeditionState? expeditionState = gameRoot?.Session.ActiveExpedition;
        if (gameRoot is null || expeditionState is null)
        {
            return;
        }

        string unitInstanceId = _selectionState.SelectedUnitInstanceIds.FirstOrDefault() ?? expeditionState.ActiveUnitInstanceIds.FirstOrDefault() ?? string.Empty;
        if (string.IsNullOrEmpty(unitInstanceId))
        {
            PlaySurfaceAudio("command_failed");
            ShowCommandFailure("没有可执行维修的单位。");
            return;
        }

        SurfaceConstructionSystem constructionSystem = new(gameRoot.Session, gameRoot.DataRegistry);
        if (!constructionSystem.TryRepairBuilding(expeditionState, buildingInstanceId, unitInstanceId, out RepairRecord _, out string message))
        {
            RecordTargetCommand(expeditionState, "repair", "building_friendly", buildingInstanceId, BuildingPosition(gameRoot.Session, buildingInstanceId), "failed", message);
            PlaySurfaceAudio("command_failed");
            ShowCommandFailure(message);
            return;
        }

        RecordTargetCommand(expeditionState, "repair", "building_friendly", buildingInstanceId, BuildingPosition(gameRoot.Session, buildingInstanceId), "completed", string.Empty);
        SetMessage(message);
        RenderSurfaceStructures(expeditionState, gameRoot.DataRegistry, gameRoot.Session);
    }

    private void RecordTargetCommand(
        ExpeditionState expeditionState,
        string commandType,
        string targetType,
        string targetId,
        Vector2 targetPosition,
        string result,
        string failureReason)
    {
        UnitCommand command = new()
        {
            CommandType = commandType,
            TargetType = targetType,
            TargetId = targetId,
            TargetPosition = targetPosition,
            IssuedAt = Time.GetUnixTimeFromSystem(),
            ValidationState = result == "failed" ? "rejected" : "accepted",
            FailureReason = failureReason
        };
        command.SourceUnitInstanceIds.AddRange(_selectionState.SelectedUnitInstanceIds);
        SurfaceCommandRecord record = new()
        {
            ExpeditionId = expeditionState.ExpeditionId,
            CommandId = command.CommandId,
            CommandType = commandType,
            TargetType = targetType,
            TargetId = targetId,
            TargetPosition = targetPosition,
            Result = result,
            FailureReason = failureReason,
            CreatedAt = Time.GetUnixTimeFromSystem()
        };
        record.UnitInstanceIds.AddRange(command.SourceUnitInstanceIds);
        expeditionState.SurfaceCommandRecords.Add(record);
        foreach (string unitInstanceId in _selectionState.SelectedUnitInstanceIds)
        {
            if (_surfaceUnits.TryGetValue(unitInstanceId, out SurfaceUnit? surfaceUnit) && surfaceUnit.RuntimeState is not null)
            {
                surfaceUnit.RuntimeState.CommandQueue.Clear();
                surfaceUnit.RuntimeState.CommandQueue.Add(command);
                surfaceUnit.RuntimeState.CurrentCommandId = command.CommandId;
                surfaceUnit.RuntimeState.CurrentTargetPosition = targetPosition;
                surfaceUnit.RuntimeState.MovementState = commandType;
            }
        }
    }

    private string FindConstructionSiteAt(Vector2 worldPosition)
    {
        GameSession? session = FindGameRoot()?.Session;
        ExpeditionState? expeditionState = session?.ActiveExpedition;
        if (session is null || expeditionState is null)
        {
            return string.Empty;
        }

        return expeditionState.ConstructionSiteIds
            .Select(siteId => session.ConstructionSites.TryGetValue(siteId, out ConstructionSiteState? site) ? site : null)
            .Where(site => site is not null && IsActiveConstructionSite(site))
            .OrderBy(site => new Vector2(site!.Position.X, site.Position.Y).DistanceTo(worldPosition))
            .FirstOrDefault(site => new Vector2(site!.Position.X, site.Position.Y).DistanceTo(worldPosition) <= StructureHitRadius)
            ?.ConstructionSiteId ?? string.Empty;
    }

    private string FindBuildingAt(Vector2 worldPosition)
    {
        GameSession? session = FindGameRoot()?.Session;
        ExpeditionState? expeditionState = session?.ActiveExpedition;
        if (session is null || expeditionState is null)
        {
            return string.Empty;
        }

        return expeditionState.BuildingInstanceIds
            .Select(buildingId => session.BuildingInstances.TryGetValue(buildingId, out BuildingInstance? building) ? building : null)
            .Where(building => building is not null)
            .OrderBy(building => new Vector2(building!.Position.X, building.Position.Y).DistanceTo(worldPosition))
            .FirstOrDefault(building => new Vector2(building!.Position.X, building.Position.Y).DistanceTo(worldPosition) <= StructureHitRadius)
            ?.BuildingInstanceId ?? string.Empty;
    }

    private static Vector2 ConstructionSitePosition(GameSession session, string constructionSiteId)
    {
        return session.ConstructionSites.TryGetValue(constructionSiteId, out ConstructionSiteState? site)
            ? new Vector2(site.Position.X, site.Position.Y)
            : Vector2.Zero;
    }

    private static bool IsActiveConstructionSite(ConstructionSiteState site)
    {
        return site.State is not ("completed" or "cancelled");
    }

    private static Vector2 BuildingPosition(GameSession session, string buildingInstanceId)
    {
        return session.BuildingInstances.TryGetValue(buildingInstanceId, out BuildingInstance? building)
            ? new Vector2(building.Position.X, building.Position.Y)
            : Vector2.Zero;
    }

    private SurfaceMineralDepositView? FindMineralAt(Vector2 worldPosition)
    {
        return _mineralViews.Values
            .Where(view => view.ContainsWorldPosition(worldPosition))
            .OrderBy(view => view.Position.DistanceTo(worldPosition))
            .FirstOrDefault();
    }

    private SurfaceMineralDepositView? FindNearestMineralForSelection()
    {
        if (_selectionState.SelectedUnitInstanceIds.Count == 0)
        {
            return null;
        }

        Vector2 selectionCenter = Vector2.Zero;
        int count = 0;
        foreach (string unitInstanceId in _selectionState.SelectedUnitInstanceIds)
        {
            if (_surfaceUnits.TryGetValue(unitInstanceId, out SurfaceUnit? unit))
            {
                selectionCenter += unit.Position;
                count++;
            }
        }

        if (count == 0)
        {
            return null;
        }

        selectionCenter /= count;
        return _mineralViews.Values
            .Where(view => !view.IsDepleted)
            .OrderBy(view => view.Position.DistanceTo(selectionCenter))
            .FirstOrDefault();
    }

    private void ShowGatherEffect(Vector2 position)
    {
        if (_effectLayer is null)
        {
            return;
        }

        Texture2D? texture = LoadOptionalTexture("res://assets/effects/surface/gather/gather_complete.png")
            ?? LoadOptionalTexture("res://assets/ui/surface/gather/gather_complete.png");
        if (texture is null)
        {
            return;
        }

        Sprite2D effect = new()
        {
            Name = "GatherCompleteEffect",
            Texture = texture,
            Position = position,
            ZIndex = 30
        };
        _effectLayer.AddChild(effect);
        Timer timer = new()
        {
            OneShot = true,
            WaitTime = 1.0
        };
        effect.AddChild(timer);
        timer.Timeout += effect.QueueFree;
        timer.Start();
    }

    private void StopSelectedUnits()
    {
        foreach (string unitInstanceId in _selectionState.SelectedUnitInstanceIds)
        {
            if (_surfaceUnits.TryGetValue(unitInstanceId, out SurfaceUnit? surfaceUnit) &&
                surfaceUnit.RuntimeState is not null &&
                surfaceUnit.UnitInstance is not null)
            {
                surfaceUnit.RuntimeState.MovementState = "idle";
                surfaceUnit.UnitInstance.CurrentCommand = "stop";
            }
        }

        SetMessage("已下达停止指令。");
        GD.Print("[指令] stop");
    }

    private void AssignControlGroup(int groupIndex)
    {
        GameRoot? gameRoot = FindGameRoot();
        ExpeditionState? expeditionState = gameRoot?.Session.ActiveExpedition;
        if (expeditionState is null || _selectionState.SelectedUnitInstanceIds.Count == 0)
        {
            PlaySurfaceAudio("command_failed");
            ShowCommandFailure("没有可保存到编组的已选单位。");
            return;
        }

        foreach (SurfaceUnitRuntimeState runtimeState in expeditionState.UnitRuntimeStates.Values)
        {
            runtimeState.SelectionGroupIds.Remove(groupIndex);
        }

        ControlGroupState groupState = new()
        {
            GroupIndex = groupIndex,
            UpdatedAt = Time.GetUnixTimeFromSystem()
        };
        groupState.UnitInstanceIds.AddRange(_selectionState.SelectedUnitInstanceIds);
        expeditionState.ControlGroupStates[groupIndex] = groupState;

        foreach (string unitInstanceId in _selectionState.SelectedUnitInstanceIds)
        {
            if (expeditionState.UnitRuntimeStates.TryGetValue(unitInstanceId, out SurfaceUnitRuntimeState? runtimeState) &&
                !runtimeState.SelectionGroupIds.Contains(groupIndex))
            {
                runtimeState.SelectionGroupIds.Add(groupIndex);
            }
        }

        PlaySurfaceAudio("group_assign");
        if (_groupBadge is not null)
        {
            _groupBadge.TooltipText = $"编组 {groupIndex}: {groupState.UnitInstanceIds.Count} 个单位";
        }

        SetMessage($"编组 {groupIndex} 已保存：{groupState.UnitInstanceIds.Count} 个单位。");
        GD.Print($"[输入] 保存编组 {groupIndex}");
    }

    private void RecallControlGroup(int groupIndex)
    {
        ExpeditionState? expeditionState = FindGameRoot()?.Session.ActiveExpedition;
        if (expeditionState is null || !expeditionState.ControlGroupStates.TryGetValue(groupIndex, out ControlGroupState? groupState))
        {
            PlaySurfaceAudio("command_failed");
            ShowCommandFailure($"编组 {groupIndex} 为空。");
            return;
        }

        List<string> validUnitIds = groupState.UnitInstanceIds.Where(unitInstanceId => _surfaceUnits.ContainsKey(unitInstanceId)).ToList();
        groupState.UnitInstanceIds.Clear();
        groupState.UnitInstanceIds.AddRange(validUnitIds);
        if (validUnitIds.Count == 0)
        {
            expeditionState.ControlGroupStates.Remove(groupIndex);
            PlaySurfaceAudio("command_failed");
            ShowCommandFailure($"编组 {groupIndex} 已失效。");
            return;
        }

        ApplySelection(validUnitIds);
        PlaySurfaceAudio("group_recall");
        SetMessage($"召回编组 {groupIndex}。");
        GD.Print($"[输入] 召回编组 {groupIndex}");
    }

    private void RefreshSelectionUi()
    {
        if (_selectionLabel is null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(_selectedConstructionSiteId) && TryRefreshConstructionSiteSelection())
        {
            return;
        }

        if (!string.IsNullOrEmpty(_selectedBuildingInstanceId) && TryRefreshBuildingSelection())
        {
            return;
        }

        if (_selectionState.SelectedUnitInstanceIds.Count == 0)
        {
            _selectionLabel.Text = "未选择单位";
            if (_selectionPortrait is not null)
            {
                _selectionPortrait.Texture = UiAssets.LoadTexture("res://assets/ui/surface/status/command_failed.png");
            }

            if (_behaviorModeButton is not null)
            {
                _behaviorModeButton.Disabled = true;
                _behaviorModeButton.Text = "切换行为";
            }

            return;
        }

        if (_behaviorModeButton is not null)
        {
            _behaviorModeButton.Disabled = false;
            _behaviorModeButton.Text = "切换行为模式";
        }

        if (_selectionState.SelectedUnitInstanceIds.Count == 1 &&
            _surfaceUnits.TryGetValue(_selectionState.SelectedUnitInstanceIds[0], out SurfaceUnit? unit) &&
            unit.UnitInstance is not null &&
            unit.UnitData is not null)
        {
            if (_selectionPortrait is not null)
            {
                _selectionPortrait.Texture = UiAssets.LoadTexture(unit.UnitData.PortraitPath);
            }

            _selectionLabel.Text =
                $"{unit.DisplayName()}\n耐久 {unit.UnitInstance.Durability}/{unit.UnitData.BaseDurability}  能源 {unit.UnitInstance.Energy}/{unit.UnitData.BaseEnergy}\n行为 {unit.UnitInstance.BehaviorMode}  指令 {unit.UnitInstance.CurrentCommand}";
            return;
        }

        if (_selectionPortrait is not null)
        {
            _selectionPortrait.Texture = UiAssets.LoadTexture("res://assets/ui/surface/groups/group_badge.png");
        }

        _selectionLabel.Text = $"已选择 {_selectionState.SelectedUnitInstanceIds.Count} 个单位\n可用命令取交集显示。";
    }

    private bool TryRefreshConstructionSiteSelection()
    {
        GameRoot? gameRoot = FindGameRoot();
        ExpeditionState? expeditionState = gameRoot?.Session.ActiveExpedition;
        if (gameRoot is null ||
            expeditionState is null ||
            string.IsNullOrEmpty(_selectedConstructionSiteId) ||
            !gameRoot.Session.ConstructionSites.TryGetValue(_selectedConstructionSiteId, out ConstructionSiteState? site) ||
            !IsActiveConstructionSite(site) ||
            !gameRoot.DataRegistry.TryGetBuilding(site.BuildingId, out BuildingData? buildingData) ||
            buildingData is null)
        {
            ClearStructureSelection();
            return false;
        }

        if (_selectionPortrait is not null)
        {
            _selectionPortrait.Texture = UiAssets.LoadTexture(buildingData.ConstructionSpritePath);
        }

        if (_behaviorModeButton is not null)
        {
            _behaviorModeButton.Disabled = true;
            _behaviorModeButton.Text = "施工点";
        }

        _selectionLabel!.Text =
            $"{buildingData.DisplayName} 施工点\n状态 {site.State}  进度 {site.ConstructionProgress * 100f:0}%\n需求 {StackSummary(site.RequiredItems)}\n已送达 {InventorySummary(gameRoot.Session, site.DeliveredInventoryId)}";
        return true;
    }

    private bool TryRefreshBuildingSelection()
    {
        GameRoot? gameRoot = FindGameRoot();
        ExpeditionState? expeditionState = gameRoot?.Session.ActiveExpedition;
        if (gameRoot is null ||
            expeditionState is null ||
            string.IsNullOrEmpty(_selectedBuildingInstanceId) ||
            !gameRoot.Session.BuildingInstances.TryGetValue(_selectedBuildingInstanceId, out BuildingInstance? building) ||
            !gameRoot.DataRegistry.TryGetBuilding(building.BuildingId, out BuildingData? buildingData) ||
            buildingData is null)
        {
            ClearStructureSelection();
            return false;
        }

        if (_selectionPortrait is not null)
        {
            _selectionPortrait.Texture = UiAssets.LoadTexture(buildingData.IconPath);
        }

        if (_behaviorModeButton is not null)
        {
            _behaviorModeButton.Disabled = true;
            _behaviorModeButton.Text = "建筑";
        }

        ProductionJobState? latestJob = expeditionState.ProductionJobs
            .LastOrDefault(job => job.BuildingInstanceId == building.BuildingInstanceId);
        string productionText = latestJob is null
            ? "无生产任务"
            : $"{latestJob.RecipeId} {latestJob.State} {latestJob.Progress * 100f:0}%";
        _selectionLabel!.Text =
            $"{buildingData.DisplayName}\n耐久 {building.Durability}/{building.MaxDurability}  电力 {building.PowerState}\n施工 {building.ConstructionProgress * 100f:0}%  生产 {productionText}\n输入 {InventorySummary(gameRoot.Session, building.InputInventoryId)}\n输出 {InventorySummary(gameRoot.Session, building.OutputInventoryId)}";
        return true;
    }

    private static string StackSummary(IEnumerable<ItemStack> stacks)
    {
        List<string> parts = stacks
            .Where(stack => stack.Count > 0)
            .Select(stack => $"{stack.ItemId} x{stack.Count}")
            .ToList();
        return parts.Count == 0 ? "无" : string.Join("  ", parts);
    }

    private static string InventorySummary(GameSession session, string inventoryId)
    {
        if (string.IsNullOrEmpty(inventoryId) ||
            !session.Inventories.TryGetValue(inventoryId, out InventoryContainer? inventory))
        {
            return "无";
        }

        if (inventory.ItemStacks.Count == 0 && inventory.ItemInstanceIds.Count == 0)
        {
            return "空";
        }

        List<string> parts = inventory.ItemStacks
            .Where(stack => stack.Count > 0)
            .Select(stack => $"{stack.ItemId} x{stack.Count}")
            .ToList();
        if (inventory.ItemInstanceIds.Count > 0)
        {
            parts.Add($"实例 x{inventory.ItemInstanceIds.Count}");
        }

        return string.Join("  ", parts);
    }

    private void CycleSelectedBehaviorMode()
    {
        string[] modes = { "balanced", "work", "scout", "support", "hold" };
        if (_selectionState.SelectedUnitInstanceIds.Count == 0)
        {
            return;
        }

        string firstMode = "balanced";
        string firstUnitId = _selectionState.SelectedUnitInstanceIds[0];
        if (_surfaceUnits.TryGetValue(firstUnitId, out SurfaceUnit? firstUnit) &&
            firstUnit.UnitInstance is not null)
        {
            firstMode = firstUnit.UnitInstance.BehaviorMode;
        }

        int nextIndex = System.Array.IndexOf(modes, firstMode) + 1;
        if (nextIndex <= 0 || nextIndex >= modes.Length)
        {
            nextIndex = 0;
        }

        string nextMode = modes[nextIndex];
        foreach (string unitInstanceId in _selectionState.SelectedUnitInstanceIds)
        {
            if (_surfaceUnits.TryGetValue(unitInstanceId, out SurfaceUnit? surfaceUnit) &&
                surfaceUnit.UnitInstance is not null)
            {
                surfaceUnit.UnitInstance.BehaviorMode = nextMode;
            }
        }

        SetMessage($"行为模式切换为：{nextMode}");
        RefreshSelectionUi();
        GD.Print($"[单位] 行为模式切换：{nextMode}");
    }

    private void RunDebugSelfTestIfRequested(ScenePayload payload, ExpeditionState expeditionState)
    {
        if (!payload.DebugEnabled || !HasArgument("--surface-self-test"))
        {
            return;
        }

        if (_surfaceUnits.Count == 0)
        {
            GD.PushError("[调试] 地表自检失败：没有可实例化单位");
            QuitSelfTestFailure();
            return;
        }

        ApplySelection(_surfaceUnits.Keys);
        Vector2 target = new(expeditionState.DropPosition.X + 128f, expeditionState.DropPosition.Y - 64f);
        IssueMoveCommand(target);
        AssignControlGroup(1);
        RecallControlGroup(1);
        CycleSelectedBehaviorMode();

        int movingCount = expeditionState.UnitRuntimeStates.Values.Count(state => state.MovementState == "moving");
        if (movingCount == 0 || expeditionState.SurfaceCommandRecords.Count == 0 || !expeditionState.ControlGroupStates.ContainsKey(1))
        {
            GD.PushError("[调试] 地表自检失败：移动、指令记录或编组状态未写回");
            QuitSelfTestFailure();
            return;
        }

        GD.Print($"[调试] 地表自检完成：选择 {_selectionState.SelectedUnitInstanceIds.Count}，移动 {movingCount}，编组 1");
    }

    private void RunGatherSelfTestIfRequested(ScenePayload payload, ExpeditionState expeditionState)
    {
        if (!payload.DebugEnabled || !HasArgument("--surface-gather-self-test"))
        {
            return;
        }

        GameRoot? gameRoot = FindGameRoot();
        if (gameRoot is null)
        {
            GD.PushError("[调试] 采集自检失败：缺少 GameRoot");
            QuitSelfTestFailure();
            return;
        }

        SurfaceUnit? gatherUnit = _surfaceUnits.Values.FirstOrDefault(unit => unit.UnitData?.AvailableCommands.Contains("gather") == true);
        SurfaceMineralDepositView? mineralView = _mineralViews.Values.FirstOrDefault(view => !view.IsDepleted);
        if (gatherUnit is null || mineralView is null)
        {
            GD.PushError("[调试] 采集自检失败：缺少可采集单位或矿产点");
            QuitSelfTestFailure();
            return;
        }

        MineralDepositInstance mineralBefore = expeditionState.MineralDepositStates[mineralView.MineralDepositInstanceId];
        if (!gameRoot.DataRegistry.TryGetMineralDeposit(mineralBefore.MineralDepositId, out MineralDepositData? mineralData) || mineralData is null)
        {
            GD.PushError("[调试] 采集自检失败：矿产点定义缺失");
            QuitSelfTestFailure();
            return;
        }

        int remainingBefore = mineralBefore.RemainingYield;
        int recordsBefore = expeditionState.GatherRecords.Count;
        int transfersBefore = gameRoot.Session.InventoryTransfers.Count;
        int groundBefore = expeditionState.GroundItemStateIds.Count;
        InventoryContainer? orbitInventory = gameRoot.Session.Inventories.GetValueOrDefault(gameRoot.Session.OrbitState.InventoryId);
        int orbitBefore = orbitInventory?.GetItemCount(mineralData.YieldItemId) ?? 0;

        ApplySelection(new[] { gatherUnit.UnitInstanceId });
        IssueGatherCommand(mineralView.MineralDepositInstanceId);

        GatherRecord? latestRecord = expeditionState.GatherRecords.LastOrDefault();
        int orbitAfter = orbitInventory?.GetItemCount(mineralData.YieldItemId) ?? 0;
        bool wroteConcreteLocation = gameRoot.Session.InventoryTransfers.Count > transfersBefore ||
            expeditionState.GroundItemStateIds.Count > groundBefore;
        if (mineralBefore.RemainingYield >= remainingBefore ||
            expeditionState.GatherRecords.Count <= recordsBefore ||
            latestRecord is null ||
            latestRecord.Result is not ("inventory" or "ground_item") ||
            !wroteConcreteLocation ||
            orbitAfter != orbitBefore)
        {
            GD.PushError("[调试] 采集自检失败：矿产剩余量、记录、具体位置或轨道库存边界不符合预期");
            QuitSelfTestFailure();
            return;
        }

        GD.Print($"[调试] 采集自检完成：{latestRecord.ItemId} x{latestRecord.Count} -> {latestRecord.Result}");
    }

    private void RunEconomySelfTestIfRequested(ScenePayload payload, ExpeditionState expeditionState)
    {
        if (!payload.DebugEnabled || !HasArgument("--surface-economy-self-test"))
        {
            return;
        }

        GameRoot? gameRoot = FindGameRoot();
        SurfaceUnit? gatherUnit = _surfaceUnits.Values.FirstOrDefault(unit => unit.UnitData?.AvailableCommands.Contains("gather") == true);
        if (gameRoot is null || gatherUnit is null)
        {
            GD.PushError("[调试] 经济自检失败：缺少 GameRoot 或采集单位");
            QuitSelfTestFailure();
            return;
        }

        SurfaceMiningSystem miningSystem = new(gameRoot.Session, gameRoot.DataRegistry);
        for (int index = 0; index < 5; index++)
        {
            if (!TryGatherSpecificMineralForSelfTest(expeditionState, miningSystem, gatherUnit.UnitInstanceId, "mineral_metal_deposit_basic", out string gatherMessage))
            {
                GD.PushError($"[调试] 经济自检失败：{gatherMessage}");
                QuitSelfTestFailure();
                return;
            }
        }

        ApplySelection(new[] { gatherUnit.UnitInstanceId });
        SurfaceConstructionSystem constructionSystem = new(gameRoot.Session, gameRoot.DataRegistry, allowDebugBlueprintBypass: true);
        int transfersBefore = gameRoot.Session.InventoryTransfers.Count;
        int logisticsBefore = expeditionState.LogisticsOrderIds.Count;
        int commandRecordsBefore = expeditionState.SurfaceCommandRecords.Count;
        Vector2I solarPosition = expeditionState.DropPosition + new Vector2I(-310, 250);
        CreateConstructionSiteAtPosition(new Vector2(solarPosition.X, solarPosition.Y), "solar_panel");
        string solarSiteId = FindConstructionSiteAt(new Vector2(solarPosition.X, solarPosition.Y));
        if (string.IsNullOrEmpty(solarSiteId))
        {
            GD.PushError("[调试] 经济自检失败：目标建造指令没有创建太阳能板施工点");
            QuitSelfTestFailure();
            return;
        }

        SelectStructure(solarSiteId, string.Empty);
        if (_selectionLabel is null ||
            !_selectionLabel.Text.Contains("施工点") ||
            !_selectionLabel.Text.Contains("需求"))
        {
            GD.PushError("[调试] 经济自检失败：施工点详情未刷新");
            QuitSelfTestFailure();
            return;
        }

        ApplySelection(new[] { gatherUnit.UnitInstanceId });
        DeliverConstructionSite(solarSiteId);
        BuildingInstance? solarPanel = FindCompletedBuildingForSelfTest(gameRoot.Session, expeditionState, "solar_panel", solarPosition);
        if (solarPanel is null)
        {
            GD.PushError("[调试] 经济自检失败：目标补料指令没有完成太阳能板");
            QuitSelfTestFailure();
            return;
        }

        ApplySelection(new[] { gatherUnit.UnitInstanceId });
        Vector2I cancelPosition = expeditionState.DropPosition + new Vector2I(520, 260);
        CreateConstructionSiteAtPosition(new Vector2(cancelPosition.X, cancelPosition.Y), "storage_box");
        string cancelSiteId = FindConstructionSiteAt(new Vector2(cancelPosition.X, cancelPosition.Y));
        string cancelDeliveryMessage = string.Empty;
        if (string.IsNullOrEmpty(cancelSiteId) ||
            !constructionSystem.TryDeliverConstructionMaterials(expeditionState, cancelSiteId, out cancelDeliveryMessage))
        {
            GD.PushError($"[调试] 经济自检失败：取消施工预置失败：{cancelDeliveryMessage}");
            QuitSelfTestFailure();
            return;
        }

        int metalBeforeCancel = CountExpeditionStack(gameRoot.Session, expeditionState, "metal");
        int scrapBeforeCancel = CountExpeditionStack(gameRoot.Session, expeditionState, "scrap");
        int transfersBeforeCancel = gameRoot.Session.InventoryTransfers.Count;
        int logisticsBeforeCancel = expeditionState.LogisticsOrderIds.Count;
        SelectStructure(cancelSiteId, string.Empty);
        if (!CancelSelectedConstructionSite() ||
            !gameRoot.Session.ConstructionSites.TryGetValue(cancelSiteId, out ConstructionSiteState? cancelledSite) ||
            cancelledSite.State != "cancelled" ||
            !_selectionLabel!.Text.Contains("未选择单位") ||
            CountExpeditionStack(gameRoot.Session, expeditionState, "metal") != metalBeforeCancel ||
            CountExpeditionStack(gameRoot.Session, expeditionState, "scrap") != scrapBeforeCancel ||
            gameRoot.Session.InventoryTransfers.Count <= transfersBeforeCancel ||
            expeditionState.LogisticsOrderIds.Count <= logisticsBeforeCancel ||
            !HasLogisticsOrderCreatedBy(gameRoot.Session, expeditionState, "construction_cancel_return"))
        {
            GD.PushError("[调试] 经济自检失败：取消施工未返还材料或未清理选择");
            QuitSelfTestFailure();
            return;
        }

        if (!BuildForSelfTest(expeditionState, constructionSystem, "assembler_basic", expeditionState.DropPosition + new Vector2I(90, 210), gatherUnit.UnitInstanceId, out BuildingInstance assembler, out string assemblerMessage))
        {
            GD.PushError($"[调试] 经济自检失败：{assemblerMessage}");
            QuitSelfTestFailure();
            return;
        }

        PowerNetworkState network = constructionSystem.RecalculatePowerNetwork(expeditionState);
        if (network.State != "online" || solarPanel.PowerState != "online" || assembler.PowerState != "online")
        {
            GD.PushError("[调试] 经济自检失败：电力网络未在线");
            QuitSelfTestFailure();
            return;
        }

        if (!constructionSystem.TryDeliverRecipeInputs(expeditionState, assembler.BuildingInstanceId, "recycle_scrap_to_metal", out string inputMessage))
        {
            GD.PushError($"[调试] 经济自检失败：{inputMessage}");
            QuitSelfTestFailure();
            return;
        }

        if (!constructionSystem.TryRunRecipe(expeditionState, assembler.BuildingInstanceId, "recycle_scrap_to_metal", out ProductionJobState productionJob, out string productionMessage))
        {
            GD.PushError($"[调试] 经济自检失败：{productionMessage}");
            QuitSelfTestFailure();
            return;
        }

        InventoryContainer? outputInventory = gameRoot.Session.Inventories.GetValueOrDefault(assembler.OutputInventoryId);
        if (productionJob.State != "completed" ||
            outputInventory is null ||
            outputInventory.GetItemCount("metal") < 3 ||
            gameRoot.Session.InventoryTransfers.Count <= transfersBefore ||
            expeditionState.LogisticsOrderIds.Count <= logisticsBefore)
        {
            GD.PushError("[调试] 经济自检失败：生产输出、库存转移或物流订单未写回");
            QuitSelfTestFailure();
            return;
        }

        int repairRecordsBefore = expeditionState.RepairRecords.Count;
        assembler.Durability = System.Math.Max(0, assembler.MaxDurability - 60);
        ApplySelection(new[] { gatherUnit.UnitInstanceId });
        RepairBuilding(assembler.BuildingInstanceId);
        RepairRecord? repairRecord = expeditionState.RepairRecords.LastOrDefault(record => record.TargetId == assembler.BuildingInstanceId);
        if (repairRecord is null ||
            repairRecord.Result != "completed" ||
            assembler.Durability != assembler.MaxDurability ||
            repairRecord.ConsumedTransferIds.Count == 0 ||
            expeditionState.RepairRecords.Count <= repairRecordsBefore ||
            expeditionState.SurfaceCommandRecords.Count < commandRecordsBefore + 3)
        {
            GD.PushError("[调试] 经济自检失败：目标维修指令或指令记录未写回");
            QuitSelfTestFailure();
            return;
        }

        SelectStructure(string.Empty, assembler.BuildingInstanceId);
        if (_selectionLabel is null ||
            !_selectionLabel.Text.Contains("电力") ||
            !_selectionLabel.Text.Contains("输入") ||
            !_selectionLabel.Text.Contains("输出"))
        {
            GD.PushError("[调试] 经济自检失败：建筑详情未刷新");
            QuitSelfTestFailure();
            return;
        }

        GD.Print($"[调试] 经济自检完成：建筑 {expeditionState.BuildingInstanceIds.Count}，物流 {expeditionState.LogisticsOrderIds.Count}，生产 {productionJob.RecipeId}，维修 {repairRecord.RepairRecordId}");
    }

    private bool TryGatherSpecificMineralForSelfTest(
        ExpeditionState expeditionState,
        SurfaceMiningSystem miningSystem,
        string unitInstanceId,
        string mineralDepositId,
        out string message)
    {
        MineralDepositInstance? mineral = expeditionState.MineralDepositStates.Values
            .FirstOrDefault(instance => instance.MineralDepositId == mineralDepositId && !instance.IsDepleted);
        if (mineral is null)
        {
            message = $"缺少自检矿产点：{mineralDepositId}";
            return false;
        }

        return miningSystem.TryGather(expeditionState, new[] { unitInstanceId }, mineral.MineralDepositInstanceId, out GatherRecord _, out message);
    }

    private static BuildingInstance? FindCompletedBuildingForSelfTest(
        GameSession session,
        ExpeditionState expeditionState,
        string buildingId,
        Vector2I position)
    {
        return expeditionState.BuildingInstanceIds
            .Select(instanceId => session.BuildingInstances.TryGetValue(instanceId, out BuildingInstance? building) ? building : null)
            .LastOrDefault(building => building is not null &&
                building.BuildingId == buildingId &&
                building.Position == position);
    }

    private static int CountExpeditionStack(GameSession session, ExpeditionState expeditionState, string itemId)
    {
        int count = 0;
        foreach (string inventoryId in expeditionState.LocationInventoryIds.Distinct())
        {
            if (session.Inventories.TryGetValue(inventoryId, out InventoryContainer? inventory))
            {
                count += inventory.GetItemCount(itemId);
            }
        }

        foreach (string groundItemId in expeditionState.GroundItemStateIds)
        {
            if (session.GroundItems.TryGetValue(groundItemId, out GroundItemState? groundItem) &&
                groundItem.Stack.ItemId == itemId)
            {
                count += groundItem.Stack.Count;
            }
        }

        return count;
    }

    private static bool HasLogisticsOrderCreatedBy(GameSession session, ExpeditionState expeditionState, string createdBy)
    {
        return expeditionState.LogisticsOrderIds.Any(orderId =>
            session.LogisticsOrders.TryGetValue(orderId, out LogisticsOrderState? order) &&
            order.CreatedBy == createdBy);
    }

    private static bool BuildForSelfTest(
        ExpeditionState expeditionState,
        SurfaceConstructionSystem constructionSystem,
        string buildingId,
        Vector2I position,
        string unitInstanceId,
        out BuildingInstance buildingInstance,
        out string message)
    {
        buildingInstance = new BuildingInstance();
        if (!constructionSystem.TryCreateConstructionSite(expeditionState, buildingId, position, unitInstanceId, out ConstructionSiteState site, out message))
        {
            return false;
        }

        if (!constructionSystem.TryDeliverConstructionMaterials(expeditionState, site.ConstructionSiteId, out message))
        {
            return false;
        }

        return constructionSystem.TryCompleteConstruction(expeditionState, site.ConstructionSiteId, out buildingInstance, out message);
    }

    private void RefreshCommandButtons()
    {
        HashSet<string> availableCommands = AvailableCommandsForSelection();
        foreach (KeyValuePair<string, Button> pair in _commandButtons)
        {
            pair.Value.Disabled = _selectionState.SelectedUnitInstanceIds.Count == 0 || !availableCommands.Contains(pair.Key);
        }
    }

    private HashSet<string> AvailableCommandsForSelection()
    {
        HashSet<string> availableCommands = new();
        bool initialized = false;
        foreach (string unitInstanceId in _selectionState.SelectedUnitInstanceIds)
        {
            if (!_surfaceUnits.TryGetValue(unitInstanceId, out SurfaceUnit? surfaceUnit) ||
                surfaceUnit.UnitData is null)
            {
                continue;
            }

            if (!initialized)
            {
                availableCommands.UnionWith(surfaceUnit.UnitData.AvailableCommands);
                initialized = true;
            }
            else
            {
                availableCommands.IntersectWith(surfaceUnit.UnitData.AvailableCommands);
            }
        }

        return availableCommands;
    }

    private void HandleCameraMotion(double delta)
    {
        if (_camera is null)
        {
            return;
        }

        Vector2 direction = Vector2.Zero;
        if (Input.IsActionPressed("camera_move_up"))
        {
            direction.Y -= 1f;
        }

        if (Input.IsActionPressed("camera_move_down"))
        {
            direction.Y += 1f;
        }

        if (Input.IsActionPressed("camera_move_left"))
        {
            direction.X -= 1f;
        }

        if (Input.IsActionPressed("camera_move_right"))
        {
            direction.X += 1f;
        }

        if (direction != Vector2.Zero)
        {
            _camera.Position += direction.Normalized() * CameraSpeed * (float)delta / _camera.Zoom.X;
        }
    }

    private void AdjustZoom(float delta)
    {
        if (_camera is null)
        {
            return;
        }

        float nextZoom = Mathf.Clamp(_camera.Zoom.X + delta, 0.55f, 1.7f);
        _camera.Zoom = new Vector2(nextZoom, nextZoom);
    }

    private void UpdateDragFrame(Vector2 currentViewport)
    {
        if (_dragSelectFrame is null)
        {
            return;
        }

        Rect2 viewportRect = NormalizeRect(_dragStartViewport, currentViewport);
        _dragSelectFrame.Position = viewportRect.Position;
        _dragSelectFrame.Size = viewportRect.Size;
        _dragSelectFrame.Visible = viewportRect.Size.Length() > DragThreshold;
    }

    private void SetMessage(string message)
    {
        if (_messageLabel is not null)
        {
            _messageLabel.Text = message;
        }
    }

    private void ShowCommandFailure(string message)
    {
        if (_commandFailedIcon is not null)
        {
            _commandFailedIcon.Visible = true;
        }

        Texture2D? invalidCursor = UiAssets.LoadTexture("res://assets/ui/surface/cursors/cursor_invalid.png");
        if (invalidCursor is not null)
        {
            Input.SetCustomMouseCursor(invalidCursor, Input.CursorShape.Arrow);
        }

        SetMessage(message);
    }

    private void PlaySurfaceAudio(string soundId, string category = "ui")
    {
        if (_audioPlayer is null || DisplayServer.GetName() == "headless")
        {
            return;
        }

        string path = $"res://assets/audio/surface/{category}/{soundId}.wav";
        AudioStream? stream = null;
        if (ResourceLoader.Exists(path, nameof(AudioStream)))
        {
            stream = ResourceLoader.Load<AudioStream>(path);
        }

        stream ??= FileAccess.FileExists(path) ? AudioStreamWav.LoadFromFile(path) : null;
        if (stream is null)
        {
            return;
        }

        _audioPlayer.Stream = stream;
        _audioPlayer.Play();
    }

    private ScenePayload CreateReturnPayload(GameRoot gameRoot)
    {
        ScenePayload returnPayload = new()
        {
            FromScene = SceneId.SurfaceExpedition,
            TargetScene = SceneId.ReturnSummary,
            PayloadType = "surface_return_summary",
            DebugEnabled = _payload?.DebugEnabled ?? false,
            Seed = _payload?.Seed ?? gameRoot.Session.ActiveExpedition?.Seed ?? 0
        };

        ExpeditionState? expeditionState = gameRoot.Session.ActiveExpedition;
        ReturnSummaryPayloadData summaryData = new();
        if (expeditionState is not null)
        {
            summaryData.ExpeditionId = expeditionState.ExpeditionId;
            summaryData.BroughtItems.AddRange(expeditionState.InitialItems);
            summaryData.ReturnCargo.AddRange(expeditionState.RocketState.CargoItems);
            summaryData.ReturnedItemInstanceIds.AddRange(expeditionState.RocketState.ReturningItemInstanceIds);
            summaryData.ReturnedAwakenedUnitIds.AddRange(expeditionState.RocketState.ReturningAwakenedUnitIds);
            summaryData.ReturnedChipIds.AddRange(expeditionState.RocketState.ReturningChipIds);
            summaryData.ReturnedBlueprintIds.AddRange(expeditionState.RocketState.ReturningBlueprintIds);
            summaryData.LeftSurfaceAssetIds.AddRange(expeditionState.MapState.LeftAssetIds);
            summaryData.DiscoveredIds.AddRange(expeditionState.RocketState.ReturningBlueprintIds);
            summaryData.DiscoveredIds.AddRange(expeditionState.DiscoveredIds);
            foreach (InventoryTransfer transfer in gameRoot.Session.InventoryTransfers)
            {
                if (transfer.ExpeditionId == expeditionState.ExpeditionId && transfer.Reason == "rocket_cargo_load")
                {
                    summaryData.RelatedTransferIds.Add(transfer.TransferId);
                }
            }
        }

        returnPayload.ReturnSummaryData = summaryData;
        return returnPayload;
    }

    private void RegisterUiInputBlocker(Control control)
    {
        control.MouseFilter = Control.MouseFilterEnum.Stop;
        control.MouseEntered += () => FindGameRoot()?.InputIntentController.SetUiBlocked(true);
        control.MouseExited += () => FindGameRoot()?.InputIntentController.SetUiBlocked(false);
        control.GuiInput += inputEvent =>
        {
            if (inputEvent is InputEventMouseButton)
            {
                GetViewport().SetInputAsHandled();
            }
        };
    }

    private static Button AddCommandButton(GridContainer commandPanel, string text, string iconPath, System.Action pressed)
    {
        Button button = new()
        {
            Text = text,
            Icon = UiAssets.LoadTexture(iconPath),
            CustomMinimumSize = new Vector2(88, 56),
            ExpandIcon = true
        };
        button.Pressed += pressed;
        commandPanel.AddChild(button);
        return button;
    }

    private static void RegisterCommandCursor(Button button, string commandId)
    {
        button.MouseEntered += () =>
        {
            Texture2D? cursorTexture = UiAssets.LoadTexture(CursorPathForCommand(commandId));
            if (cursorTexture is not null)
            {
                Input.SetCustomMouseCursor(cursorTexture, Input.CursorShape.Arrow);
            }
        };
        button.MouseExited += () => Input.SetCustomMouseCursor(null, Input.CursorShape.Arrow);
    }

    private static string CursorPathForCommand(string commandId)
    {
        return commandId switch
        {
            "move" or "stop" => "res://assets/ui/surface/cursors/cursor_move.png",
            "attack" or "guard" => "res://assets/ui/surface/cursors/cursor_attack.png",
            "repair" or "return_to_repair" => "res://assets/ui/surface/cursors/cursor_repair.png",
            "scan" or "hack" or "scout" => "res://assets/ui/surface/cursors/cursor_scan.png",
            _ => "res://assets/ui/surface/cursors/cursor_invalid.png"
        };
    }

    private static Texture2D? LoadOptionalTexture(string path)
    {
        if (!ResourceLoader.Exists(path, nameof(Texture2D)) && !FileAccess.FileExists(path))
        {
            return null;
        }

        return UiAssets.LoadTexture(path);
    }

    private static Rect2 NormalizeRect(Vector2 first, Vector2 second)
    {
        Vector2 position = new(Mathf.Min(first.X, second.X), Mathf.Min(first.Y, second.Y));
        Vector2 end = new(Mathf.Max(first.X, second.X), Mathf.Max(first.Y, second.Y));
        return new Rect2(position, end - position);
    }

    private static int GroupIndexFromKey(Key key)
    {
        return key switch
        {
            Key.Key1 => 1,
            Key.Key2 => 2,
            Key.Key3 => 3,
            Key.Key4 => 4,
            Key.Key5 => 5,
            Key.Key6 => 6,
            Key.Key7 => 7,
            Key.Key8 => 8,
            Key.Key9 => 9,
            _ => 0
        };
    }

    private void QuitSelfTestFailure()
    {
        if (DisplayServer.GetName() == "headless")
        {
            GetTree().Quit(1);
        }
    }

    private static bool HasArgument(string key)
    {
        foreach (string argument in OS.GetCmdlineArgs())
        {
            if (argument == key || argument.StartsWith($"{key}=", System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
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
