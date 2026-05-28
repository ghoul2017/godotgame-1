using System.Collections.Generic;

namespace GodotGame;

public sealed class MineralDepositData
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string YieldItemId { get; set; } = string.Empty;
    public int BaseYield { get; set; }
    public int MaxYield { get; set; }
    public float GatherTime { get; set; }
    public int RequiresScanLevel { get; set; }
    public string DepletedBehavior { get; set; } = "show_depleted";
    public string IconPath { get; set; } = string.Empty;
    public string SpritePath { get; set; } = string.Empty;
    public string DepletedSpritePath { get; set; } = string.Empty;
    public List<string> RequiredToolTags { get; } = new();
    public List<string> PreferredUnitTags { get; } = new();
    public List<string> Tags { get; } = new();
}
