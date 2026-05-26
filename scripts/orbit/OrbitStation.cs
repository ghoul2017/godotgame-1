using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGame;

public partial class OrbitStation : Control, ScenePayloadReceiver
{
    [Signal]
    public delegate void PageChangedEventHandler(string pageId);

    private readonly Dictionary<string, Button> _tabButtons = new();
    private GameRoot? _gameRoot;
    private OrbitTransactionService? _transactionService;
    private ScenePayload? _payload;
    private string _currentPage = OrbitPageId.Inventory;
    private string _selectedId = string.Empty;
    private string _pendingActionId = string.Empty;
    private string _inventoryFilter = "all";

    private Label? _creditsLabel;
    private Label? _statusLabel;
    private Label? _pageTitle;
    private Label? _payloadLabel;
    private Label? _feedbackLabel;
    private VBoxContainer? _filterContainer;
    private VBoxContainer? _listContainer;
    private TextureRect? _detailIcon;
    private Label? _detailTitle;
    private Label? _detailBody;
    private Button? _actionButton;
    private Button? _cancelButton;
    private AudioStreamPlayer? _audioPlayer;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        _gameRoot = FindGameRoot();
        if (_gameRoot is not null)
        {
            _gameRoot.InputIntentController.SetUiBlocked(true);
            _transactionService = new OrbitTransactionService(_gameRoot.Session, _gameRoot.DataRegistry);
        }

