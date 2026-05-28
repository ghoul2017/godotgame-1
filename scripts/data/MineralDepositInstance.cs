using Godot;

namespace GodotGame;

public sealed class MineralDepositInstance
{
    public string MineralDepositInstanceId { get; set; } = string.Empty;
    public string MineralDepositId { get; set; } = string.Empty;
    public string ExpeditionId { get; set; } = string.Empty;
    public Vector2I Position { get; set; }
    public int RemainingYield { get; set; }
    public bool IsDiscovered { get; set; }
    public bool IsDepleted { get; set; }
    public string ReservedByUnitInstanceId { get; set; } = string.Empty;
    public string LinkedInventoryId { get; set; } = string.Empty;
    public string SourceCoordinateId { get; set; } = string.Empty;
    public int CreatedBySeed { get; set; }
}
