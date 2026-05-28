using Godot;

namespace GodotGame;

public sealed class GatherRecord
{
    public string GatherRecordId { get; set; } = System.Guid.NewGuid().ToString("N");
    public string ExpeditionId { get; set; } = string.Empty;
    public string UnitInstanceId { get; set; } = string.Empty;
    public string MineralDepositInstanceId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public int Count { get; set; }
    public string TargetLocationType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string DestinationInventoryId { get; set; } = string.Empty;
    public string GroundItemStateId { get; set; } = string.Empty;
    public string TransferId { get; set; } = string.Empty;
    public string Result { get; set; } = "pending";
    public Vector2I Position { get; set; }
    public string FailureReason { get; set; } = string.Empty;
    public double CreatedAt { get; set; }
}
