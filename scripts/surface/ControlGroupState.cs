using System.Collections.Generic;

namespace GodotGame;

public sealed class ControlGroupState
{
    public int GroupIndex { get; set; }
    public List<string> UnitInstanceIds { get; } = new();
    public double UpdatedAt { get; set; }
}
