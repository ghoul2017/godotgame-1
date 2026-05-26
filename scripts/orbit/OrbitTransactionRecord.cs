using System;
using System.Collections.Generic;

namespace GodotGame;

public sealed class OrbitTransactionRecord
{
    public string TransactionId { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public List<ItemStack> CostItems { get; } = new();
    public int CostCredits { get; set; }
    public int CreditsBefore { get; set; }
    public int CreditsAfter { get; set; }
    public List<ItemStack> RewardItems { get; } = new();
    public List<string> RewardItemInstanceIds { get; } = new();
    public List<string> UnlockBlueprintIds { get; } = new();
    public List<string> UnlockProtocolIds { get; } = new();
    public List<string> BlueprintIdsBefore { get; } = new();
    public List<string> BlueprintIdsAfter { get; } = new();
    public List<string> ProtocolIdsBefore { get; } = new();
    public List<string> ProtocolIdsAfter { get; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<string> RelatedTransferIds { get; } = new();
    public string Result { get; set; } = "success";
    public string FailureReason { get; set; } = string.Empty;
}
