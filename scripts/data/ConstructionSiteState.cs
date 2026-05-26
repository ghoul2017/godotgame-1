using System.Collections.Generic;
using Godot;

namespace GodotGame;

public sealed class ConstructionSiteState
{
    public string ConstructionSiteId { get; set; } = string.Empty;
    public string ExpeditionId { get; set; } = string.Empty;
    public string BuildingId { get; set; } = string.Empty;
    public Vector2I Position { get; set; }
    public int Rotation { get; set; }
    public string DeliveredInventoryId { get; set; } = string.Empty;
    public List<ItemStack> RequiredItems { get; } = new();
    public string State { get; set; } = "waiting_materials";
    public float ConstructionProgress { get; set; }
    public string AssignedUnitInstanceId { get; set; } = string.Empty;
}
