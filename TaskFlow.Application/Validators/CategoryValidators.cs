using FluentValidation;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Validators;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required")
            .MaximumLength(255).WithMessage("Category name cannot exceed 255 characters");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Category slug is required")
            .MaximumLength(255).WithMessage("Category slug cannot exceed 255 characters")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug can only contain lowercase letters, numbers, and hyphens");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Category description cannot exceed 500 characters");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(255).WithMessage("Image URL cannot exceed 255 characters")
            .Must(uri => string.IsNullOrEmpty(uri) || Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Image URL must be a valid URL if provided");
    }
}

public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required")
            .MaximumLength(255).WithMessage("Category name cannot exceed 255 characters");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Category slug is required")
            .MaximumLength(255).WithMessage("Category slug cannot exceed 255 characters")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug can only contain lowercase letters, numbers, and hyphens");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Category description cannot exceed 500 characters");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(255).WithMessage("Image URL cannot exceed 255 characters")
            .Must(uri => string.IsNullOrEmpty(uri) || Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Image URL must be a valid URL if provided");
    }
}
