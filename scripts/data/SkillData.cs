using System.Collections.Generic;
using Godot;

namespace GodotGame;

public partial class SkillData : Resource
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxLevel { get; set; } = 20;
    public List<int> ExperienceThresholds { get; } = new();
    public List<string> EffectTags { get; } = new();
    public string IconPath { get; set; } = string.Empty;
}
