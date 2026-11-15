using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Repositories;
using TaskFlow.Infrastructure;

namespace TaskFlow.Infrastructure.Repositories;

public class StockRepository : IStockRepository
{
    private readonly ApplicationDbContext _context;

    public StockRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryDecrementAsync(Guid productId, int quantity)
    {
        var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"Products\" SET \"Stock\" = \"Stock\" - {0} WHERE \"Id\" = {1} AND \"Stock\" >= {0}",
            quantity, productId);

        return rowsAffected > 0;
    }
}
