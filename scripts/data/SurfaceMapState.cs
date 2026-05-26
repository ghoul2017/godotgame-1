using System.Collections.Generic;

namespace GodotGame;

public sealed class SurfaceMapState
{
    public List<string> ExploredRegionIds { get; } = new();
    public List<string> DiscoveredResourcePointIds { get; } = new();
    public List<string> DiscoveredRuinIds { get; } = new();
    public List<string> LeftAssetIds { get; } = new();
    public Dictionary<string, string> EventStates { get; } = new();
}
