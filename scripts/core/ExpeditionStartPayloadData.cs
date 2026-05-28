using System.Collections.Generic;
using Godot;

namespace GodotGame;

public sealed class ExpeditionStartPayloadData
{
    public string ExpeditionId { get; set; } = string.Empty;
    public string DropPlanId { get; set; } = string.Empty;
    public string TargetCoordinateId { get; set; } = string.Empty;
    public string DropPodCargoInventoryId { get; set; } = string.Empty;
    public int Seed { get; set; }
    public Vector2I DropPosition { get; set; }
    public List<string> ActiveUnitInstanceIds { get; } = new();
    public List<UnitStack> InitialUnits { get; } = new();
    public List<ItemStack> InitialItems { get; } = new();
}
