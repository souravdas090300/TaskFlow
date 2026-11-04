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
}
