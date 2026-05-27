using System.Collections.Generic;
using Godot;

namespace GodotGame;

public sealed class DropConfigSession
{
    public string SessionId { get; set; } = System.Guid.NewGuid().ToString("N");
    public string SourceOrbitStateId { get; set; } = string.Empty;
    public string SelectedCoordinateId { get; set; } = string.Empty;
    public Vector2I TargetCoordinate { get; set; }
    public string SelectedDropPodId { get; set; } = string.Empty;
    public int Seed { get; set; }
    public List<string> SelectedAwakenedUnitInstanceIds { get; } = new();
    public List<string> SelectedMassUnitInstanceIds { get; } = new();
    public List<SelectedUnitPlatformItem> SelectedUnitPlatformItems { get; } = new();
    public List<ItemStack> SelectedStackItems { get; } = new();
    public List<string> SelectedItemInstanceIds { get; } = new();
    public float UsedWeight { get; set; }
    public float WeightLimit { get; set; }
    public int UsedSlots { get; set; }
    public int SlotLimit { get; set; }
    public int UsedUnitCapacity { get; set; }
    public int UnitCapacity { get; set; }
    public List<string> ValidationErrors { get; } = new();
    public List<string> ValidationWarnings { get; } = new();

    public bool IsValid => ValidationErrors.Count == 0;
}
