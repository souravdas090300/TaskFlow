namespace TaskFlow.API.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendOrderConfirmationEmailAsync(string toEmail, string customerName, string orderNumber, decimal total, string orderDate)
    {
        // TODO: Implement actual email sending with Resend or other provider
        Console.WriteLine($"[EMAIL MOCK] Order Confirmation sent to {toEmail}");
        Console.WriteLine($"  Customer: {customerName}");
        Console.WriteLine($"  Order: {orderNumber}");
        Console.WriteLine($"  Total: ${total:F2}");
        Console.WriteLine($"  Date: {orderDate}");
        await Task.CompletedTask;
    }

    public async Task SendShippedNotificationEmailAsync(string toEmail, string customerName, string orderNumber, string trackingNumber)
    {
        // TODO: Implement actual email sending with Resend or other provider
        Console.WriteLine($"[EMAIL MOCK] Shipped notification sent to {toEmail}");
        Console.WriteLine($"  Customer: {customerName}");
        Console.WriteLine($"  Order: {orderNumber}");
        Console.WriteLine($"  Tracking: {trackingNumber}");
        await Task.CompletedTask;
    }

    public async Task SendLowStockAlertEmailAsync(string adminEmail, string productName, int currentStock, int threshold)
    {
        // TODO: Implement actual email sending with Resend or other provider
        Console.WriteLine($"[EMAIL MOCK] Low stock alert sent to {adminEmail}");
        Console.WriteLine($"  Product: {productName}");
        Console.WriteLine($"  Current Stock: {currentStock}");
        Console.WriteLine($"  Threshold: {threshold}");
        await Task.CompletedTask;
    }
}
