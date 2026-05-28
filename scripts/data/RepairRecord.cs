using System.Collections.Generic;

namespace GodotGame;

public sealed class RepairRecord
{
    public string RepairRecordId { get; set; } = System.Guid.NewGuid().ToString("N");
    public string ExpeditionId { get; set; } = string.Empty;
    public string UnitInstanceId { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public List<ItemStack> ConsumedItems { get; } = new();
    public List<string> ConsumedTransferIds { get; } = new();
    public int DurabilityBefore { get; set; }
    public int DurabilityAfter { get; set; }
    public string Result { get; set; } = "pending";
    public string FailureReason { get; set; } = string.Empty;
}
