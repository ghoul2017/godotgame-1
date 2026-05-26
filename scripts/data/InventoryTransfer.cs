namespace GodotGame;

public sealed class InventoryTransfer
{
    public string TransferId { get; set; } = string.Empty;
    public string FromInventoryId { get; set; } = string.Empty;
    public string ToInventoryId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public int Count { get; set; }
    public System.Collections.Generic.List<string> ItemInstanceIds { get; } = new();
    public string Reason { get; set; } = string.Empty;
    public string ExpeditionId { get; set; } = string.Empty;
    public string RelatedRunRecordId { get; set; } = string.Empty;
}
