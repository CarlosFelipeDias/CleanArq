using FluentValidation;
using CleanArq.Domain.Entities;

namespace CleanArq.Domain.Validation;

public sealed class CategoryValidator : AbstractValidator<Category>
{
    public CategoryValidator()
    {
        // Only validate Id if it's not 0 (new categories have Id = 0 until saved to database)
        RuleFor(c => c.Id)
            .GreaterThan(0)
            .WithMessage("Category id must be greater than zero")
            .When(c => c.Id != 0);

        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage("Category name is required")
            .MinimumLength(3)
            .WithMessage("Category name must have at least 3 characters")
            .MaximumLength(100)
            .WithMessage("Category name must have a maximum of 100 characters");
    }
}
