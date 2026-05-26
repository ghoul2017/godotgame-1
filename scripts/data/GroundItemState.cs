using System.Collections.Generic;
using Godot;

namespace GodotGame;

public sealed class GroundItemState
{
    public string GroundItemStateId { get; set; } = string.Empty;
    public Vector2I Position { get; set; }
    public ItemStack Stack { get; set; } = new();
    public List<string> ItemInstanceIds { get; } = new();
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public double CreatedAtRunTime { get; set; }
}
