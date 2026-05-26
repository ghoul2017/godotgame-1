using Godot;

namespace GodotGame;

public static class UiAssets
{
    public const string OrbitBackground = "res://assets/ui/backgrounds/orbit_station_background.svg";
    public const string SurfaceBackground = "res://assets/ui/backgrounds/surface_expedition_background.svg";
    public const string PrologueBackground = "res://assets/ui/backgrounds/prologue_background.svg";
    public const string SummaryBackground = "res://assets/ui/backgrounds/return_summary_background.svg";

    public const string IconInventory = "res://assets/ui/icons/tab_inventory.svg";
    public const string IconTrade = "res://assets/ui/icons/tab_trade.svg";
    public const string IconResearch = "res://assets/ui/icons/tab_research.svg";
    public const string IconCharacters = "res://assets/ui/icons/tab_characters.svg";
    public const string IconDrop = "res://assets/ui/icons/tab_drop.svg";
    public const string IconCargo = "res://assets/ui/icons/summary_cargo.svg";
    public const string IconLoss = "res://assets/ui/icons/summary_loss.svg";
    public const string IconDiscovery = "res://assets/ui/icons/summary_discovery.svg";
    public const string IconCommand = "res://assets/ui/icons/surface_command.svg";
    public const string IconMinimap = "res://assets/ui/icons/surface_minimap.svg";

    public const string ButtonPrimary = "res://assets/ui/buttons/button_primary.svg";
    public const string PanelFrame = "res://assets/ui/panels/panel_frame.svg";

    public static Texture2D? LoadTexture(string path)
    {
        if (!path.EndsWith(".svg", System.StringComparison.OrdinalIgnoreCase))
        {
            Texture2D? texture = ResourceLoader.Load<Texture2D>(path);
            if (texture is not null)
            {
                return texture;
            }
        }

        if (path.EndsWith(".svg", System.StringComparison.OrdinalIgnoreCase))
        {
            string svgText = FileAccess.GetFileAsString(path);
            if (!string.IsNullOrEmpty(svgText))
            {
                Image image = new();
                Error error = image.LoadSvgFromString(svgText, 1.0f);
                if (error == Error.Ok)
                {
                    return ImageTexture.CreateFromImage(image);
                }

                GD.PushWarning($"[UI] SVG 资源解析失败：{path}，错误：{error}");
            }
        }

        GD.PushWarning($"[UI] 资源加载失败：{path}");
        return null;
    }

    public static TextureRect CreateTextureRect(string name, string path, TextureRect.ExpandModeEnum expandMode = TextureRect.ExpandModeEnum.FitWidthProportional)
    {
        TextureRect textureRect = new()
        {
            Name = name,
            Texture = LoadTexture(path),
            ExpandMode = expandMode,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        return textureRect;
    }

    public static Label CreateSectionLabel(string text)
    {
        return new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    public static Theme CreateBaseTheme()
    {
        Theme theme = new();
        theme.DefaultFontSize = 18;

        StyleBoxTexture? buttonNormal = CreateTextureStyle(ButtonPrimary, new Rect2(8, 8, 304, 56), new SideMargins(10, 10, 10, 10));
        StyleBoxTexture? panelStyle = CreateTextureStyle(PanelFrame, new Rect2(16, 16, 448, 288), new SideMargins(14, 14, 14, 14));
        if (buttonNormal is not null)
        {
            theme.SetStylebox("normal", "Button", buttonNormal);
            theme.SetStylebox("hover", "Button", buttonNormal);
            theme.SetStylebox("pressed", "Button", buttonNormal);
            theme.SetStylebox("disabled", "Button", buttonNormal);
        }

        if (panelStyle is not null)
        {
            theme.SetStylebox("panel", "PanelContainer", panelStyle);
        }

        theme.SetColor("font_color", "Button", new Color(0.86f, 0.9f, 0.84f));
        theme.SetColor("font_hover_color", "Button", new Color(1f, 0.9f, 0.55f));
        theme.SetColor("font_pressed_color", "Button", new Color(0.65f, 0.82f, 0.78f));
        theme.SetColor("font_disabled_color", "Button", new Color(0.45f, 0.5f, 0.48f));
        return theme;
    }

    private static StyleBoxTexture? CreateTextureStyle(string path, Rect2 region, SideMargins margins)
    {
        Texture2D? texture = LoadTexture(path);
        if (texture is null)
        {
            return null;
        }

        StyleBoxTexture styleBox = new()
        {
            Texture = texture,
            RegionRect = region,
            TextureMarginLeft = margins.Left,
            TextureMarginTop = margins.Top,
            TextureMarginRight = margins.Right,
            TextureMarginBottom = margins.Bottom
        };
        return styleBox;
    }

    private readonly record struct SideMargins(float Left, float Top, float Right, float Bottom);
}
