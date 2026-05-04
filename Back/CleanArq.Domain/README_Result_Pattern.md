# Result/Notification Pattern - CleanArq.Domain

## 📋 Overview

This implementation uses the **Result/Notification** pattern with **FluentValidation** for domain validations, avoiding the use of exceptions for control flow and creating a robust **Rich Domain Model**.

## 🎯 Main Components

### 1. **Result.cs**
- Encapsulates the result of an operation (success or failure)
- Generic: `Result<T>` for operations that return a value
- Non-generic: `Result` for operations without return

**Properties:**
- `IsSuccess`: Indicates if the operation was successful
- `IsFailure`: Inverse of `IsSuccess`
- `Errors`: List of `Notification` with errors
- `Value`: The returned value (only in `Result<T>`)

### 2. **Notification.cs**
- Represents an error or warning within the domain
- Contains:
  - `PropertyName`: Which property generated the error
  - `Message`: Descriptive error message

### 3. **Validators (FluentValidation)**
- `ProductValidator`: Validation rules for Product
- `CategoryValidator`: Validation rules for Category

### 4. **Enriched Entities**
- **Private Constructors**: Force the use of factory methods
- **Factory Methods**: `Create()` with built-in validation
- **Update Methods**: Validate before updating properties

## ✅ Benefits

| Aspect | Advantage |
|--------|-----------|
| **Explicitness** | `Result<T>` return makes it clear it can fail |
| **Performance** | No exception overhead |
| **Composition** | Easy to compose multiple validations |
| **Testing** | Simple and straightforward tests |
| **Rich Domain** | Domain is always valid |
| **Error Handling** | Deterministic and predictable |

## 🚀 How to Use

### Create an Entity with Validation

```csharp
// Create new category
var categoryResult = Category.Create("Electronics");

if (categoryResult.IsFailure)
{
    foreach (var error in categoryResult.Errors)
    {
        Console.WriteLine(error.Message);
    }
    return;
}

var category = categoryResult.Value;
// Use the category (guaranteed to be valid)
```

### Create a Product with Validation

```csharp
var productResult = Product.Create(
    name: "Notebook",
    description: "Powerful notebook for development",
    price: 3500.00m,
    stock: 5,
    imageUrl: "https://example.com/notebook.jpg",
    categoryId: 1
);

if (productResult.IsSuccess)
{
    var product = productResult.Value;
    await repository.Add(product);
}
else
{
    // Multiple errors can be present
    var errors = productResult.Errors;
}
```

### Update an Entity

```csharp
var updateResult = product.Update(
    name: "Notebook Pro",
    description: "Powerful notebook for development and daily work",
    price: 2999.99m,
    stock: 4,
    imageUrl: "https://example.com/notebook-pro.jpg",
    categoryId: 1
);

if (updateResult.IsFailure)
{
    // Handle errors
    foreach (var error in updateResult.Errors)
    {
        Console.WriteLine(error.ToString());
    }
}
else
{
    await repository.Update(product);
}
```

### Use Cases with Composition

```csharp
public class CreateProductUseCase
{
    public async Task<Result<ProductDTO>> Execute(CreateProductRequest request)
    {
        // Validate and create
        var productResult = Product.Create(
            request.Name,
            request.Description,
            request.Price,
            request.Stock,
            request.ImageUrl,
            request.CategoryId
        );

        // Propagate failures
        if (productResult.IsFailure)
            return Result.Failure<ProductDTO>(productResult.Errors);

        var product = productResult.Value;
        await _repository.Add(product);
        
        return Result.Success(new ProductDTO(...));
    }
}
```

## 📁 Folder Structure

```
CleanArq.Domain/
├── Common/
│   ├── Result.cs           # Result/Result<T> class
│   └── Notification.cs     # Error notification class
├── Entities/
│   ├── Category.cs         # Entity with factory methods
│   └── Product.cs          # Entity with factory methods
└── Validation/
    ├── CategoryValidator.cs # FluentValidation validator
    └── ProductValidator.cs # FluentValidation validator
```

## 🔄 Validation Flow

```
1. Call to Create() or Update()
   ↓
2. Create temporary entity instance
   ↓
3. Execute validator (FluentValidation)
   ↓
4. If invalid → Return Result.Failure with Notifications
   ↓
5. If valid → Return Result.Success with entity
   ↓
6. Caller decides what to do (propagate error, log, etc)
```

## 💡 Next Steps

For the **Application Layer**, you can:

1. **Create DTOs** to represent data externally
2. **Use Cases** that orchestrate domain operations
3. **Mappers** that convert Domain → DTO
4. **Handlers** that translate `Result` to HTTP responses

## ❌ Anti-Patterns Avoided

- ❌ ~~Throw exceptions for validations~~
- ❌ ~~Invalid entities in memory~~
- ❌ ~~Validation spread throughout the application~~
- ✅ Validation centralized in the domain
- ✅ Explicit return of failures
- ✅ Domain always in valid state

