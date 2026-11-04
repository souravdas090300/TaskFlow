using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure;

namespace TaskFlow.API.Services;

public class CartService
{
    private readonly ApplicationDbContext _context;

    public CartService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CartDto> GetCartAsync(Guid userId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .Include(c => c.Items)
                .ThenInclude(i => i.ProductVariant)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            // Create new cart
            cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
        }

        return MapToCartDto(cart);
    }

    public async Task<CartDto> AddToCartAsync(Guid userId, AddToCartDto dto)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Carts.Add(cart);
        }

        // Get product
        var product = await _context.Products.FindAsync(dto.ProductId);
        if (product == null)
        {
            throw new InvalidOperationException("Product not found");
        }

        // Get variant if specified
        ProductVariant? variant = null;
        if (dto.ProductVariantId.HasValue)
        {
            variant = await _context.ProductVariants.FindAsync(dto.ProductVariantId.Value);
            if (variant == null)
            {
                throw new InvalidOperationException("Product variant not found");
            }
        }

        // Check stock
        var availableStock = variant?.Stock ?? product.Stock;
        if (availableStock < dto.Quantity)
        {
            throw new InvalidOperationException("Insufficient stock");
        }

        // Check if item already exists in cart
        var existingItem = cart.Items.FirstOrDefault(i =>
            i.ProductId == dto.ProductId &&
            i.ProductVariantId == dto.ProductVariantId);

        if (existingItem != null)
        {
            // Update quantity
            existingItem.Quantity += dto.Quantity;
            existingItem.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Add new item
            var price = variant?.Price ?? product.Price;
            var cartItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = dto.ProductId,
                ProductVariantId = dto.ProductVariantId,
                Quantity = dto.Quantity,
                UnitPrice = price,
                CreatedAt = DateTime.UtcNow
            };
            _context.CartItems.Add(cartItem);
        }

        await _context.SaveChangesAsync();

        return await GetCartAsync(userId);
    }

    public async Task<CartDto> UpdateCartItemAsync(Guid userId, Guid cartItemId, UpdateCartItemDto dto)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            throw new InvalidOperationException("Cart not found");
        }

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item == null)
        {
            throw new InvalidOperationException("Cart item not found");
        }

        // Check stock
        var product = await _context.Products.FindAsync(item.ProductId);
        var variant = item.ProductVariantId.HasValue ? await _context.ProductVariants.FindAsync(item.ProductVariantId.Value) : null;
        var availableStock = variant?.Stock ?? product?.Stock ?? 0;

        if (availableStock < dto.Quantity)
        {
            throw new InvalidOperationException("Insufficient stock");
        }

        item.Quantity = dto.Quantity;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetCartAsync(userId);
    }

    public async Task<CartDto> RemoveFromCartAsync(Guid userId, Guid cartItemId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            throw new InvalidOperationException("Cart not found");
        }

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item == null)
        {
            throw new InvalidOperationException("Cart item not found");
        }

        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();

        return await GetCartAsync(userId);
    }

    public async Task ClearCartAsync(Guid userId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart != null)
        {
            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync();
        }
    }

    private CartDto MapToCartDto(Cart cart)
    {
        var items = cart.Items.Select(i => new CartItemDto
        {
            Id = i.Id,
            CartId = i.CartId,
            ProductId = i.ProductId,
            ProductName = i.Product.Name,
            ProductImageUrl = i.Product.Images.FirstOrDefault(img => img.IsPrimary)?.ImageUrl,
            ProductVariantId = i.ProductVariantId,
            VariantName = i.ProductVariant != null ? $"{i.ProductVariant.Size} / {i.ProductVariant.Color}" : null,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            TotalPrice = i.UnitPrice * i.Quantity
        }).ToList();

        var subtotal = items.Sum(i => i.TotalPrice);
        var tax = subtotal * 0.18m; // 18% VAT for Malta
        var shipping = subtotal > 0 ? (subtotal >= 50 ? 0 : 5.99m) : 0; // Free shipping over 50

        return new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            Items = items,
            Subtotal = subtotal,
            Tax = tax,
            Shipping = shipping,
            Total = subtotal + tax + shipping
        };
    }
}
