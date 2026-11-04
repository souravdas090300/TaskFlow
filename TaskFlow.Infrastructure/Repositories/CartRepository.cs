using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Repositories;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure;

namespace TaskFlow.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly ApplicationDbContext _context;

    public CartRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Cart?> GetByUserIdAsync(Guid userId)
    {
        return await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<Cart?> GetByUserIdWithItemsAsync(Guid userId)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<Cart> AddAsync(Cart cart)
    {
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        return cart;
    }

    public async Task UpdateAsync(Cart cart)
    {
        _context.Carts.Update(cart);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Cart cart)
    {
        _context.Carts.Remove(cart);
        await _context.SaveChangesAsync();
    }
}
