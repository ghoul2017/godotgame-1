using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGame;

[GlobalClass]
public partial class TradeOfferData : Resource
{
    [Export]
    public string Id { get; set; } = string.Empty;
    [Export]
    public string DisplayName { get; set; } = string.Empty;
    [Export]
    public string Category { get; set; } = string.Empty;
    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; } = string.Empty;
    [Export]
    public Godot.Collections.Array<string> CostItemIds { get; set; } = new();
    [Export]
    public Godot.Collections.Array<int> CostItemCounts { get; set; } = new();
    public List<ItemStack> CostItems { get; } = new();
    [Export]
    public int CostCredits { get; set; }
    [Export]
    public Godot.Collections.Array<string> RewardItemIds { get; set; } = new();
    [Export]
    public Godot.Collections.Array<int> RewardItemCounts { get; set; } = new();
    public List<ItemStack> RewardItems { get; } = new();
    [Export]
    public Godot.Collections.Array<string> RequiredBlueprintIds { get; set; } = new();
    [Export]
    public Godot.Collections.Array<string> RequiredProtocolIds { get; set; } = new();
    [Export]
    public Godot.Collections.Array<string> RequiredStoryFlags { get; set; } = new();
    [Export]
    public int StockLimit { get; set; }
    [Export]
    public string IconPath { get; set; } = string.Empty;

    public IReadOnlyList<ItemStack> GetCostItems()
    {
        return CostItems.Count > 0 ? CostItems : BuildStacks(CostItemIds, CostItemCounts);
    }

    public IReadOnlyList<ItemStack> GetRewardItems()
    {
        return RewardItems.Count > 0 ? RewardItems : BuildStacks(RewardItemIds, RewardItemCounts);
    }

    public void AddCostItem(string itemId, int count)
    {
        CostItems.Add(new ItemStack { ItemId = itemId, Count = count });
        CostItemIds.Add(itemId);
        CostItemCounts.Add(count);
    }

    public void AddRewardItem(string itemId, int count)
    {
        RewardItems.Add(new ItemStack { ItemId = itemId, Count = count });
        RewardItemIds.Add(itemId);
        RewardItemCounts.Add(count);
    }

    private static List<ItemStack> BuildStacks(IReadOnlyList<string> itemIds, IReadOnlyList<int> counts)
    {
        int count = System.Math.Min(itemIds.Count, counts.Count);
        return Enumerable.Range(0, count)
            .Where(index => !string.IsNullOrWhiteSpace(itemIds[index]) && counts[index] > 0)
            .Select(index => new ItemStack { ItemId = itemIds[index], Count = counts[index] })
            .ToList();
    }
}
