namespace GodotGame;

public sealed class ScenePayload
{
    public string FromScene { get; set; } = string.Empty;
    public string TargetScene { get; set; } = string.Empty;
    public string PayloadType { get; set; } = string.Empty;
    public NavigationPayloadData? NavigationData { get; set; }
    public ExpeditionStartPayloadData? ExpeditionStartData { get; set; }
    public ReturnSummaryPayloadData? ReturnSummaryData { get; set; }
    public bool DebugEnabled { get; set; }
    public int Seed { get; set; }
}
