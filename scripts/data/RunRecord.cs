using System.Collections.Generic;

namespace GodotGame;

public sealed class RunRecord
{
    public string ExpeditionId { get; set; } = string.Empty;
    public int Seed { get; set; }
    public List<ItemStack> BroughtItems { get; } = new();
    public List<ItemStack> ReturnedItems { get; } = new();
    public List<string> LostUnits { get; } = new();
    public List<string> DiscoveredIds { get; } = new();
}
