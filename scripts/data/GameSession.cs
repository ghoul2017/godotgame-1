using System.Collections.Generic;

namespace GodotGame;

public sealed class GameSession
{
    public string SessionId { get; set; } = System.Guid.NewGuid().ToString("N");
    public string CurrentState { get; set; } = "boot";
    public OrbitState OrbitState { get; } = new();
    public ExpeditionState? ActiveExpedition { get; set; }
    public List<RunRecord> RunRecords { get; } = new();
}
