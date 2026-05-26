using System.Collections.Generic;
using Godot;

namespace GodotGame;

public partial class EventData : Resource
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> TriggerTags { get; } = new();
    public int Weight { get; set; } = 1;
    public int RiskLevel { get; set; }
    public List<string> RewardTables { get; } = new();
    public string IconPath { get; set; } = string.Empty;
    public List<string> StoryFlagsRequired { get; } = new();
    public List<string> StoryFlagsSet { get; } = new();
}
