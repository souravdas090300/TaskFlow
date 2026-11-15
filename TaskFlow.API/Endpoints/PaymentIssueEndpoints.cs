using TaskFlow.Application.Repositories;

namespace TaskFlow.API.Endpoints;

public record ResolvePaymentIssueRequest(string Note);

public static class PaymentIssueEndpoints
{
    public static WebApplication MapPaymentIssueEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/payment-issues")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));   // use your existing admin policy if you have one

        group.MapGet("/", async (IPaymentIssueRepository repo) =>
        {
            var open = await repo.GetUnresolvedAsync();
            return Results.Ok(open.Select(i => new
            {
                i.Id, i.OrderId, i.OrderNumber, i.PaymentIntentId, Type = i.Type.ToString(), i.Details, i.CreatedAt
            }));
        });

        group.MapPost("/{id:guid}/resolve", async (Guid id, ResolvePaymentIssueRequest body, IPaymentIssueRepository repo) =>
        {
            if (string.IsNullOrWhiteSpace(body.Note))
                return Results.BadRequest(new { error = "A resolution note is required (for example: refunded in Stripe, re_xxx)." });

            var resolved = await repo.ResolveAsync(id, body.Note.Trim());
            return resolved ? Results.NoContent() : Results.NotFound();
        });

        return app;
    }
}
