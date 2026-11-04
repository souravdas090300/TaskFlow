using Microsoft.Extensions.Configuration;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Repositories;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Services;

public class CartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IConfiguration _configuration;

    public CartService(ICartRepository cartRepository, IProductRepository productRepository, IConfiguration configuration)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _configuration = configuration;
    }

    public async Task<CartDto> GetCartAsync(Guid userId)
    {
        var cart = await _cartRepository.GetByUserIdWithItemsAsync(userId);

        if (cart == null)
        {
            cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _cartRepository.AddAsync(cart);
        }

        return MapToCartDto(cart);
    }

    public async Task<CartDto> AddToCartAsync(Guid userId, AddToCartDto dto)
    {
        var cart = await _cartRepository.GetByUserIdWithItemsAsync(userId);

        if (cart == null)
        {
            cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _cartRepository.AddAsync(cart);
        }

        var product = await _productRepository.GetByIdAsync(dto.ProductId);
        if (product == null)
        {
            throw new InvalidOperationException("Product not found");
        }

        var availableStock = product.Stock;
        if (availableStock < dto.Quantity)
        {
            throw new InvalidOperationException("Insufficient stock");
        }

        var existingItem = cart.Items.FirstOrDefault(i =>
            i.ProductId == dto.ProductId &&
            i.ProductVariantId == dto.ProductVariantId);

        if (existingItem != null)
        {
            existingItem.Quantity += dto.Quantity;
            existingItem.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var cartItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = dto.ProductId,
                ProductVariantId = dto.ProductVariantId,
                Quantity = dto.Quantity,
                UnitPrice = product.Price,
                CreatedAt = DateTime.UtcNow
            };
            cart.Items.Add(cartItem);
        }

        await _cartRepository.UpdateAsync(cart);
        return await GetCartAsync(userId);
    }

    public async Task<CartDto> UpdateCartItemAsync(Guid userId, Guid cartItemId, UpdateCartItemDto dto)
    {
        var cart = await _cartRepository.GetByUserIdWithItemsAsync(userId);

        if (cart == null)
        {
            throw new InvalidOperationException("Cart not found");
        }

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item == null)
        {
            throw new InvalidOperationException("Cart item not found");
        }

        var product = await _productRepository.GetByIdAsync(item.ProductId);
        var availableStock = product?.Stock ?? 0;

        if (availableStock < dto.Quantity)
        {
            throw new InvalidOperationException("Insufficient stock");
        }

        item.Quantity = dto.Quantity;
        item.UpdatedAt = DateTime.UtcNow;

        await _cartRepository.UpdateAsync(cart);
        return await GetCartAsync(userId);
    }

    public async Task<CartDto> RemoveFromCartAsync(Guid userId, Guid cartItemId)
    {
        var cart = await _cartRepository.GetByUserIdWithItemsAsync(userId);

        if (cart == null)
        {
            throw new InvalidOperationException("Cart not found");
        }

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item == null)
        {
            throw new InvalidOperationException("Cart item not found");
        }

        cart.Items.Remove(item);
        await _cartRepository.UpdateAsync(cart);
        return await GetCartAsync(userId);
    }

    public async Task ClearCartAsync(Guid userId)
    {
        await _cartRepository.ClearCartItemsAsync(userId);
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
        var taxRate = _configuration.GetValue<decimal>("OrderSettings:TaxRate");
        var tax = subtotal * taxRate;
        var freeShippingThreshold = _configuration.GetValue<decimal>("OrderSettings:FreeShippingThreshold");
        var shippingCost = _configuration.GetValue<decimal>("OrderSettings:ShippingCost");
        var shipping = subtotal > 0 ? (subtotal >= freeShippingThreshold ? 0 : shippingCost) : 0;

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
