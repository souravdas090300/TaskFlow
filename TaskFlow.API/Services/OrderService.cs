using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using TaskFlow.Application;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Repositories;
using TaskFlow.Application.Services;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure;

namespace TaskFlow.API.Services;

public class OrderService
{
    private const string DateFormat = "MMMM dd, yyyy";
    private const string ShippedStatus = "Shipped";

    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly IOrderRepository _orderRepository;
    private readonly IProcessedWebhookEventRepository _eventRepository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        ApplicationDbContext context,
        IConfiguration configuration,
        IEmailService emailService,
        IOrderRepository orderRepository,
        IProcessedWebhookEventRepository eventRepository,
        ILogger<OrderService> logger)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
        _orderRepository = orderRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<CheckoutDto?> GetCheckoutSummaryAsync(Guid userId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null) return null;

        var addresses = await _context.Addresses
            .Where(a => a.UserId == userId)
            .Select(a => new AddressDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Type = a.Type,
                FirstName = a.FirstName,
                LastName = a.LastName,
                AddressLine1 = a.AddressLine1,
                AddressLine2 = a.AddressLine2,
                City = a.City,
                State = a.State,
                PostalCode = a.PostalCode,
                Country = a.Country,
                Phone = a.Phone,
                IsDefault = a.IsDefault,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
        var taxRate = _configuration.GetValue<decimal>("OrderSettings:TaxRate");
        var tax = subtotal * taxRate;
        var freeShippingThreshold = _configuration.GetValue<decimal>("OrderSettings:FreeShippingThreshold");
        var shippingCost = _configuration.GetValue<decimal>("OrderSettings:ShippingCost");
        var shipping = subtotal >= freeShippingThreshold ? 0 : shippingCost;
        var total = subtotal + tax + shipping;

        return new CheckoutDto
        {
            Items = cart.Items.Select(i => new CartItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                ProductImage = i.Product.Images.FirstOrDefault()?.ImageUrl,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Total = i.UnitPrice * i.Quantity
            }).ToList(),
            Subtotal = subtotal,
            Tax = tax,
            Shipping = shipping,
            Total = total,
            Addresses = addresses
        };
    }

    public async Task<OrderDto?> CreateOrderAsync(Guid userId, CreateOrderDto dto)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.Items.Any())
            return null;

        var shippingAddress = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == dto.ShippingAddressId && a.UserId == userId);

        if (shippingAddress == null)
            return null;

        // Calculate totals server-side
        var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
        var taxRate = _configuration.GetValue<decimal>("OrderSettings:TaxRate");
        var tax = subtotal * taxRate;
        var freeShippingThreshold = _configuration.GetValue<decimal>("OrderSettings:FreeShippingThreshold");
        var shippingCost = _configuration.GetValue<decimal>("OrderSettings:ShippingCost");
        var shipping = subtotal >= freeShippingThreshold ? 0 : shippingCost;
        var total = subtotal + tax + shipping;

        // Generate order number
        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderNumber = orderNumber,
            Status = "Pending",
            PaymentMethod = dto.PaymentMethod.ToString(),
            Subtotal = subtotal,
            Tax = tax,
            ShippingCost = shipping,
            Total = total,
            CreatedAt = DateTime.UtcNow,
            ConcurrencyToken = Guid.NewGuid()
        };

        _context.Orders.Add(order);

        // Create order items with price snapshots
        foreach (var cartItem in cart.Items)
        {
            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = cartItem.ProductId,
                ProductName = cartItem.Product.Name,
                ProductImageUrl = cartItem.Product.Images.FirstOrDefault()?.ImageUrl,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.UnitPrice, // Price snapshot
                TotalPrice = cartItem.UnitPrice * cartItem.Quantity
            };
            _context.OrderItems.Add(orderItem);
        }

        await _context.SaveChangesAsync();

        // Send order confirmation email
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            await _emailService.SendOrderConfirmationEmailAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}",
                orderNumber,
                total,
                DateTime.UtcNow.ToString(DateFormat)
            );
        }

        return await GetOrderByIdAsync(userId, order.Id);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid userId, Guid orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null) return null;

        var shippingAddress = await _context.Addresses
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Type == "Shipping" && a.IsDefault);

        var billingAddress = await _context.Addresses
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Type == "Billing" && a.IsDefault);

        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            OrderNumber = order.OrderNumber,
            Status = Enum.Parse<OrderStatus>(order.Status),
            PaymentMethod = Enum.Parse<PaymentMethod>(order.PaymentMethod),
            Subtotal = order.Subtotal,
            Tax = order.Tax,
            Shipping = order.ShippingCost,
            Total = order.Total,
            StripePaymentIntentId = order.StripePaymentIntentId,
            StripeEventId = order.StripeEventId,
            CreatedAt = order.CreatedAt,
            PaidAt = order.PaidAt,
            ShippedAt = order.ShippedAt,
            DeliveredAt = order.DeliveredAt,
            CancelledAt = order.CancelledAt,
            ShippingAddress = shippingAddress != null ? new AddressDto
            {
                Id = shippingAddress.Id,
                UserId = shippingAddress.UserId,
                Type = shippingAddress.Type,
                FirstName = shippingAddress.FirstName,
                LastName = shippingAddress.LastName,
                AddressLine1 = shippingAddress.AddressLine1,
                AddressLine2 = shippingAddress.AddressLine2,
                City = shippingAddress.City,
                State = shippingAddress.State,
                PostalCode = shippingAddress.PostalCode,
                Country = shippingAddress.Country,
                Phone = shippingAddress.Phone,
                IsDefault = shippingAddress.IsDefault,
                CreatedAt = shippingAddress.CreatedAt
            } : null,
            BillingAddress = billingAddress != null ? new AddressDto
            {
                Id = billingAddress.Id,
                UserId = billingAddress.UserId,
                Type = billingAddress.Type,
                FirstName = billingAddress.FirstName,
                LastName = billingAddress.LastName,
                AddressLine1 = billingAddress.AddressLine1,
                AddressLine2 = billingAddress.AddressLine2,
                City = billingAddress.City,
                State = billingAddress.State,
                PostalCode = billingAddress.PostalCode,
                Country = billingAddress.Country,
                Phone = billingAddress.Phone,
                IsDefault = billingAddress.IsDefault,
                CreatedAt = billingAddress.CreatedAt
            } : null,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                OrderId = i.OrderId,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ProductImage = i.ProductImageUrl,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Total = i.TotalPrice
            }).ToList()
        };
    }

    public async Task<List<OrderDto>> GetUserOrdersAsync(Guid userId)
    {
        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var result = new List<OrderDto>();
        foreach (var order in orders)
        {
            var orderDto = await GetOrderByIdAsync(userId, order.Id);
            if (orderDto != null)
                result.Add(orderDto);
        }

        return result;
    }

    public async Task<PaymentResult> ProcessStripePaymentAsync(Guid orderId, string paymentIntentId, string eventId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            _logger.LogError("Stripe event {EventId}: order {OrderId} not found", eventId, orderId);
            return PaymentResult.OrderNotFound;
        }

        if (!IsOrderPayable(order, paymentIntentId))
            return PaymentResult.Rejected;

        var paidAt = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        if (!await _eventRepository.TryMarkProcessedAsync(eventId))
            return PaymentResult.AlreadyProcessed;

        if (!await _orderRepository.TryMarkPaidAsync(orderId, paymentIntentId, eventId, paidAt))
        {
            await transaction.CommitAsync(); // Keep the event record
            return PaymentResult.AlreadyProcessed;
        }

        var orderItems = await _context.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .ToListAsync();

        var stockOk = await TryDecrementStockAsync(orderItems);

        if (stockOk)
        {
            await ClearCartAsync(order.UserId);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            await TrySendConfirmationEmailAsync(order, paidAt);
            return PaymentResult.Processed;
        }

        await transaction.RollbackAsync();
        await FlagPaymentReviewAsync(orderId, paymentIntentId, eventId, order.OrderNumber);
        return PaymentResult.StockConflict;
    }

    private bool IsOrderPayable(Order order, string paymentIntentId)
    {
        if (order.Status == "Paid" && order.StripePaymentIntentId != paymentIntentId)
            return false;

        if (order.Status == "Cancelled" || order.Status == ShippedStatus || order.Status == "Delivered")
        {
            _logger.LogError(
                "Stripe event for payment {PaymentIntent} arrived for order {OrderNumber} in status {Status}. Refund may be needed.",
                paymentIntentId, order.OrderNumber, order.Status);
            return false;
        }

        return true;
    }

    private async Task<bool> TryDecrementStockAsync(List<OrderItem> orderItems)
    {
        foreach (var item in orderItems.OrderBy(i => i.ProductId))
        {
            var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                "UPDATE \"Products\" SET \"Stock\" = \"Stock\" - {0} WHERE \"Id\" = {1} AND \"Stock\" >= {0}",
                item.Quantity, item.ProductId);

            if (rowsAffected == 0)
                return false;
        }

        return true;
    }

    private async Task ClearCartAsync(Guid userId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart != null)
        {
            _context.CartItems.RemoveRange(cart.Items);
        }
    }

    private async Task TrySendConfirmationEmailAsync(Order order, DateTime paidAt)
    {
        var user = await _context.Users.FindAsync(order.UserId);
        if (user != null)
        {
            try
            {
                await _emailService.SendOrderConfirmationEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    order.OrderNumber,
                    order.Total,
                    paidAt.ToString(DateFormat)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order confirmation email for {OrderNumber}", order.OrderNumber);
            }
        }
    }

    private async Task FlagPaymentReviewAsync(Guid orderId, string paymentIntentId, string eventId, string orderNumber)
    {
        await using var reviewTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (!await _eventRepository.TryMarkProcessedAsync(eventId))
                return;

            await _orderRepository.MarkPaymentReviewAsync(orderId, paymentIntentId);
            await reviewTransaction.CommitAsync();

            _logger.LogError(
                "Order {OrderNumber} paid ({PaymentIntent}) but stock is insufficient. Flagged PaymentReview: refund or restock.",
                orderNumber, paymentIntentId);
        }
        catch
        {
            await reviewTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> ProcessCashOnDeliveryAsync(Guid orderId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null || order.Status != "Pending")
                return false;

            order.Status = "Paid"; // Mark as paid for COD
            order.PaidAt = DateTime.UtcNow;

            // Get order items for stock reduction
            var orderItems = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();

            // Atomic stock reduction - use SQL UPDATE to prevent overselling
            foreach (var item in orderItems)
            {
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Products\" SET \"Stock\" = \"Stock\" - {0} WHERE \"Id\" = {1} AND \"Stock\" >= {0}",
                    item.Quantity, item.ProductId);

                if (rowsAffected == 0)
                {
                    // Insufficient stock - rollback transaction and fail
                    throw new InvalidOperationException($"Insufficient stock for product {item.ProductId}. Order cannot be fulfilled.");
                }
            }

            // Clear cart
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == order.UserId);
            if (cart != null)
            {
                _context.CartItems.RemoveRange(cart.Items);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Send order confirmation email for COD orders
            // Wrapped in try/catch to prevent failure on email issues
            var user = await _context.Users.FindAsync(order.UserId);
            if (user != null)
            {
                try
                {
                    await _emailService.SendOrderConfirmationEmailAsync(
                        user.Email,
                        $"{user.FirstName} {user.LastName}",
                        order.OrderNumber,
                        order.Total,
                        order.PaidAt?.ToString(DateFormat) ?? DateTime.UtcNow.ToString(DateFormat)
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send COD order confirmation email for {order.OrderNumber}: {ex.Message}");
                }
            }

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> CancelOrderAsync(Guid userId, Guid orderId)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null)
            return false;

        // Can only cancel if not shipped
        if (order.Status == ShippedStatus || order.Status == "Delivered")
            return false;

        order.Status = "Cancelled";
        order.CancelledAt = DateTime.UtcNow;

        // Restore stock
        var orderItems = await _context.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .ToListAsync();

        foreach (var item in orderItems)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                product.Stock += item.Quantity;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<OrderDto>> GetAllOrdersAsync()
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var result = new List<OrderDto>();
        foreach (var order in orders)
        {
            var shippingAddress = await _context.Addresses
                .FirstOrDefaultAsync(a => a.UserId == order.UserId && a.Type == "Shipping" && a.IsDefault);

            var billingAddress = await _context.Addresses
                .FirstOrDefaultAsync(a => a.UserId == order.UserId && a.Type == "Billing" && a.IsDefault);

            var user = await _context.Users.FindAsync(order.UserId);

            result.Add(new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                UserEmail = user?.Email,
                OrderNumber = order.OrderNumber,
                Status = Enum.Parse<OrderStatus>(order.Status),
                PaymentMethod = Enum.Parse<PaymentMethod>(order.PaymentMethod),
                Subtotal = order.Subtotal,
                Tax = order.Tax,
                Shipping = order.ShippingCost,
                Total = order.Total,
                StripePaymentIntentId = order.StripePaymentIntentId,
                StripeEventId = order.StripeEventId,
                CreatedAt = order.CreatedAt,
                PaidAt = order.PaidAt,
                ShippedAt = order.ShippedAt,
                DeliveredAt = order.DeliveredAt,
                CancelledAt = order.CancelledAt,
                TrackingNumber = order.TrackingNumber,
                ShippingAddress = shippingAddress != null ? new AddressDto
                {
                    Id = shippingAddress.Id,
                    UserId = shippingAddress.UserId,
                    Type = shippingAddress.Type,
                    FirstName = shippingAddress.FirstName,
                    LastName = shippingAddress.LastName,
                    AddressLine1 = shippingAddress.AddressLine1,
                    AddressLine2 = shippingAddress.AddressLine2,
                    City = shippingAddress.City,
                    State = shippingAddress.State,
                    PostalCode = shippingAddress.PostalCode,
                    Country = shippingAddress.Country,
                    Phone = shippingAddress.Phone,
                    IsDefault = shippingAddress.IsDefault,
                    CreatedAt = shippingAddress.CreatedAt
                } : null,
                BillingAddress = billingAddress != null ? new AddressDto
                {
                    Id = billingAddress.Id,
                    UserId = billingAddress.UserId,
                    Type = billingAddress.Type,
                    FirstName = billingAddress.FirstName,
                    LastName = billingAddress.LastName,
                    AddressLine1 = billingAddress.AddressLine1,
                    AddressLine2 = billingAddress.AddressLine2,
                    City = billingAddress.City,
                    State = billingAddress.State,
                    PostalCode = billingAddress.PostalCode,
                    Country = billingAddress.Country,
                    Phone = billingAddress.Phone,
                    IsDefault = billingAddress.IsDefault,
                    CreatedAt = billingAddress.CreatedAt
                } : null,
                Items = order.Items.Select(i => new OrderItemDto
                {
                    Id = i.Id,
                    OrderId = i.OrderId,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    ProductImage = i.ProductImageUrl,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Total = i.TotalPrice
                }).ToList()
            });
        }

        return result;
    }

    public async Task<bool> UpdateOrderStatusAsync(Guid orderId, string status)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            return false;

        order.Status = status;

        if (status == "Shipped")
        {
            order.ShippedAt = DateTime.UtcNow;
        }
        else if (status == "Delivered")
        {
            order.DeliveredAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Send shipping notification if status changed to Shipped
        if (status == "Shipped" && !string.IsNullOrEmpty(order.TrackingNumber))
        {
            var user = await _context.Users.FindAsync(order.UserId);
            if (user != null)
            {
                await _emailService.SendShippedNotificationEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    order.OrderNumber,
                    order.TrackingNumber
                );
            }
        }

        return true;
    }

    public async Task<bool> UpdateOrderTrackingAsync(Guid orderId, string trackingNumber)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            return false;

        order.TrackingNumber = trackingNumber;
        await _context.SaveChangesAsync();

        // Send shipping notification if order is already shipped
        if (order.Status == ShippedStatus)
        {
            var user = await _context.Users.FindAsync(order.UserId);
            if (user != null)
            {
                await _emailService.SendShippedNotificationEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    order.OrderNumber,
                    trackingNumber
                );
            }
        }

        return true;
    }
}
