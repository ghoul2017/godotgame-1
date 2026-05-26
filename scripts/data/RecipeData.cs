using System.Collections.Generic;
using Godot;

namespace GodotGame;

public partial class RecipeData : Resource
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<ItemStack> InputItems { get; } = new();
    public List<ItemStack> OutputItems { get; } = new();
    public float WorkTime { get; set; }
    public int PowerCost { get; set; }
    public List<string> RequiredBuildingTags { get; } = new();
    public string RequiredBlueprintId { get; set; } = string.Empty;
    public string OperatorSkillId { get; set; } = string.Empty;
}
