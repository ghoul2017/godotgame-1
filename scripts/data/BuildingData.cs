using System.Collections.Generic;
using Godot;

namespace GodotGame;

public partial class BuildingData : Resource
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Vector2I Footprint { get; set; } = Vector2I.One;
    public List<ItemStack> BuildCost { get; } = new();
    public float BuildTime { get; set; }
    public int PowerGeneration { get; set; }
    public int PowerConsumption { get; set; }
    public int StorageCapacity { get; set; }
    public List<string> RecipeIds { get; } = new();
    public List<string> FunctionTags { get; } = new();
    public string IconPath { get; set; } = string.Empty;
    public string SpritePath { get; set; } = string.Empty;
    public string PreviewSpritePath { get; set; } = string.Empty;
    public string ConstructionSpritePath { get; set; } = string.Empty;
    public string DamagedSpritePath { get; set; } = string.Empty;
    public string RequiresBlueprintId { get; set; } = string.Empty;
}
