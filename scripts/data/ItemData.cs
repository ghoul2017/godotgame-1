using System.Collections.Generic;
using Godot;

namespace GodotGame;

public partial class ItemData : Resource
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SubCategory { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public string WorldSpritePath { get; set; } = string.Empty;
    public int MaxStack { get; set; } = 100;
    public float UnitWeight { get; set; } = 1f;
    public int BaseValue { get; set; }
    public List<string> Tags { get; } = new();
    public bool IsUnique { get; set; }
    public bool IsQuestItem { get; set; }
    public bool CanDiscard { get; set; } = true;

    public bool CanStack => !IsUnique && MaxStack > 1;
    public bool RequiresInstance => IsUnique || MaxStack <= 1 || Tags.Contains("instance_item");
}
