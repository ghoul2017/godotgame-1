using System.Collections.Generic;
using Godot;

namespace GodotGame;

public sealed class ExpeditionState
{
    public string ExpeditionId { get; set; } = string.Empty;
    public int Seed { get; set; }
    public Vector2I DropPosition { get; set; }
    public List<UnitStack> InitialUnits { get; } = new();
    public List<ItemStack> InitialItems { get; } = new();
    public Dictionary<string, int> SurfaceInventory { get; } = new();
    public Dictionary<string, string> MapState { get; } = new();
    public Dictionary<string, string> RocketState { get; } = new();
}
