using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Repositories;

/// <summary>
/// Repository interface for Order entity operations.
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<Order?> GetByIdWithItemsAsync(Guid id);
    Task<Order?> GetByIdWithItemsAndUserAsync(Guid id, Guid userId);
    Task<List<Order>> GetUserOrdersAsync(Guid userId);
    Task<List<Order>> GetAllOrdersAsync();
    Task<Order> AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task DeleteAsync(Order order);
    Task<bool> ExistsByOrderNumberAsync(string orderNumber);
    Task<bool> TryMarkPaidAsync(Guid orderId, string paymentIntentId, DateTime paidAtUtc);
    Task MarkPaymentReviewAsync(Guid orderId, string paymentIntentId);
}
