using Godot;

namespace GodotGame;

public sealed class ItemLocation
{
    public string LocationType { get; set; } = string.Empty;
    public string InventoryId { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string UnitInstanceId { get; set; } = string.Empty;
    public string BuildingInstanceId { get; set; } = string.Empty;
    public string ConstructionSiteId { get; set; } = string.Empty;
    public string GroundItemStateId { get; set; } = string.Empty;
    public Vector2I MapPosition { get; set; }

    public bool IsInventoryBacked => !string.IsNullOrEmpty(InventoryId);

    public static ItemLocation FromInventory(string locationType, string inventoryId, string ownerId = "")
    {
        return new ItemLocation
        {
            LocationType = locationType,
            InventoryId = inventoryId,
            OwnerId = ownerId
        };
    }
}
