using System.Collections.Generic;
using Godot;

namespace GodotGame;

public sealed class SurfaceUnitRuntimeState
{
    public string UnitInstanceId { get; set; } = string.Empty;
    public string ExpeditionId { get; set; } = string.Empty;
    public Vector2 Position { get; set; }
    public float FacingAngle { get; set; }
    public string MovementState { get; set; } = "idle";
    public string CurrentCommandId { get; set; } = string.Empty;
    public List<UnitCommand> CommandQueue { get; } = new();
    public string CurrentTargetId { get; set; } = string.Empty;
    public Vector2 CurrentTargetPosition { get; set; }
    public Vector2 LastReachablePosition { get; set; }
    public bool IsSelected { get; set; }
    public bool IsControllable { get; set; } = true;
    public List<int> SelectionGroupIds { get; } = new();
    public string LastErrorCode { get; set; } = string.Empty;
}