        BuildUi();
        OpenPage(!string.IsNullOrEmpty(_payload?.NavigationData?.OrbitPageId) ? _payload.NavigationData.OrbitPageId : OrbitPageId.Inventory);
    }

    public override void _ExitTree()
    {
        _gameRoot?.InputIntentController.SetUiBlocked(false);
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent.IsActionPressed("cancel_action"))
        {
            if (!string.IsNullOrEmpty(_pendingActionId))
            {
                _pendingActionId = string.Empty;
                SetFeedback("已取消确认。");
                RefreshCurrentPage();
                PlayAudio(UiAssets.OrbitAudioDialogClose);
                AcceptEvent();
                return;
            }

            if (!string.IsNullOrEmpty(_selectedId))
            {
                _selectedId = string.Empty;
                ShowEmptyDetail("未选择对象", "选择列表条目查看完整消耗、收益、状态和资源引用。");
                AcceptEvent();
            }
        }
        else if (inputEvent.IsActionPressed("confirm_action"))
        {
            if (_actionButton is not null && !_actionButton.Disabled)
            {
                _actionButton.EmitSignal(BaseButton.SignalName.Pressed);
                AcceptEvent();
            }
        }
    }

    public void ReceivePayload(ScenePayload payload)
    {
        _payload = payload;
        if (_payloadLabel is not null)
        {
            _payloadLabel.Text = $"载荷：{payload.PayloadType}  来源：{payload.FromScene}  种子：{payload.Seed}";
        }

        if (!string.IsNullOrEmpty(payload.NavigationData?.OrbitPageId))
        {
            OpenPage(payload.NavigationData.OrbitPageId);
        }
    }

    public void OpenPage(string pageId)
    {
        string nextPage = pageId switch
        {
            OrbitPageId.Inventory => OrbitPageId.Inventory,
            OrbitPageId.Trade => OrbitPageId.Trade,
            OrbitPageId.Research => OrbitPageId.Research,
            OrbitPageId.Characters => OrbitPageId.Characters,
            OrbitPageId.Drop => OrbitPageId.Drop,
            _ => OrbitPageId.Inventory
        };

        string previousPage = _currentPage;
        _currentPage = nextPage;
        _selectedId = string.Empty;
        _pendingActionId = string.Empty;
        UpdateTabs();
        RefreshStatus();
        RefreshCurrentPage();

        if (previousPage != nextPage)
        {
            GD.Print($"[轨道] 页面切换：{previousPage} -> {nextPage}");
            PlayAudio(UiAssets.OrbitAudioTabSwitch);
        }
        else
        {
            GD.Print($"[轨道] 打开页面：{nextPage}");
        }

        EmitSignal(SignalName.PageChanged, nextPage);
    }

    private void BuildUi()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        TextureRect background = UiAssets.CreateTextureRect("OrbitBackground", UiAssets.OrbitBackground);
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(background);

        _audioPlayer = new AudioStreamPlayer
        {
            Name = "OrbitAudioPlayer"
        };
        AddChild(_audioPlayer);

        MarginContainer margin = new()
        {
            Name = "OrbitMargin"
        };
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 18);
        margin.AddThemeConstantOverride("margin_top", 14);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_bottom", 14);
        AddChild(margin);

        VBoxContainer root = new()
        {
            Name = "OrbitLayout"
        };
        margin.AddChild(root);

        root.AddChild(BuildStatusBar());
        root.AddChild(BuildMainArea());
        root.AddChild(BuildBottomBar());
    }

    private Control BuildStatusBar()
    {
        PanelContainer panel = new()
        {
            CustomMinimumSize = new Vector2(0, 74),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        HBoxContainer bar = new()
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        panel.AddChild(bar);

        TextureRect creditIcon = UiAssets.CreateTextureRect("CreditsIcon", UiAssets.OrbitIconCredits);
        creditIcon.CustomMinimumSize = new Vector2(42, 42);
        bar.AddChild(creditIcon);

        _creditsLabel = CreateLabel("信用点：--", 170);
        bar.AddChild(_creditsLabel);

        _statusLabel = new Label
        {
            Text = "轨道状态读取中",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        bar.AddChild(_statusLabel);

        _payloadLabel = CreateLabel("载荷：正式入口", 360);
        bar.AddChild(_payloadLabel);
        return panel;
    }

    private Control BuildMainArea()
    {
        HBoxContainer body = new()
        {
            Name = "OrbitBody",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };

        PanelContainer tabsPanel = new()
        {
            CustomMinimumSize = new Vector2(210, 0)
        };
        VBoxContainer tabs = new()
        {
            Name = "OrbitTabs"
        };
        tabsPanel.AddChild(tabs);
        body.AddChild(tabsPanel);

        AddTabButton(tabs, "库存", OrbitPageId.Inventory, UiAssets.IconInventory);
        AddTabButton(tabs, "交易", OrbitPageId.Trade, UiAssets.IconTrade);
        AddTabButton(tabs, "研发", OrbitPageId.Research, UiAssets.IconResearch);
        AddTabButton(tabs, "角色", OrbitPageId.Characters, UiAssets.IconCharacters);
        AddTabButton(tabs, "空投", OrbitPageId.Drop, UiAssets.IconDrop);

        PanelContainer listPanel = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        VBoxContainer listLayout = new();
        listPanel.AddChild(listLayout);
        body.AddChild(listPanel);

        _pageTitle = new Label
        {
            Text = "轨道站",
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(0, 44)
        };
        listLayout.AddChild(_pageTitle);

        _filterContainer = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(0, 42)
        };
        listLayout.AddChild(_filterContainer);

        ScrollContainer listScroll = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _listContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        listScroll.AddChild(_listContainer);
        listLayout.AddChild(listScroll);

        PanelContainer detailPanel = new()
        {
            CustomMinimumSize = new Vector2(390, 0),
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        VBoxContainer detail = new();
        detailPanel.AddChild(detail);
        body.AddChild(detailPanel);

        _detailIcon = UiAssets.CreateTextureRect("DetailIcon", UiAssets.OrbitIconLocked);
        _detailIcon.CustomMinimumSize = new Vector2(72, 72);
        detail.AddChild(_detailIcon);

        _detailTitle = new Label
        {
            Text = "未选择对象",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        detail.AddChild(_detailTitle);

        _detailBody = new Label
        {
            Text = "选择列表条目查看详情。",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        detail.AddChild(_detailBody);

        _actionButton = new Button
        {
            Text = "无可执行操作",
            Disabled = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        detail.AddChild(_actionButton);

        _cancelButton = new Button
        {
            Text = "取消确认",
            Disabled = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _cancelButton.Pressed += () =>
        {
            _pendingActionId = string.Empty;
            SetFeedback("已取消确认。");
            RefreshCurrentPage();
            PlayAudio(UiAssets.OrbitAudioDialogClose);
        };
        detail.AddChild(_cancelButton);

        return body;
    }

    private Control BuildBottomBar()
    {
        PanelContainer panel = new()
        {
            CustomMinimumSize = new Vector2(0, 58),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        _feedbackLabel = new Label
        {
            Text = "轨道站已连接永久层。交易、研发和空投入口均以 OrbitState 为权威来源。",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.AddChild(_feedbackLabel);
        return panel;
    }

    private void AddTabButton(VBoxContainer tabs, string text, string pageId, string iconPath)
    {
        Button button = new()
        {
            Text = text,
            Icon = UiAssets.LoadTexture(iconPath),
            ToggleMode = true,
            CustomMinimumSize = new Vector2(0, 62),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        button.Pressed += () => OpenPage(pageId);
        tabs.AddChild(button);
        _tabButtons[pageId] = button;
    }

    private void RefreshCurrentPage()
    {
        ClearChildren(_filterContainer);
        ClearChildren(_listContainer);
        ResetAction();

        switch (_currentPage)
        {
            case OrbitPageId.Inventory:
                BuildInventoryPage();
                break;
            case OrbitPageId.Trade:
                BuildTradePage();
                break;
            case OrbitPageId.Research:
                BuildResearchPage();
                break;
            case OrbitPageId.Characters:
                BuildCharactersPage();
                break;
            case OrbitPageId.Drop:
                BuildDropPage();
                break;
        }
    }

    private void RefreshStatus()
    {
        if (_gameRoot is null)
        {
            return;
        }

        OrbitState orbitState = _gameRoot.Session.OrbitState;
        InventoryContainer? inventory = _gameRoot.Session.Inventories.GetValueOrDefault(orbitState.InventoryId);
        float usedWeight = inventory?.GetTotalWeight(_gameRoot.DataRegistry, _gameRoot.Session.ItemInstances) ?? 0f;
        float weightLimit = inventory?.WeightLimit ?? 0f;
        int stackCount = inventory?.ItemStacks.Sum(stack => stack.Count) ?? 0;
        int instanceCount = inventory?.ItemInstanceIds.Count ?? 0;

        if (_creditsLabel is not null)
        {
            _creditsLabel.Text = $"信用点：{orbitState.Credits}";
        }

        if (_statusLabel is not null)
        {
            _statusLabel.Text = $"永久库存 {stackCount + instanceCount} 项 / 重量 {usedWeight:0.0}/{weightLimit:0.0}  蓝图 {orbitState.UnlockedBlueprints.Count}  协议 {orbitState.UnlockedProtocols.Count}  角色 {orbitState.AwakenedUnits.Count}  坐标 {orbitState.KnownCoordinates.Count}  审计 {_gameRoot.Session.OrbitTransactionRecords.Count}";
        }
    }

    private void UpdateTabs()
    {
        foreach (KeyValuePair<string, Button> pair in _tabButtons)
        {
            pair.Value.ButtonPressed = pair.Key == _currentPage;
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
