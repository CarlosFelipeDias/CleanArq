using FluentValidation;
using CleanArq.Domain.Entities;

namespace CleanArq.Domain.Validation;

public sealed class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty()
            .WithMessage("Product name is required")
            .MinimumLength(3)
            .WithMessage("Product name must have at least 3 characters")
            .MaximumLength(100)
            .WithMessage("Product name must have a maximum of 100 characters");

        RuleFor(p => p.Description)
            .NotEmpty()
            .WithMessage("Product description is required")
            .MinimumLength(10)
            .WithMessage("Product description must have at least 10 characters")
            .MaximumLength(500)
            .WithMessage("Product description must have a maximum of 500 characters");

        RuleFor(p => p.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than zero");

        RuleFor(p => p.Stock)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Stock cannot be negative");

        RuleFor(p => p.ImageUrl)
            .MaximumLength(220)
            .WithMessage("Image URL must have a maximum of 220 characters")
            .Must(BeValidUrl)
            .WithMessage("Image URL must be valid")
            .When(p => !string.IsNullOrWhiteSpace(p.ImageUrl));

        RuleFor(p => p.CategoryId)
            .GreaterThan(0)
            .WithMessage("A valid category must be selected")
            .OverridePropertyName(nameof(Product.CategoryId));

        RuleFor(p => p.Id)
            .GreaterThan(0)
            .WithMessage("Product id must be greater than zero")
            .When(p => p.Id != 0);
    }

    private static bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}
