using System.Collections.Generic;
using Godot;

namespace GodotGame;

public sealed class SurfaceMapState
{
    public string TargetCoordinateId { get; set; } = string.Empty;
    public int Seed { get; set; }
    public Vector2I DropPosition { get; set; }
    public List<string> ExploredRegionIds { get; } = new();
    public List<string> DiscoveredMineralDepositIds { get; } = new();
    public List<string> DiscoveredMineralSourceIds => DiscoveredMineralDepositIds;
    public List<string> DiscoveredRuinIds { get; } = new();
    public List<string> LeftAssetIds { get; } = new();
    public Dictionary<string, string> EventStates { get; } = new();
}
