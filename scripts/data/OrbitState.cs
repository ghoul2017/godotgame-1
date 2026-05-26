using System.Collections.Generic;

namespace GodotGame;

public sealed class OrbitState
{
    public string OrbitStateId { get; set; } = "orbit_default";
    public int SaveVersion { get; set; } = 2;
    public string InventoryId { get; set; } = "orbit_inventory_default";
    public Dictionary<string, int> Inventory { get; } = new();
    public int Credits { get; set; }
    public List<string> UnlockedBlueprints { get; } = new();
    public List<string> UnlockedProtocols { get; } = new();
    public List<string> StoredChipIds { get; } = new();
    public List<string> AwakenedUnits { get; } = new();
    public List<string> AvailableMassUnitInstanceIds { get; } = new();
    public List<string> KnownCoordinates { get; } = new();
    public List<string> CompletedRunRecordIds { get; } = new();
    public Dictionary<string, bool> StoryFlags { get; } = new();
}
