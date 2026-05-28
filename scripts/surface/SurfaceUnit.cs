using Godot;

namespace GodotGame;

public partial class SurfaceUnit : Node2D
{
    private const float SpriteDisplaySize = 74f;
    private const float StopDistance = 3f;

    private UnitData? _unitData;
    private UnitInstance? _unitInstance;
    private SurfaceUnitRuntimeState? _runtimeState;
    private Sprite2D? _sprite;
    private Sprite2D? _selectionRing;
    private Label? _nameLabel;
    private Vector2 _moveTarget;
    private bool _hasMoveTarget;

    public string UnitInstanceId => _unitInstance?.UnitInstanceId ?? string.Empty;
    public UnitData? UnitData => _unitData;
    public UnitInstance? UnitInstance => _unitInstance;
    public SurfaceUnitRuntimeState? RuntimeState => _runtimeState;

    public void Configure(UnitInstance unitInstance, UnitData unitData, SurfaceUnitRuntimeState runtimeState)
    {
        _unitInstance = unitInstance;
        _unitData = unitData;
        _runtimeState = runtimeState;
        Name = $"SurfaceUnit_{unitInstance.UnitInstanceId}";
        Position = runtimeState.Position;

        Texture2D? unitTexture = UiAssets.LoadTexture(unitData.SpritePath);
        _sprite = new Sprite2D
        {
            Name = "Sprite",
            Texture = unitTexture
        };
        if (unitTexture is not null)
        {
            Vector2 textureSize = unitTexture.GetSize();
            float largestSide = Mathf.Max(textureSize.X, textureSize.Y);
            if (largestSide > 0f)
            {
                _sprite.Scale = Vector2.One * (SpriteDisplaySize / largestSide);
            }
        }

        AddChild(_sprite);

        string selectionPath = unitInstance.IsAwakened
            ? "res://assets/ui/surface/selection/selection_ring_awakened.png"
            : "res://assets/ui/surface/selection/selection_ring_mass.png";
        _selectionRing = new Sprite2D
        {
            Name = "SelectionRing",
            Texture = UiAssets.LoadTexture(selectionPath),
            Visible = false,
            ZIndex = -1
        };
        float ringScale = unitData.SelectionRadius / 52f;
        _selectionRing.Scale = Vector2.One * Mathf.Max(0.35f, ringScale);
        AddChild(_selectionRing);

        _nameLabel = new Label
        {
            Name = "NameLabel",
            Text = DisplayName(),
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(-70f, unitData.SelectionRadius + 16f),
            CustomMinimumSize = new Vector2(140f, 22f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(_nameLabel);
        SetSelected(runtimeState.IsSelected);
    }

    public override void _Process(double delta)
    {
        if (!_hasMoveTarget || _unitData is null || _runtimeState is null || _unitInstance is null)
        {
            return;
        }

        Vector2 offset = _moveTarget - Position;
        float distance = offset.Length();
        if (distance <= StopDistance)
        {
            Position = _moveTarget;
            _hasMoveTarget = false;
            _runtimeState.MovementState = "idle";
            _runtimeState.Position = Position;
            _runtimeState.LastReachablePosition = Position;
            _unitInstance.CurrentCommand = "idle";
            return;
        }

        Vector2 direction = offset / distance;
        float step = _unitData.MoveSpeed * (float)delta;
        Position += direction * Mathf.Min(step, distance);
        _runtimeState.Position = Position;
        _runtimeState.CurrentTargetPosition = _moveTarget;
        _runtimeState.FacingAngle = direction.Angle();
        _runtimeState.MovementState = "moving";
        _unitInstance.CurrentCommand = $"move:{_runtimeState.CurrentCommandId}";
    }

    public bool ContainsWorldPosition(Vector2 worldPosition)
    {
        if (_unitData is null)
        {
            return false;
        }

        return Position.DistanceTo(worldPosition) <= _unitData.SelectionRadius;
    }

    public void SetSelected(bool selected)
    {
        if (_runtimeState is not null)
        {
            _runtimeState.IsSelected = selected;
        }

        if (_selectionRing is not null)
        {
            _selectionRing.Visible = selected;
        }
    }

    public void IssueMove(Vector2 targetPosition, string commandId)
    {
        if (_runtimeState is null || _unitInstance is null)
        {
            return;
        }

        _moveTarget = targetPosition;
        _hasMoveTarget = true;
        _runtimeState.CurrentCommandId = commandId;
        _runtimeState.CurrentTargetPosition = targetPosition;
        _runtimeState.LastReachablePosition = targetPosition;
        _runtimeState.MovementState = "moving";
        _runtimeState.LastErrorCode = string.Empty;
        _unitInstance.CurrentCommand = $"move:{commandId}";
    }

    public string DisplayName()
    {
        if (_unitInstance is null || _unitData is null)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(_unitInstance.DisplayNameOverride)
            ? _unitData.DisplayName
            : _unitInstance.DisplayNameOverride;
    }
}
