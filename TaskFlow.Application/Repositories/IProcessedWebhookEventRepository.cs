using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Repositories;

/// <summary>
/// Repository interface for ProcessedWebhookEvent entity operations.
/// Used for idempotency checking in payment processing.
/// </summary>
public interface IProcessedWebhookEventRepository
{
    Task<bool> ExistsByEventIdAsync(string eventId);
    Task AddAsync(ProcessedWebhookEvent webhookEvent);
}
