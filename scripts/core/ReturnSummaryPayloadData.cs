using System.Collections.Generic;

namespace GodotGame;

public sealed class ReturnSummaryPayloadData
{
    public string ExpeditionId { get; set; } = string.Empty;
    public List<ItemStack> BroughtItems { get; } = new();
    public List<ItemStack> ReturnCargo { get; } = new();
    public List<string> ReturnedAwakenedUnitIds { get; } = new();
    public List<string> ReturnedChipIds { get; } = new();
    public List<string> ReturnedBlueprintIds { get; } = new();
    public List<string> LostUnits { get; } = new();
    public List<string> LeftSurfaceAssetIds { get; } = new();
    public List<string> DiscoveredIds { get; } = new();
}
