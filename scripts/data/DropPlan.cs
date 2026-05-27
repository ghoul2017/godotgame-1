using System.Collections.Generic;
using Godot;

namespace GodotGame;

public sealed class DropPlan
{
    public string DropPlanId { get; set; } = string.Empty;
    public string DropPodId { get; set; } = string.Empty;
    public Vector2I TargetCoordinate { get; set; }
    public int Seed { get; set; }
    public List<string> SelectedAwakenedUnitInstanceIds { get; } = new();
    public List<string> SelectedMassUnitInstanceIds { get; } = new();
    public List<SelectedUnitPlatformItem> SelectedUnitPlatformItems { get; } = new();
    public List<ItemStack> SelectedStackItems { get; } = new();
    public List<string> SelectedItemInstanceIds { get; } = new();
    public List<string> RelatedTransferIds { get; } = new();
    public float WeightLimit { get; set; }
    public float UsedWeight { get; set; }
    public string CreatedFromOrbitStateId { get; set; } = string.Empty;
}
