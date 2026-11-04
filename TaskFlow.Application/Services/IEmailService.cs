namespace TaskFlow.Application.Services;

/// <summary>
/// Email service interface for sending transactional emails.
/// Implementations can use Resend, SendGrid, or other providers.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an order confirmation email to the customer.
    /// </summary>
    Task SendOrderConfirmationEmailAsync(string toEmail, string customerName, string orderNumber, decimal total, string orderDate);

    /// <summary>
    /// Sends a shipping notification email when an order is shipped.
    /// </summary>
    Task SendShippedNotificationEmailAsync(string toEmail, string customerName, string orderNumber, string trackingNumber);

    /// <summary>
    /// Sends a low stock alert email to administrators.
    /// </summary>
    Task SendLowStockAlertEmailAsync(string adminEmail, string productName, int currentStock, int threshold);
}
