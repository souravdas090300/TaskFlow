using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Repositories;

public interface IPaymentIssueRepository
{
    /// <summary>One issue per Stripe event: INSERT ... ON CONFLICT (EventId) DO NOTHING, safe inside a transaction.</summary>
    Task AddIfNotExistsAsync(PaymentIssue issue);
    Task<List<PaymentIssue>> GetUnresolvedAsync();
    Task<bool> ResolveAsync(Guid id, string note);
    Task<PaymentIssue?> GetByIdAsync(Guid id);
    Task<int> GetUnresolvedCountAsync();
}
