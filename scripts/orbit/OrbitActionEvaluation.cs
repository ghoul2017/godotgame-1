namespace GodotGame;

public sealed class OrbitActionEvaluation
{
    public bool CanExecute { get; init; }
    public bool IsCompleted { get; init; }
    public string StatusText { get; init; } = string.Empty;
    public string FailureReason { get; init; } = string.Empty;

    public static OrbitActionEvaluation Ready(string statusText = "可执行")
    {
        return new OrbitActionEvaluation
        {
            CanExecute = true,
            StatusText = statusText
        };
    }

    public static OrbitActionEvaluation Blocked(string statusText, string failureReason)
    {
        return new OrbitActionEvaluation
        {
            StatusText = statusText,
            FailureReason = failureReason
        };
    }

    public static OrbitActionEvaluation Completed(string statusText = "已完成")
    {
        return new OrbitActionEvaluation
        {
            IsCompleted = true,
            StatusText = statusText,
            FailureReason = statusText
        };
    }
}
