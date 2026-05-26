namespace GodotGame;

public sealed class OrbitTransactionResult
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;
    public OrbitTransactionRecord? Record { get; init; }

    public static OrbitTransactionResult Success(OrbitTransactionRecord record, string message)
    {
        return new OrbitTransactionResult
        {
            IsSuccess = true,
            Message = message,
            Record = record
        };
    }

    public static OrbitTransactionResult Fail(string message)
    {
        return new OrbitTransactionResult
        {
            Message = message
        };
    }
}
