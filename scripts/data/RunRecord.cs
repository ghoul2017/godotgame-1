using System.Collections.Generic;

namespace GodotGame;

public sealed class RunRecord
{
    public string RunRecordId { get; set; } = System.Guid.NewGuid().ToString("N");
    public string ExpeditionId { get; set; } = string.Empty;
    public int Seed { get; set; }
    public string TargetCoordinate { get; set; } = string.Empty;
    public DropPlan? DropPlanSnapshot { get; set; }
    public List<ItemStack> BroughtItems { get; } = new();
    public List<ItemStack> ReturnedItems { get; } = new();
    public List<string> ReturnedItemInstanceIds { get; } = new();
    public List<string> ReturnedAwakenedUnitIds { get; } = new();
    public List<string> ReturnedChipIds { get; } = new();
    public List<string> ReturnedBlueprintIds { get; } = new();
    public List<string> DiscoveredBlueprintIds { get; } = new();
    public List<string> DiscoveredChipIds { get; } = new();
    public List<string> DiscoveredCoordinateIds { get; } = new();
    public List<string> RelatedTransferIds { get; } = new();
    public List<string> LostUnits { get; } = new();
    public List<string> LeftSurfaceAssetIds { get; } = new();
    public List<string> DiscoveredIds { get; } = new();
    public string LeftBehindSummary { get; set; } = string.Empty;
    public string ReturnReason { get; set; } = "manual_return";
}
