using System.Collections.Generic;

namespace GodotGame;

public sealed class UnitInstance
{
    public string UnitInstanceId { get; set; } = string.Empty;
    public string UnitId { get; set; } = string.Empty;
    public string DisplayNameOverride { get; set; } = string.Empty;
    public bool IsAwakened { get; set; }
    public int Durability { get; set; }
    public int Energy { get; set; }
    public string InventoryId { get; set; } = string.Empty;
    public List<string> EquipmentInstanceIds { get; } = new();
    public List<string> ModPartInstanceIds { get; } = new();
    public Dictionary<string, int> SkillExperience { get; } = new();
    public Dictionary<string, int> SkillLevels { get; } = new();
    public string BehaviorMode { get; set; } = "balanced";
    public string CurrentCommand { get; set; } = string.Empty;
    public List<string> StoryFlags { get; } = new();
}
