using System.Collections.Generic;
using Godot;

namespace GodotGame;

public sealed class KnownCoordinate
{
    public string CoordinateId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string RegionType { get; set; } = string.Empty;
    public int SeedHint { get; set; }
    public int RiskLevel { get; set; }
    public Vector2I DropPosition { get; set; }
    public List<string> MineralTags { get; } = new();
    public List<string> StoryFlagsRequired { get; } = new();
    public bool IsRevisitable { get; set; }
    public string LinkedExpeditionId { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
}
