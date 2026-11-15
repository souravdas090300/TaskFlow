using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Repositories;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure;

namespace TaskFlow.Infrastructure.Repositories;

public class PaymentIssueRepository : IPaymentIssueRepository
{
    private readonly ApplicationDbContext _context;
    public PaymentIssueRepository(ApplicationDbContext context) => _context = context;

    // Raw ON CONFLICT so a duplicate does not abort the surrounding Postgres transaction.
    public Task AddIfNotExistsAsync(PaymentIssue i) =>
        _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""PaymentIssues""
              (""Id"", ""OrderId"", ""OrderNumber"", ""PaymentIntentId"", ""EventId"", ""Type"", ""Details"", ""CreatedAt"", ""IsResolved"")
            VALUES
              ({i.Id}, {i.OrderId}, {i.OrderNumber}, {i.PaymentIntentId}, {i.EventId}, {i.Type.ToString()}, {i.Details}, {i.CreatedAt}, false)
            ON CONFLICT (""EventId"") DO NOTHING");

    public Task<List<PaymentIssue>> GetUnresolvedAsync() =>
        _context.PaymentIssues.AsNoTracking().Where(p => !p.IsResolved).OrderBy(p => p.CreatedAt).ToListAsync();

    public async Task<bool> ResolveAsync(Guid id, string note)
    {
        var rows = await _context.PaymentIssues
            .Where(p => p.Id == id && !p.IsResolved)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.IsResolved, true)
                .SetProperty(p => p.ResolvedAt, (DateTime?)DateTime.UtcNow)
                .SetProperty(p => p.ResolutionNote, note));
        return rows == 1;
    }

    public Task<PaymentIssue?> GetByIdAsync(Guid id) =>
        _context.PaymentIssues.FindAsync(id).AsTask();

    public Task<int> GetUnresolvedCountAsync() =>
        _context.PaymentIssues.CountAsync(p => !p.IsResolved);
}
