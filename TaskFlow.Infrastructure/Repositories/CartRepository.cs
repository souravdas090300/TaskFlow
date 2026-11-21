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
                    .ThenInclude(p => p.Images)
            .Include(c => c.Items)
                .ThenInclude(i => i.ProductVariant)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public Task<Cart?> GetForCheckoutAsync(Guid userId) => GetByUserIdWithItemsAsync(userId);

    public async Task<Cart> AddAsync(Cart cart)
    {
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        return cart;
    }

    public async Task UpdateAsync(Cart cart)
    {
        foreach (var item in cart.Items)
        {
            var entry = _context.Entry(item);
            if (entry.State == EntityState.Detached)
            {
                entry.State = EntityState.Added;
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Cart cart)
    {
        _context.Carts.Remove(cart);
        await _context.SaveChangesAsync();
    }

    public async Task ClearCartItemsAsync(Guid userId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart != null && cart.Items.Any())
        {
            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync();
        }
    }
}
