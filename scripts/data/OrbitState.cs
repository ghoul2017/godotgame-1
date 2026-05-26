using System.Collections.Generic;

namespace GodotGame;

public sealed class OrbitState
{
    public Dictionary<string, int> Inventory { get; } = new();
    public int Credits { get; set; }
    public List<string> UnlockedBlueprints { get; } = new();
    public List<string> UnlockedProtocols { get; } = new();
    public List<string> StoredChipIds { get; } = new();
    public List<string> AwakenedUnits { get; } = new();
    public Dictionary<string, bool> StoryFlags { get; } = new();
}
