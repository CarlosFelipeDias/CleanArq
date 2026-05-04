using CleanArq.Domain.Common;
using CleanArq.Domain.Validation;

namespace CleanArq.Domain.Entities;

public sealed class Product : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public string ImageUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Navigation fields used by EF Core. These are not part of the product's core domain invariants.
    /// CategoryId remains part of the product invariants and must be valid.
    /// </summary>
    public int CategoryId { get; private set; }
    public Category? Category { get; private set; }

    /// <summary>
    /// Parameterless constructor for EF Core materialization
    /// </summary>
    private Product()
    {
    }

    /// <summary>
    /// Private constructor for new products (to be called by Create factory method)
    /// </summary>
    private Product(string name, string description, decimal price, int stock, string imageUrl, int categoryId)
    {
        Name = name;
        Description = description;
        Price = price;
        Stock = stock;
        ImageUrl = imageUrl;
        CategoryId = categoryId;
    }

    /// <summary>
    /// Private constructor for existing products (from database)
    /// </summary>
    private Product(int id, string name, string description, decimal price, int stock, string imageUrl, int categoryId)
    {
        Id = id;
        Name = name;
        Description = description;
        Price = price;
        Stock = stock;
        ImageUrl = imageUrl;
        CategoryId = categoryId;
    }

    /// <summary>
    /// Factory method to create a new product with validation
    /// </summary>
    public static Result<Product> Create(string name, string description, decimal price, int stock, string imageUrl, int categoryId)
    {
        var validator = new ProductValidator();
        var tempProduct = new Product(name, description, price, stock, imageUrl, categoryId);

        var validationResult = validator.Validate(tempProduct);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new Notification(e.PropertyName, e.ErrorMessage))
                .ToList();

            return Result.Failure<Product>(errors);
        }

        return Result.Success(tempProduct);
    }

    /// <summary>
    /// Factory method to recreate an existing product (from database)
    /// </summary>
    public static Result<Product> Create(int id, string name, string description, decimal price, int stock, string imageUrl, int categoryId)
    {
        var validator = new ProductValidator();
        var product = new Product(id, name, description, price, stock, imageUrl, categoryId);

        var validationResult = validator.Validate(product);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new Notification(e.PropertyName, e.ErrorMessage))
                .ToList();

            return Result.Failure<Product>(errors);
        }

        return Result.Success(product);
    }

    /// <summary>
    /// Updates the product with validation.
    /// This is the single global update method for product state changes.
    /// </summary>
    public Result Update(string name, string description, decimal price, int stock, string imageUrl, int categoryId)
    {
        var validator = new ProductValidator();
        var tempProduct = new Product(Id, name, description, price, stock, imageUrl, categoryId);

        var validationResult = validator.Validate(tempProduct);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new Notification(e.PropertyName, e.ErrorMessage))
                .ToList();

            return Result.Failure(errors);
        }

        Name = name;
        Description = description;
        Price = price;
        Stock = stock;
        ImageUrl = imageUrl;
        CategoryId = categoryId;
        Category = null;

        return Result.Success();
    }
}
