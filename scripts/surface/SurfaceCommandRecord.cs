using System.Collections.Generic;
using Godot;

namespace GodotGame;

public sealed class SurfaceCommandRecord
{
    public string RecordId { get; set; } = System.Guid.NewGuid().ToString("N");
    public string ExpeditionId { get; set; } = string.Empty;
    public string CommandId { get; set; } = string.Empty;
    public string CommandType { get; set; } = string.Empty;
    public List<string> UnitInstanceIds { get; } = new();
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public Vector2 TargetPosition { get; set; }
    public string Result { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public double CreatedAt { get; set; }
}
