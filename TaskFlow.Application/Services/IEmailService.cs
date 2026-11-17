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
    /// Sends an email to the customer when their paid order is flagged for payment review
    /// (e.g. stock ran out after payment — a refund or restock is pending).
    /// </summary>
    Task SendPaymentReviewEmailAsync(string toEmail, string customerName, string orderNumber);

    /// <summary>
    /// Sends a low stock alert email to administrators.
    /// </summary>
    Task SendLowStockAlertEmailAsync(string adminEmail, string productName, int currentStock, int threshold);

    /// <summary>
    /// Sends an email verification link to a newly registered customer.
    /// </summary>
    Task SendEmailVerificationEmailAsync(string toEmail, string customerName, string verificationUrl);

    /// <summary>
    /// Sends a password reset link to a customer.
    /// </summary>
    Task SendPasswordResetEmailAsync(string toEmail, string customerName, string resetUrl);

    /// <summary>
    /// Alerts an administrator that a payment issue needs attention (usually a refund).
    /// </summary>
    Task SendPaymentIssueAlertEmailAsync(string adminEmail, string issueType, string? orderNumber, string paymentIntentId, string details);
}
