# Resend Email Service Registration

## Overview
TaskFlow uses Resend for production email delivery. The application automatically uses `MockEmailService` in development and `ResendEmailService` in production.

## Configuration

### 1. Create a Resend Account
1. Go to https://resend.com and sign up
2. Navigate to API Keys: https://resend.com/api-keys
3. Create a new API key
4. Copy the API key (starts with `re_`)

### 2. Verify Your Domain (Required for Production)
**Before sending emails, you MUST verify your domain in Resend:**

1. In Resend dashboard, go to **Domains** → **Add Domain**
2. Enter your domain (e.g., `taskflow-sourav.duckdns.org` or your custom domain)
3. Resend will provide DNS records for SPF and DKIM
4. Add these records to your DNS provider:
   - **SPF record**: TXT record at your domain root
   - **DKIM record**: CNAME record provided by Resend
5. Wait for DNS propagation (usually 5-10 minutes)
6. Click **Verify** in Resend dashboard
7. Once verified, you can send emails from any address on that domain

**Why this is required:**
- Email providers (Gmail, Outlook, etc.) reject emails from unverified domains
- SPF and DKIM prevent your emails from being marked as spam
- Without verification, emails will likely end up in spam folders

### 3. Configure Environment Variables

**For Development (.env or .env.dev):**
```bash
# Resend is optional in development (MockEmailService is used)
RESEND_API_KEY=re_your_test_key
RESEND_FROM_EMAIL=noreply@yourdomain.com
```

**For Production (.env.prod):**
```bash
# Required in production
RESEND_API_KEY=re_your_production_key
RESEND_FROM_EMAIL=noreply@yourdomain.com
```

**Note:** `RESEND_FROM_EMAIL` must be on your verified domain.

### 4. Application Configuration

The email service is automatically registered in `Program.cs`:

```csharp
// Register email service
builder.Services.AddHttpClient<ResendEmailService>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
});
builder.Services.Configure<ResendOptions>(builder.Configuration.GetSection("Resend"));

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IEmailService, MockEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, ResendEmailService>();
}
```

## Testing Email Delivery

### Development (MockEmailService)
In development, emails are logged to the console but not actually sent. Check your application logs for:
```
[MockEmailService] Order confirmation email would be sent to: user@example.com
```

### Production (ResendEmailService)
In production, emails are sent via Resend API. Check:
1. Resend dashboard logs: https://resend.com/logs
2. Application logs for errors
3. Ensure your domain is verified before expecting delivery

## Idempotency
The `ResendEmailService` uses the `Idempotency-Key` header to prevent duplicate emails:
- Order confirmation: `order-confirmation-{orderNumber}`
- Shipped notification: `order-shipped-{orderNumber}-{trackingNumber}`

If the same email is sent twice with the same idempotency key, Resend will only deliver it once.

## Error Handling
- Email failures are logged but do NOT fail the order
- CheckoutService catches and logs email exceptions
- Orders complete successfully even if email fails
- Check logs for any email delivery issues

## Troubleshooting

### Emails not arriving in production
1. Verify your domain is SPF+DKIM verified in Resend
2. Check Resend dashboard logs for failed sends
3. Check application logs for `Resend returned 4xx/5xx` errors
4. Ensure `RESEND_API_KEY` is correct
5. Ensure `RESEND_FROM_EMAIL` is on a verified domain

### Emails going to spam
1. Domain must be verified (SPF + DKIM)
2. Use a proper domain, not a free email provider
3. Check email content for spam triggers
4. Set up proper DNS records (SPF, DKIM, DMARC)

### API key errors
- Ensure API key starts with `re_`
- Check the key has the correct permissions
- Regenerate key if compromised

## Cost
- Resend offers 3,000 free emails per month
- After that: $1 per 1,000 emails
- For a small e-commerce site, the free tier is usually sufficient

## Security
- Never commit `RESEND_API_KEY` to version control
- Rotate API keys if compromised
- Use production keys only in production environment
- Use test keys for development/staging
