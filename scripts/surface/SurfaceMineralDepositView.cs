using Godot;

namespace GodotGame;

public partial class SurfaceMineralDepositView : Node2D
{
    private const float SpriteDisplaySize = 92f;
    private const float HitRadius = 52f;

    private MineralDepositInstance? _instance;
    private MineralDepositData? _data;
    private Sprite2D? _sprite;
    private Label? _label;

    public string MineralDepositInstanceId => _instance?.MineralDepositInstanceId ?? string.Empty;
    public bool IsDepleted => _instance?.IsDepleted ?? true;

    public void Configure(MineralDepositInstance instance, MineralDepositData data)
    {
        _instance = instance;
        _data = data;
        Name = $"SurfaceMineral_{instance.MineralDepositInstanceId}";
        Position = new Vector2(instance.Position.X, instance.Position.Y);

        _sprite = new Sprite2D
        {
            Name = "Sprite",
            ZIndex = -4
        };
        AddChild(_sprite);

        _label = new Label
        {
            Name = "Label",
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(-86f, 48f),
            CustomMinimumSize = new Vector2(172f, 24f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(_label);

        Refresh();
    }

    public bool ContainsWorldPosition(Vector2 worldPosition)
    {
        return Position.DistanceTo(worldPosition) <= HitRadius;
    }

    public void Refresh()
    {
        if (_instance is null || _data is null)
        {
            return;
        }

        string spritePath = _instance.IsDepleted ? _data.DepletedSpritePath : _data.SpritePath;
        Texture2D? texture = UiAssets.LoadTexture(spritePath);
        if (_sprite is not null)
        {
            _sprite.Texture = texture;
            if (texture is not null)
            {
                Vector2 textureSize = texture.GetSize();
                float largestSide = Mathf.Max(textureSize.X, textureSize.Y);
                _sprite.Scale = largestSide > 0f ? Vector2.One * (SpriteDisplaySize / largestSide) : Vector2.One;
            }
        }

        if (_label is not null)
        {
            string stateText = _instance.IsDepleted ? "耗尽" : $"{_instance.RemainingYield}";
            _label.Text = $"{_data.DisplayName} {stateText}";
        }
    }
}
