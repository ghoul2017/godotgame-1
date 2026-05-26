using System.Collections.Generic;
using Godot;

namespace GodotGame;

public sealed class ExpeditionStartPayloadData
{
    public string ExpeditionId { get; set; } = string.Empty;
    public int Seed { get; set; }
    public Vector2I DropPosition { get; set; }
    public List<UnitStack> InitialUnits { get; } = new();
    public List<ItemStack> InitialItems { get; } = new();
}
