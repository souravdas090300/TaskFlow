using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Repositories;
using TaskFlow.Infrastructure;

namespace TaskFlow.Infrastructure.Repositories;

public class AddressLookup : IAddressLookup
{
    private readonly ApplicationDbContext _context;

    public AddressLookup(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsForUserAsync(Guid addressId, Guid userId)
    {
        return await _context.Addresses
            .AnyAsync(a => a.Id == addressId && a.UserId == userId);
    }
}
