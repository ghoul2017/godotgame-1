namespace GodotGame;

public enum InventoryTransferStatus
{
    Success,
    ItemNotFound,
    NotEnoughQuantity,
    TargetCapacityExceeded,
    TargetRejected,
    ItemLocked,
    MissingDefinition,
    InvalidQuantity
}

public sealed class InventoryTransferResult
{
    public InventoryTransferStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public InventoryTransfer? Transfer { get; init; }
    public bool IsSuccess => Status == InventoryTransferStatus.Success;

    public static InventoryTransferResult Success(InventoryTransfer transfer)
    {
        return new InventoryTransferResult
        {
            Status = InventoryTransferStatus.Success,
            Message = "成功",
            Transfer = transfer
        };
    }

    public static InventoryTransferResult Fail(InventoryTransferStatus status, string message)
    {
        return new InventoryTransferResult
        {
            Status = status,
            Message = message
        };
    }
}
