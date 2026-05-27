using System.Collections.Generic;
using Godot;

namespace GodotGame;

public sealed class DropPlan
{
    public string DropPlanId { get; set; } = string.Empty;
    public string DropPodId { get; set; } = string.Empty;
    public string TargetCoordinateId { get; set; } = string.Empty;
    public Vector2I TargetCoordinate { get; set; }
    public int Seed { get; set; }
    public List<string> SelectedAwakenedUnitInstanceIds { get; } = new();
    public List<string> SelectedMassUnitInstanceIds { get; } = new();
    public List<SelectedUnitPlatformItem> SelectedUnitPlatformItems { get; } = new();
    public List<ItemStack> SelectedStackItems { get; } = new();
    public List<string> SelectedItemInstanceIds { get; } = new();
    public List<string> RelatedTransferIds { get; } = new();
    public List<string> CreatedUnitInstanceIds { get; } = new();
    public float WeightLimit { get; set; }
    public float UsedWeight { get; set; }
    public int SlotLimit { get; set; }
    public int UsedSlots { get; set; }
    public int UnitCapacity { get; set; }
    public int UsedUnitCapacity { get; set; }
    public string CreatedFromOrbitStateId { get; set; } = string.Empty;
    public double CreatedAt { get; set; }
}
