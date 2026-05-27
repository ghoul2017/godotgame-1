using System.Collections.Generic;
using Godot;

namespace GodotGame;

public sealed class ExpeditionState
{
    public string ExpeditionId { get; set; } = string.Empty;
    public int Seed { get; set; }
    public string DropPlanId { get; set; } = string.Empty;
    public string TargetCoordinateId { get; set; } = string.Empty;
    public string DropPodCargoInventoryId { get; set; } = string.Empty;
    public List<string> ActiveUnitInstanceIds { get; } = new();
    public List<string> LocationInventoryIds { get; } = new();
    public List<string> BuildingInstanceIds { get; } = new();
    public List<string> GroundItemStateIds { get; } = new();
    public List<string> ConstructionSiteIds { get; } = new();
    public List<string> LogisticsOrderIds { get; } = new();
    public List<string> DiscoveredIds { get; } = new();
    public Dictionary<string, string> EventState { get; } = new();
    public double CreatedAtRunTime { get; set; }
    public Vector2I DropPosition { get; set; }
    public List<UnitStack> InitialUnits { get; } = new();
    public List<ItemStack> InitialItems { get; } = new();
    public SurfaceMapState MapState { get; } = new();
    public RocketState RocketState { get; } = new();
}
