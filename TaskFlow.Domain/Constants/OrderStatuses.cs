namespace TaskFlow.Domain.Constants;

public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Processing = "Processing";
    public const string Shipped = "Shipped";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
    public const string PaymentReview = "PaymentReview";
    public const string Refunded = "Refunded";
}

public static class PaymentMethodNames
{
    public const string Stripe = "Stripe";
    public const string CashOnDelivery = "CashOnDelivery";
}
