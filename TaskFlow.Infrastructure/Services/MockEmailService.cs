using Microsoft.Extensions.Logging;
using TaskFlow.Application.Services;

namespace TaskFlow.Infrastructure.Services;

/// <summary>
/// Mock email service for development and testing.
/// Logs email calls to console instead of sending actual emails.
/// TODO: Replace with ResendEmailService in production.
/// </summary>
public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendOrderConfirmationEmailAsync(string toEmail, string customerName, string orderNumber, decimal total, string orderDate)
    {
        _logger.LogInformation("[EMAIL MOCK] Order Confirmation sent to {Email}", toEmail);
        _logger.LogInformation("  Customer: {Customer}", customerName);
        _logger.LogInformation("  Order: {OrderNumber}", orderNumber);
        _logger.LogInformation("  Total: {Total:C}", total);
        _logger.LogInformation("  Date: {OrderDate}", orderDate);
        return Task.CompletedTask;
    }

    public Task SendShippedNotificationEmailAsync(string toEmail, string customerName, string orderNumber, string trackingNumber)
    {
        _logger.LogInformation("[EMAIL MOCK] Shipped notification sent to {Email}", toEmail);
        _logger.LogInformation("  Customer: {Customer}", customerName);
        _logger.LogInformation("  Order: {OrderNumber}", orderNumber);
        _logger.LogInformation("  Tracking: {TrackingNumber}", trackingNumber);
        return Task.CompletedTask;
    }

    public Task SendLowStockAlertEmailAsync(string adminEmail, string productName, int currentStock, int threshold)
    {
        _logger.LogInformation("[EMAIL MOCK] Low stock alert sent to {Email}", adminEmail);
        _logger.LogInformation("  Product: {ProductName}", productName);
        _logger.LogInformation("  Current Stock: {CurrentStock}", currentStock);
        _logger.LogInformation("  Threshold: {Threshold}", threshold);
        return Task.CompletedTask;
    }
}
