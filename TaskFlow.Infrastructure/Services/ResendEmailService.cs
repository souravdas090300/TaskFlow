using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Services;

namespace TaskFlow.Infrastructure.Services;

public class ResendOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;     // must be on a domain verified in Resend (SPF + DKIM)
}

/// <summary>Real IEmailService. Throws on failure; callers (CheckoutService) catch and log so email never fails an order.
/// Every dynamic value is HTML-encoded. Idempotency-Key stops a retry from sending the same email twice.</summary>
public class ResendEmailService : IEmailService
{
    private readonly HttpClient _http;
    private readonly ResendOptions _options;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(HttpClient http, IOptions<ResendOptions> options, ILogger<ResendEmailService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendOrderConfirmationEmailAsync(string email, string name, string orderNumber, decimal total, string paidAt)
    {
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:560px;margin:auto;color:#111827">
              <h2>Thank you, {Enc(name)}!</h2>
              <p>We have received your order <strong>{Enc(orderNumber)}</strong> on {Enc(paidAt)}.</p>
              <p>Total: <strong>{total.ToString("0.00", CultureInfo.InvariantCulture)} EUR</strong></p>
              <p style="color:#6b7280;font-size:13px">Questions about your order? Just reply to this email.</p>
            </div>
            """;
        await SendAsync(email, $"Order confirmation {orderNumber}", html, $"order-confirmation-{orderNumber}");
    }

    public async Task SendShippedNotificationEmailAsync(string email, string name, string orderNumber, string trackingNumber)
    {
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:560px;margin:auto;color:#111827">
              <h2>Good news, {Enc(name)}!</h2>
              <p>Your order <strong>{Enc(orderNumber)}</strong> has been shipped.</p>
              <p>Tracking number: <strong>{Enc(trackingNumber)}</strong></p>
            </div>
            """;
        await SendAsync(email, $"Your order {orderNumber} has shipped", html, $"order-shipped-{orderNumber}-{trackingNumber}");
    }

    public async Task SendPaymentReviewEmailAsync(string email, string name, string orderNumber)
    {
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:560px;margin:auto;color:#111827">
              <h2>We're reviewing your payment, {Enc(name)}.</h2>
              <p>Your payment for order <strong>{Enc(orderNumber)}</strong> was received, but we're doing a quick review before dispatch.</p>
              <p>No action is needed from you — we'll email you as soon as your order is confirmed or refunded.</p>
              <p style="color:#6b7280;font-size:13px">Questions? Just reply to this email.</p>
            </div>
            """;
        await SendAsync(email, $"We're reviewing your order {orderNumber}", html, $"payment-review-{orderNumber}");
    }

    public async Task SendLowStockAlertEmailAsync(string adminEmail, string productName, int currentStock, int threshold)
    {
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:560px;margin:auto;color:#111827">
              <h2>Low Stock Alert</h2>
              <p>Product <strong>{Enc(productName)}</strong> is running low on stock.</p>
              <p>Current stock: <strong>{currentStock}</strong></p>
              <p>Threshold: <strong>{threshold}</strong></p>
              <p style="color:#dc2626;font-size:13px">Please restock this item soon.</p>
            </div>
            """;
        await SendAsync(adminEmail, $"Low stock alert: {productName}", html, $"low-stock-{productName}-{currentStock}");
    }

    public async Task SendEmailVerificationEmailAsync(string email, string name, string verificationUrl)
    {
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:560px;margin:auto;color:#111827">
              <h2>Welcome, {Enc(name)}!</h2>
              <p>Please confirm your email address to finish setting up your TaskFlow account.</p>
              <p><a href="{Enc(verificationUrl)}" style="display:inline-block;background:#7c3aed;color:#fff;padding:10px 20px;border-radius:6px;text-decoration:none">Verify my email</a></p>
              <p style="color:#6b7280;font-size:13px">If you didn't create an account, you can ignore this email.</p>
            </div>
            """;
        await SendAsync(email, "Verify your TaskFlow email", html, $"email-verify-{email}");
    }

    public async Task SendPasswordResetEmailAsync(string email, string name, string resetUrl)
    {
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:560px;margin:auto;color:#111827">
              <h2>Hi {Enc(name)},</h2>
              <p>We received a request to reset your TaskFlow password.</p>
              <p><a href="{Enc(resetUrl)}" style="display:inline-block;background:#7c3aed;color:#fff;padding:10px 20px;border-radius:6px;text-decoration:none">Reset my password</a></p>
              <p style="color:#6b7280;font-size:13px">This link expires in 1 hour. If you didn't request this, you can ignore this email.</p>
            </div>
            """;
        await SendAsync(email, "Reset your TaskFlow password", html, $"password-reset-{email}");
    }

    public async Task SendPaymentIssueAlertEmailAsync(string adminEmail, string issueType, string? orderNumber, string paymentIntentId, string details)
    {
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:560px;margin:auto;color:#111827">
              <h2 style="color:#dc2626">Payment Issue: {Enc(issueType)}</h2>
              <p>Order: <strong>{Enc(orderNumber ?? "unknown")}</strong></p>
              <p>Payment intent: <strong>{Enc(paymentIntentId)}</strong></p>
              <p>{Enc(details)}</p>
              <p style="color:#6b7280;font-size:13px">Review it in the admin panel under Payment Issues.</p>
            </div>
            """;
        await SendAsync(adminEmail, $"[TaskFlow] Payment issue: {issueType}", html, $"payment-issue-{issueType}-{paymentIntentId}");
    }

    private async Task SendAsync(string to, string subject, string html, string idempotencyKey)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
            {
                Content = JsonContent.Create(new { from = _options.FromEmail, to = new[] { to }, subject, html })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            request.Headers.Add("Idempotency-Key", idempotencyKey);

            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Resend API returned {StatusCode}: {Body}", (int)response.StatusCode, body);
                throw new InvalidOperationException($"Resend returned {(int)response.StatusCode}: {body}");
            }

            _logger.LogInformation("Email sent successfully to {To} with idempotency key {Key}", to, idempotencyKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }

    private static string Enc(string value) => WebUtility.HtmlEncode(value);
}
