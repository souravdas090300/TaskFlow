using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TaskFlow.Application;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Persistence;
using TaskFlow.Application.Repositories;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Services;

public class OrderService
{
    private const string ShippedStatus = "Shipped";

    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IApplicationDbContext context,
        IConfiguration configuration,
        IEmailService emailService,
        IPaymentGateway paymentGateway,
        ILogger<OrderService> logger)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
        _paymentGateway = paymentGateway;
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

    public async Task<bool> CancelOrderAsync(Guid userId, Guid orderId)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null)
            return false;

        // Can only cancel if not shipped
        if (order.Status == ShippedStatus || order.Status == "Delivered")
            return false;

        // Orders paid at Stripe (Paid or stuck in PaymentReview) must be refunded before cancelling
        var paidAtStripe = order.PaymentMethod == "Stripe" && !string.IsNullOrEmpty(order.StripePaymentIntentId)
            && order.Status is "Paid" or "PaymentReview";
        if (paidAtStripe)
        {
            var refund = await _paymentGateway.RefundPaymentAsync(order.StripePaymentIntentId!);
            if (!refund.Ok)
            {
                _logger.LogError(
                    "Failed to refund order {OrderNumber} (payment {PaymentIntent}): {Error}. Order left uncancelled.",
                    order.OrderNumber, order.StripePaymentIntentId, refund.Error);
                return false;
            }

            _logger.LogInformation(
                "Refunded order {OrderNumber}: refund {RefundId} for payment {PaymentIntent}",
                order.OrderNumber, refund.RefundId, order.StripePaymentIntentId);
        }

        // Stock was reserved for Paid/Processing orders; PaymentReview rolled the decrement back
        var restoreStock = order.Status is "Paid" or "Processing";

        order.Status = "Cancelled";
        order.CancelledAt = DateTime.UtcNow;

        if (restoreStock)
        {
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
        }

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Refunds a paid Stripe order in full and marks it Refunded.
    /// Stock is restored only when it was actually reserved (Paid/Processing),
    /// not for PaymentReview orders where the stock decrement was rolled back.
    /// </summary>
    public async Task<(bool Ok, string? Error)> RefundOrderAsync(Guid orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            return (false, "Order not found");

        if (order.PaymentMethod != "Stripe" || string.IsNullOrEmpty(order.StripePaymentIntentId))
            return (false, "Order has no Stripe payment to refund");

        if (order.Status is not ("Paid" or "Processing" or "PaymentReview"))
            return (false, $"Order in status '{order.Status}' cannot be refunded");

        var refund = await _paymentGateway.RefundPaymentAsync(order.StripePaymentIntentId);
        if (!refund.Ok)
        {
            _logger.LogError(
                "Stripe refund failed for order {OrderNumber} (payment {PaymentIntent}): {Error}",
                order.OrderNumber, order.StripePaymentIntentId, refund.Error);
            return (false, refund.Error ?? "Refund failed");
        }

        var restoreStock = order.Status is "Paid" or "Processing";
        order.Status = "Refunded";

        if (restoreStock)
        {
            var orderItems = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();

            foreach (var item in orderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                    product.Stock += item.Quantity;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Order {OrderNumber} refunded: refund {RefundId} for payment {PaymentIntent}",
            order.OrderNumber, refund.RefundId, order.StripePaymentIntentId);
        return (true, null);
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
