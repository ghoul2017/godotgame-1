namespace GodotGame;

public sealed class PowerNetworkState
{
    public string PowerNetworkId { get; set; } = string.Empty;
    public string ExpeditionId { get; set; } = string.Empty;
    public int TotalGeneration { get; set; }
    public int TotalConsumption { get; set; }
    public int StoredEnergy { get; set; }
    public int StorageCapacity { get; set; }
    public string State { get; set; } = "offline";
}
