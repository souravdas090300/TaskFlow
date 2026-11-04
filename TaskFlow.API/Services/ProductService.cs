using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure;

namespace TaskFlow.API.Services;

public class ProductService
{
    private readonly ApplicationDbContext _context;

    public ProductService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductListDto>> GetAllProductsAsync(
        int page = 1,
        int pageSize = 20,
        Guid? categoryId = null,
        string? search = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sortBy = null)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .Where(p => p.IsActive)
            .AsQueryable();

        // Filter by category
        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        // Search by name
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
        }

        // Filter by price range
        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice);
        }

        // Sorting
        query = sortBy switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            "name" => query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Price = p.Price,
                Stock = p.Stock,
                Brand = p.Brand,
                PrimaryImageUrl = p.Images.FirstOrDefault()!.ImageUrl,
                CategoryName = p.Category.Name,
                CategorySlug = p.Category.Slug,
                IsFeatured = p.IsFeatured,
                IsNewArrival = p.IsNewArrival,
                AverageRating = p.AverageRating,
                ReviewCount = p.ReviewCount
            })
            .ToListAsync();

        return products;
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
            .Include(p => p.Variants.Where(v => v.IsActive))
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return null;

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Slug = product.Slug,
            Price = product.Price,
            Stock = product.Stock,
            Brand = product.Brand,
            SKU = product.SKU,
            IsFeatured = product.IsFeatured,
            IsNewArrival = product.IsNewArrival,
            IsActive = product.IsActive,
            AverageRating = product.AverageRating,
            ReviewCount = product.ReviewCount,
            CreatedAt = product.CreatedAt,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            CategorySlug = product.Category.Slug,
            Images = product.Images.Select(i => new ProductImageDto
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl,
                AltText = i.AltText,
                DisplayOrder = i.DisplayOrder,
                IsPrimary = i.IsPrimary
            }).ToList(),
            Variants = product.Variants.Select(v => new ProductVariantDto
            {
                Id = v.Id,
                Size = v.Size,
                Color = v.Color,
                ColorCode = v.ColorCode,
                Price = v.Price,
                Stock = v.Stock,
                SKU = v.SKU,
                IsActive = v.IsActive
            }).ToList()
        };
    }

    public async Task<ProductDto?> GetProductBySlugAsync(string slug)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
            .Include(p => p.Variants.Where(v => v.IsActive))
            .FirstOrDefaultAsync(p => p.Slug == slug);

        if (product == null) return null;

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Slug = product.Slug,
            Price = product.Price,
            Stock = product.Stock,
            Brand = product.Brand,
            SKU = product.SKU,
            IsFeatured = product.IsFeatured,
            IsNewArrival = product.IsNewArrival,
            IsActive = product.IsActive,
            AverageRating = product.AverageRating,
            ReviewCount = product.ReviewCount,
            CreatedAt = product.CreatedAt,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            CategorySlug = product.Category.Slug,
            Images = product.Images.Select(i => new ProductImageDto
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl,
                AltText = i.AltText,
                DisplayOrder = i.DisplayOrder,
                IsPrimary = i.IsPrimary
            }).ToList(),
            Variants = product.Variants.Select(v => new ProductVariantDto
            {
                Id = v.Id,
                Size = v.Size,
                Color = v.Color,
                ColorCode = v.ColorCode,
                Price = v.Price,
                Stock = v.Stock,
                SKU = v.SKU,
                IsActive = v.IsActive
            }).ToList()
        };
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            Slug = dto.Slug,
            Price = dto.Price,
            Stock = dto.Stock,
            Brand = dto.Brand,
            SKU = dto.SKU,
            IsFeatured = dto.IsFeatured,
            IsNewArrival = dto.IsNewArrival,
            IsActive = true,
            CategoryId = dto.CategoryId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);

        // Add images
        foreach (var imageDto in dto.Images)
        {
            var image = new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ImageUrl = imageDto.ImageUrl,
                AltText = imageDto.AltText,
                DisplayOrder = imageDto.DisplayOrder,
                IsPrimary = imageDto.IsPrimary,
                CreatedAt = DateTime.UtcNow
            };
            _context.ProductImages.Add(image);
        }

        // Add variants
        foreach (var variantDto in dto.Variants)
        {
            var variant = new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Size = variantDto.Size,
                Color = variantDto.Color,
                ColorCode = variantDto.ColorCode,
                Price = variantDto.Price,
                Stock = variantDto.Stock,
                SKU = variantDto.SKU,
                IsActive = variantDto.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            _context.ProductVariants.Add(variant);
        }

        await _context.SaveChangesAsync();

        return await GetProductByIdAsync(product.Id);
    }

    public async Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductDto dto)
    {
        var product = await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return null;

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Slug = dto.Slug;
        product.Price = dto.Price;
        product.Stock = dto.Stock;
        product.Brand = dto.Brand;
        product.SKU = dto.SKU;
        product.IsFeatured = dto.IsFeatured;
        product.IsNewArrival = dto.IsNewArrival;
        product.IsActive = dto.IsActive;
        product.CategoryId = dto.CategoryId;
        product.UpdatedAt = DateTime.UtcNow;

        // Update images
        if (dto.Images != null)
        {
            // Remove existing images
            var existingImages = _context.ProductImages.Where(i => i.ProductId == id);
            _context.ProductImages.RemoveRange(existingImages);

            // Add new images
            foreach (var imageDto in dto.Images)
            {
                var image = new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    ImageUrl = imageDto.ImageUrl,
                    AltText = imageDto.AltText,
                    DisplayOrder = imageDto.DisplayOrder,
                    IsPrimary = imageDto.IsPrimary,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ProductImages.Add(image);
            }
        }

        // Update variants
        if (dto.Variants != null)
        {
            // Remove existing variants
            var existingVariants = _context.ProductVariants.Where(v => v.ProductId == id);
            _context.ProductVariants.RemoveRange(existingVariants);

            // Add new variants
            foreach (var variantDto in dto.Variants)
            {
                var variant = new ProductVariant
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Size = variantDto.Size,
                    Color = variantDto.Color,
                    ColorCode = variantDto.ColorCode,
                    Price = variantDto.Price,
                    Stock = variantDto.Stock,
                    SKU = variantDto.SKU,
                    IsActive = variantDto.IsActive,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ProductVariants.Add(variant);
            }
        }

        await _context.SaveChangesAsync();

        return await GetProductByIdAsync(product.Id);
    }

    public async Task<bool> DeleteProductAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ProductListDto>> GetFeaturedProductsAsync(int limit = 10)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .Where(p => p.IsActive && p.IsFeatured)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Price = p.Price,
                Stock = p.Stock,
                Brand = p.Brand,
                PrimaryImageUrl = p.Images.FirstOrDefault()!.ImageUrl,
                CategoryName = p.Category.Name,
                CategorySlug = p.Category.Slug,
                IsFeatured = p.IsFeatured,
                IsNewArrival = p.IsNewArrival,
                AverageRating = p.AverageRating,
                ReviewCount = p.ReviewCount
            })
            .ToListAsync();
    }

    public async Task<List<ProductListDto>> GetNewArrivalsAsync(int limit = 10)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .Where(p => p.IsActive && p.IsNewArrival)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Price = p.Price,
                Stock = p.Stock,
                Brand = p.Brand,
                PrimaryImageUrl = p.Images.FirstOrDefault()!.ImageUrl,
                CategoryName = p.Category.Name,
                CategorySlug = p.Category.Slug,
                IsFeatured = p.IsFeatured,
                IsNewArrival = p.IsNewArrival,
                AverageRating = p.AverageRating,
                ReviewCount = p.ReviewCount
            })
            .ToListAsync();
    }
}
