using FluentValidation;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Validators;

public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(255).WithMessage("Product name cannot exceed 255 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Product description is required")
            .MaximumLength(500).WithMessage("Product description cannot exceed 500 characters");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Product slug is required")
            .MaximumLength(255).WithMessage("Product slug cannot exceed 255 characters")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug can only contain lowercase letters, numbers, and hyphens");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0")
            .LessThanOrEqualTo(1000000).WithMessage("Price cannot exceed 1,000,000");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Stock cannot be negative")
            .LessThanOrEqualTo(100000).WithMessage("Stock cannot exceed 100,000");

        RuleFor(x => x.Brand)
            .MaximumLength(255).WithMessage("Brand cannot exceed 255 characters");

        RuleFor(x => x.SKU)
            .MaximumLength(100).WithMessage("SKU cannot exceed 100 characters");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required");

        RuleForEach(x => x.Images).SetValidator(new CreateProductImageValidator());
        RuleForEach(x => x.Variants).SetValidator(new CreateProductVariantValidator());
    }
}

public class CreateProductImageValidator : AbstractValidator<CreateProductImageDto>
{
    public CreateProductImageValidator()
    {
        RuleFor(x => x.ImageUrl)
            .NotEmpty().WithMessage("Image URL is required")
            .MaximumLength(500).WithMessage("Image URL cannot exceed 500 characters")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _)).WithMessage("Image URL must be a valid URL");

        RuleFor(x => x.AltText)
            .MaximumLength(255).WithMessage("Alt text cannot exceed 255 characters");
    }
}

public class CreateProductVariantValidator : AbstractValidator<CreateProductVariantDto>
{
    public CreateProductVariantValidator()
    {
        RuleFor(x => x.Size)
            .MaximumLength(100).WithMessage("Size cannot exceed 100 characters");

        RuleFor(x => x.Color)
            .MaximumLength(100).WithMessage("Color cannot exceed 100 characters");

        RuleFor(x => x.ColorCode)
            .MaximumLength(50).WithMessage("Color code cannot exceed 50 characters")
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Color code must be a valid hex color (e.g., #FF0000)");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Variant price must be greater than 0")
            .LessThanOrEqualTo(1000000).WithMessage("Variant price cannot exceed 1,000,000");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Variant stock cannot be negative")
            .LessThanOrEqualTo(100000).WithMessage("Variant stock cannot exceed 100,000");

        RuleFor(x => x.SKU)
            .MaximumLength(100).WithMessage("Variant SKU cannot exceed 100 characters");
    }
}

public class UpdateProductValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(255).WithMessage("Product name cannot exceed 255 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Product description is required")
            .MaximumLength(500).WithMessage("Product description cannot exceed 500 characters");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Product slug is required")
            .MaximumLength(255).WithMessage("Product slug cannot exceed 255 characters")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug can only contain lowercase letters, numbers, and hyphens");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0")
            .LessThanOrEqualTo(1000000).WithMessage("Price cannot exceed 1,000,000");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Stock cannot be negative")
            .LessThanOrEqualTo(100000).WithMessage("Stock cannot exceed 100,000");

        RuleFor(x => x.Brand)
            .MaximumLength(255).WithMessage("Brand cannot exceed 255 characters");

        RuleFor(x => x.SKU)
            .MaximumLength(100).WithMessage("SKU cannot exceed 100 characters");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required");

        When(x => x.Images != null, () =>
        {
            RuleForEach(x => x.Images).SetValidator(new CreateProductImageValidator());
        });

        When(x => x.Variants != null, () =>
        {
            RuleForEach(x => x.Variants).SetValidator(new CreateProductVariantValidator());
        });
    }
}
