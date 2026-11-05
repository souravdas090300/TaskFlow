using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Repositories;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure;

namespace TaskFlow.Infrastructure.Repositories;

public class ProcessedWebhookEventRepository : IProcessedWebhookEventRepository
{
    private readonly ApplicationDbContext _context;

    public ProcessedWebhookEventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsByEventIdAsync(string eventId)
    {
        return await _context.ProcessedWebhookEvents.AnyAsync(e => e.EventId == eventId);
    }

    public async Task AddAsync(ProcessedWebhookEvent webhookEvent)
    {
        _context.ProcessedWebhookEvents.Add(webhookEvent);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> TryMarkProcessedAsync(string eventId)
    {
        // ON CONFLICT DO NOTHING does not abort the surrounding Postgres transaction
        var rows = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""ProcessedWebhookEvents"" (""EventId"", ""ProcessedAt"")
            VALUES ({eventId}, {DateTime.UtcNow})
            ON CONFLICT (""EventId"") DO NOTHING");
        return rows == 1;
    }
}
