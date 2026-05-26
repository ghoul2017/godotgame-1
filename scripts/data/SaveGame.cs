using System;
using System.Collections.Generic;

namespace GodotGame;

public sealed class SaveGame
{
    public int SaveVersion { get; set; } = 2;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string ActiveSceneId { get; set; } = SceneId.Main;
    public GameSession GameSession { get; set; } = new();
    public Dictionary<string, DropPlan> DropPlans => GameSession.DropPlans;
    public Dictionary<string, ItemInstance> ItemInstances => GameSession.ItemInstances;
    public Dictionary<string, UnitInstance> UnitInstances => GameSession.UnitInstances;
    public Dictionary<string, BuildingInstance> BuildingInstances => GameSession.BuildingInstances;
    public Dictionary<string, InventoryContainer> Inventories => GameSession.Inventories;
    public List<InventoryTransfer> InventoryTransfers => GameSession.InventoryTransfers;
}
