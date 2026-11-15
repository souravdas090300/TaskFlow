namespace TaskFlow.Application.Services;

public class OrderSettings
{
    public decimal TaxRate { get; set; } = 0.18m;
    public decimal FreeShippingThreshold { get; set; } = 50.00m;
    public decimal ShippingCost { get; set; } = 5.99m;
    public string Currency { get; set; } = "EUR";
}

public static class OrderPricing
{
    public static (decimal Subtotal, decimal Tax, decimal Shipping, decimal Total) Calculate(decimal subtotal, OrderSettings settings)
    {
        var tax = subtotal * settings.TaxRate;
        var shipping = subtotal >= settings.FreeShippingThreshold ? 0 : settings.ShippingCost;
        var total = subtotal + tax + shipping;
        return (subtotal, tax, shipping, total);
    }
}
