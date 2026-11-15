namespace TaskFlow.Application.Services;

public enum CreateOrderError
{
    CartEmpty,
    AddressNotFound,
    ProductUnavailable,
    InsufficientStock
}

public record CreateOrderResult(bool IsSuccess, Guid? OrderId = null, string? OrderNumber = null, CreateOrderError? Error = null, string? Message = null)
{
    public static CreateOrderResult Ok(Guid orderId, string orderNumber) => new(true, orderId, orderNumber);
    public static CreateOrderResult Fail(CreateOrderError error, string message) => new(false, Error: error, Message: message);
}

public enum CashOnDeliveryResult
{
    Confirmed,
    OrderNotFound,
    NotPending,
    NotCashOnDelivery,
    OutOfStock
}
