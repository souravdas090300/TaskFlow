using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Repositories;

/// <summary>
/// Repository interface for Cart entity operations.
/// </summary>
public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(Guid userId);
    Task<Cart?> GetByUserIdWithItemsAsync(Guid userId);
    /// <summary>Cart with items and products included, for checkout validation and order creation.</summary>
    Task<Cart?> GetForCheckoutAsync(Guid userId);
    Task<Cart> AddAsync(Cart cart);
    Task AddItemAsync(CartItem item);
    Task UpdateAsync(Cart cart);
    Task DeleteAsync(Cart cart);
    Task ClearCartItemsAsync(Guid userId);
}
