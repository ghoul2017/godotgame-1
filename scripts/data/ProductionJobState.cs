namespace GodotGame;

public sealed class ProductionJobState
{
    public string ProductionJobId { get; set; } = System.Guid.NewGuid().ToString("N");
    public string ExpeditionId { get; set; } = string.Empty;
    public string BuildingInstanceId { get; set; } = string.Empty;
    public string RecipeId { get; set; } = string.Empty;
    public string State { get; set; } = "queued";
    public float Progress { get; set; }
    public string InputInventoryId { get; set; } = string.Empty;
    public string OutputInventoryId { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
}
