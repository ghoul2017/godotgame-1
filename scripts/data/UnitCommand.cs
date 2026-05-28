using System.Collections.Generic;
using Godot;

namespace GodotGame;

public sealed class UnitCommand
{
    public string CommandId { get; set; } = System.Guid.NewGuid().ToString("N");
    public string CommandType { get; set; } = string.Empty;
    public List<string> SourceUnitInstanceIds { get; } = new();
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public Vector2 TargetPosition { get; set; }
    public string IssuedBy { get; set; } = "player";
    public double IssuedAt { get; set; }
    public string QueueMode { get; set; } = "replace";
    public List<string> RequiredCommandTags { get; } = new();
    public string ValidationState { get; set; } = "pending";
    public string FailureReason { get; set; } = string.Empty;
}
