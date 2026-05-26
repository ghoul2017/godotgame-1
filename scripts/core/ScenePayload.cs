using Godot;
using Godot.Collections;

namespace GodotGame;

public sealed class ScenePayload
{
    public string FromScene { get; set; } = string.Empty;
    public string TargetScene { get; set; } = string.Empty;
    public string PayloadType { get; set; } = string.Empty;
    public Dictionary<string, Variant> Data { get; } = new();
    public bool DebugEnabled { get; set; }
    public int Seed { get; set; }
}
