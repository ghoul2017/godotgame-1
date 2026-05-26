using System.Collections.Generic;

namespace GodotGame;

public sealed class GameSession
{
    public string SessionId { get; set; } = System.Guid.NewGuid().ToString("N");
    public string CurrentState { get; set; } = "boot";
    public int SaveVersion { get; set; } = 2;
    public OrbitState OrbitState { get; } = new();
    public ExpeditionState? ActiveExpedition { get; set; }
    public List<RunRecord> RunRecords { get; } = new();
    public Dictionary<string, DropPlan> DropPlans { get; } = new();
    public Dictionary<string, InventoryContainer> Inventories { get; } = new();
    public Dictionary<string, ItemInstance> ItemInstances { get; } = new();
    public Dictionary<string, UnitInstance> UnitInstances { get; } = new();
    public Dictionary<string, BuildingInstance> BuildingInstances { get; } = new();
    public List<InventoryTransfer> InventoryTransfers { get; } = new();
}
