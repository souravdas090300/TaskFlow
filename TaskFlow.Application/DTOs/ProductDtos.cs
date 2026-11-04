using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Application.DTOs;

public class CreateProductDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    [Required]
    [Range(0, 100000)]
    public int Stock { get; set; }

    [MaxLength(255)]
    public string? Brand { get; set; }

    [MaxLength(100)]
    public string? SKU { get; set; }

    public bool IsFeatured { get; set; } = false;

    public bool IsNewArrival { get; set; } = false;

    [Required]
    public Guid CategoryId { get; set; }

    public List<CreateProductImageDto> Images { get; set; } = new();

    public List<CreateProductVariantDto> Variants { get; set; } = new();
}

public class CreateProductImageDto
{
    [Required]
    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? AltText { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public bool IsPrimary { get; set; } = false;
}

public class CreateProductVariantDto
{
    [MaxLength(100)]
    public string? Size { get; set; }

    [MaxLength(100)]
    public string? Color { get; set; }

    [MaxLength(50)]
    public string? ColorCode { get; set; }

    [Required]
    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    [Required]
    [Range(0, 100000)]
    public int Stock { get; set; }

    [MaxLength(100)]
    public string? SKU { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateProductDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    [Required]
    [Range(0, 100000)]
    public int Stock { get; set; }

    [MaxLength(255)]
    public string? Brand { get; set; }

    [MaxLength(100)]
    public string? SKU { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsNewArrival { get; set; }

    public bool IsActive { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    public List<CreateProductImageDto>? Images { get; set; }

    public List<CreateProductVariantDto>? Variants { get; set; }
}

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string Slug { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation
    public string? ParentCategoryName { get; set; }
    public List<CategoryDto> SubCategories { get; set; } = new();
    public int ProductCount { get; set; }
}

public class CreateCategoryDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(255)]
    public string? ImageUrl { get; set; }

    [Required]
    [MaxLength(255)]
    public string Slug { get; set; } = string.Empty;

    public Guid? ParentCategoryId { get; set; }

    public int DisplayOrder { get; set; } = 0;
}

public class UpdateCategoryDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(255)]
    public string? ImageUrl { get; set; }

    [Required]
    [MaxLength(255)]
    public string Slug { get; set; } = string.Empty;

    public Guid? ParentCategoryId { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }
}

public class ProductListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? Brand { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public bool IsNewArrival { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
