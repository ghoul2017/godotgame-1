using System.Collections.Generic;
using Godot;

namespace GodotGame;

public partial class DropPodData : Resource
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float WeightLimit { get; set; }
    public int SlotLimit { get; set; }
    public int UnitCapacity { get; set; }
    public List<string> AcceptedTags { get; } = new();
    public List<string> BlockedTags { get; } = new();
    public string IconPath { get; set; } = string.Empty;
    public string SpritePath { get; set; } = string.Empty;
    public string RequiresBlueprintId { get; set; } = string.Empty;
    public List<string> RequiresProtocolIds { get; } = new();
}
