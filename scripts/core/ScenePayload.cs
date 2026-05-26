using Godot;
using System.Collections.Generic;

namespace GodotGame;

public sealed class ScenePayload
{
    public string FromScene { get; set; } = string.Empty;
    public string TargetScene { get; set; } = string.Empty;
    public string PayloadType { get; set; } = string.Empty;
    public Godot.Collections.Dictionary<string, Variant> Data { get; } = new();
    public List<ItemStack> ReturnCargo { get; } = new();
    public List<string> LostUnits { get; } = new();
    public List<string> DiscoveredIds { get; } = new();
    public bool DebugEnabled { get; set; }
    public int Seed { get; set; }
}
