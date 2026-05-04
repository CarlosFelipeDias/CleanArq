using CleanArq.Domain.Common;
using CleanArq.Domain.Validation;

namespace CleanArq.Domain.Entities;

public sealed class Category : Entity
{
    public string Name { get; private set; } = string.Empty;
    public ICollection<Product> Products { get;  set; } = new List<Product>();

    /// <summary>
    /// Parameterless constructor for EF Core materialization
    /// </summary>
    private Category()
    {
    }

    /// <summary>
    /// Private constructor for new categories (to be called by Create factory method)
    /// </summary>
    private Category(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Private constructor for existing categories (from database)
    /// </summary>
    private Category(int id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Factory method to create a new category with validation
    /// </summary>
    public static Result<Category> Create(string name)
    {
        var validator = new CategoryValidator();
        var tempCategory = new Category(name);

        var validationResult = validator.Validate(tempCategory);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new Notification(e.PropertyName, e.ErrorMessage))
                .ToList();

            return Result.Failure<Category>(errors);
        }

        return Result.Success(tempCategory);
    }

    /// <summary>
    /// Factory method to recreate an existing category (from database)
    /// </summary>
    public static Result<Category> Create(int id, string name)
    {
        var validator = new CategoryValidator();
        var category = new Category(id, name);

        var validationResult = validator.Validate(category);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new Notification(e.PropertyName, e.ErrorMessage))
                .ToList();

            return Result.Failure<Category>(errors);
        }

        return Result.Success(category);
    }

    /// <summary>
    /// Updates the category name with validation
    /// </summary>
    public Result UpdateName(string newName)
    {
        var validator = new CategoryValidator();
        var tempCategory = new Category(Id, newName);

        var validationResult = validator.Validate(tempCategory);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new Notification(e.PropertyName, e.ErrorMessage))
                .ToList();

            return Result.Failure(errors);
        }

        Name = newName;
        return Result.Success();
    }
}
