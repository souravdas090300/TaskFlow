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
    /// <summary>Adds an order and its items in one SaveChanges.</summary>
    Task AddWithItemsAsync(Order order, IEnumerable<OrderItem> items);
    Task<List<OrderItem>> GetItemsAsync(Guid orderId);
    /// <summary>Atomic Pending -> Paid transition for COD orders owned by the user. False = not eligible.</summary>
    Task<bool> TryConfirmCashOnDeliveryAsync(Guid orderId, Guid userId, DateTime confirmedAtUtc);
    Task UpdateAsync(Order order);
    Task DeleteAsync(Order order);
    Task<bool> ExistsByOrderNumberAsync(string orderNumber);
    Task<bool> TryMarkPaidAsync(Guid orderId, string paymentIntentId, DateTime paidAtUtc);
    Task<bool> TryMarkPaidAsync(Guid orderId, string paymentIntentId, string eventId, DateTime paidAtUtc);
    Task MarkPaymentReviewAsync(Guid orderId, string paymentIntentId);
}
