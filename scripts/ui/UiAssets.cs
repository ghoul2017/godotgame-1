using Godot;

namespace GodotGame;

public static class UiAssets
{
    public const string OrbitBackground = "res://assets/ui/orbit/backgrounds/orbit_station_command_deck.svg";
    public const string SurfaceBackground = "res://assets/ui/backgrounds/surface_expedition_background.svg";
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

    public const string OrbitCategoryAll = "res://assets/ui/orbit/categories/category_all.svg";
    public const string OrbitCategoryMineral = "res://assets/ui/orbit/categories/category_mineral.svg";
    public const string OrbitCategoryMaterial = "res://assets/ui/orbit/categories/category_material.svg";
    public const string OrbitCategoryEquipment = "res://assets/ui/orbit/categories/category_equipment.svg";
    public const string OrbitCategoryChip = "res://assets/ui/orbit/categories/category_chip.svg";
    public const string OrbitCategoryUnitPlatform = "res://assets/ui/orbit/categories/category_unit_platform.svg";
    public const string OrbitCategoryBlueprint = "res://assets/ui/orbit/categories/category_blueprint.svg";
    public const string OrbitCategoryKeyItem = "res://assets/ui/orbit/categories/category_key_item.svg";
    public const string OrbitIconCredits = "res://assets/ui/orbit/status/credits.svg";
    public const string OrbitIconAvailable = "res://assets/ui/orbit/status/available.svg";
    public const string OrbitIconCompleted = "res://assets/ui/orbit/status/completed.svg";
    public const string OrbitIconInsufficient = "res://assets/ui/orbit/status/insufficient.svg";
    public const string OrbitIconLocked = "res://assets/ui/orbit/status/locked.svg";
    public const string OrbitAudioTabSwitch = "res://assets/audio/ui/orbit/tab_switch.wav";
    public const string OrbitAudioSelect = "res://assets/audio/ui/orbit/list_select.wav";
    public const string OrbitAudioSuccess = "res://assets/audio/ui/orbit/confirm_success.wav";
    public const string OrbitAudioFailure = "res://assets/audio/ui/orbit/confirm_failure.wav";
    public const string OrbitAudioDialogOpen = "res://assets/audio/ui/orbit/dialog_open.wav";
    public const string OrbitAudioDialogClose = "res://assets/audio/ui/orbit/dialog_close.wav";

    public const string ButtonPrimary = "res://assets/ui/orbit/buttons/button_normal.svg";
    public const string ButtonHover = "res://assets/ui/orbit/buttons/button_hover.svg";
    public const string ButtonPressed = "res://assets/ui/orbit/buttons/button_pressed.svg";
    public const string ButtonDisabled = "res://assets/ui/orbit/buttons/button_disabled.svg";
    public const string PanelFrame = "res://assets/ui/orbit/panels/orbit_panel_frame.svg";
    public const string OrbitListRow = "res://assets/ui/orbit/panels/orbit_list_row.svg";

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
        StyleBoxTexture? buttonHover = CreateTextureStyle(ButtonHover, new Rect2(8, 8, 304, 56), new SideMargins(10, 10, 10, 10));
        StyleBoxTexture? buttonPressed = CreateTextureStyle(ButtonPressed, new Rect2(8, 8, 304, 56), new SideMargins(10, 10, 10, 10));
        StyleBoxTexture? buttonDisabled = CreateTextureStyle(ButtonDisabled, new Rect2(8, 8, 304, 56), new SideMargins(10, 10, 10, 10));
        StyleBoxTexture? panelStyle = CreateTextureStyle(PanelFrame, new Rect2(16, 16, 448, 288), new SideMargins(14, 14, 14, 14));
        if (buttonNormal is not null)
        {
            theme.SetStylebox("normal", "Button", buttonNormal);
            theme.SetStylebox("hover", "Button", buttonHover ?? buttonNormal);
            theme.SetStylebox("pressed", "Button", buttonPressed ?? buttonNormal);
            theme.SetStylebox("disabled", "Button", buttonDisabled ?? buttonNormal);
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

    public static StyleBoxTexture? CreateTextureStyleBox(string path, Rect2 region, float margin)
    {
        return CreateTextureStyle(path, region, new SideMargins(margin, margin, margin, margin));
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
