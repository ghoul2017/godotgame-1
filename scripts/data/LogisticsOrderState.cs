using System.Collections.Generic;

namespace GodotGame;

public sealed class LogisticsOrderState
{
    public string LogisticsOrderId { get; set; } = string.Empty;
    public string ExpeditionId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public int Count { get; set; }
    public ItemLocation SourceLocation { get; set; } = new();
    public ItemLocation TargetLocation { get; set; } = new();
    public string AssignedUnitInstanceId { get; set; } = string.Empty;
    public string State { get; set; } = "pending";
    public List<string> ReservedItemInstanceIds { get; } = new();
    public string CreatedBy { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
}
