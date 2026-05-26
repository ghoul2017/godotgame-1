using Godot;

namespace GodotGame;

public sealed class BuildingInstance
{
    public string BuildingInstanceId { get; set; } = string.Empty;
    public string BuildingId { get; set; } = string.Empty;
    public Vector2I Position { get; set; }
    public int Rotation { get; set; }
    public int Durability { get; set; }
    public float ConstructionProgress { get; set; }
    public string PowerState { get; set; } = "offline";
    public string InventoryId { get; set; } = string.Empty;
    public string ActiveRecipeId { get; set; } = string.Empty;
    public float ProductionProgress { get; set; }
}
