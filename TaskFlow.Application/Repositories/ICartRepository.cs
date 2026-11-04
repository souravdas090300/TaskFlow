using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Repositories;

/// <summary>
/// Repository interface for Cart entity operations.
/// </summary>
public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(Guid userId);
    Task<Cart?> GetByUserIdWithItemsAsync(Guid userId);
    Task<Cart> AddAsync(Cart cart);
    Task UpdateAsync(Cart cart);
    Task DeleteAsync(Cart cart);
    Task ClearCartItemsAsync(Guid userId);
}
