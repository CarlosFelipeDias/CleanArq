namespace CleanArq.Domain.Notifications;

/// <summary>
/// Example of how to use Result/Notification with domain entities
/// 
/// NOTE: This file is for documentation/example purposes only.
/// Delete it if you don't need it or move it to a Tests folder
/// </summary>
public static class UsageExample
{
    /*
     * EXAMPLE 1: Creating a new category with validation
     * ====================================================
     * 
     * var result = Category.Create("Electronics");
     * 
     * if (result.IsFailure)
     * {
     *     foreach (var error in result.Errors)
     *     {
     *         Console.WriteLine(error.ToString()); // Output: "Name: Category name is required"
     *     }
     *     return;
     * }
     * 
     * var category = result.Value;
     * // Now you have a validated category!
     * 
     * ====================================================
     */

    /*
     * EXAMPLE 2: Creating a new product with validation
     * ====================================================
     * 
     * var result = Product.Create(
     *     name: "Dell Notebook",
     *     description: "Excellent notebook for development",
     *     price: 3500.00m,
     *     stock: 10,
     *     imageUrl: "https://example.com/notebook.jpg",
     *     categoryId: 1
     * );
     * 
     * if (result.IsFailure)
     * {
     *     foreach (var error in result.Errors)
     *     {
     *         Console.WriteLine($"{error.PropertyName}: {error.Message}");
     *     }
     *     return;
     * }
     * 
     * var product = result.Value;
     * // Save to database...
     * 
     * ====================================================
     */

    /*
     * EXAMPLE 3: Updating an entity with validation
     * ====================================================
     * 
     * var updateResult = product.Update(
     *     name: "Dell Notebook Pro",
     *     description: "Excellent notebook for development and daily work",
     *     price: 2999.99m,
     *     stock: 8,
     *     imageUrl: "https://example.com/notebook-pro.jpg",
     *     categoryId: 1
     * );
     * 
     * if (updateResult.IsFailure)
     * {
     *     // Handle error
     *     foreach (var error in updateResult.Errors)
     *     {
     *         Console.WriteLine(error.Message);
     *     }
     * }
     * else
     * {
     *     // Price updated successfully!
     *     await repository.Update(product);
     * }
     * 
     * ====================================================
     */

    /*
     * EXAMPLE 4: Composing results in use cases
     * ====================================================
     * 
     * // UseCase example
     * public class CreateProductUseCase
     * {
     *     private readonly IProductRepository _repository;
     *     
     *     public async Task<Result<ProductDTO>> Execute(CreateProductRequest request)
     *     {
     *         // Validate and create the product
     *         var productResult = Product.Create(
     *             request.Name,
     *             request.Description,
     *             request.Price,
     *             request.Stock,
     *             request.ImageUrl,
     *             request.CategoryId
     *         );
     *         
     *         if (productResult.IsFailure)
     *             return Result.Failure<ProductDTO>(productResult.Errors);
     *         
     *         // If we got here, the product is valid
     *         var product = productResult.Value;
     *         
     *         await _repository.Add(product);
     *         await _repository.SaveChanges();
     *         
     *         return Result.Success(new ProductDTO(product));
     *     }
     * }
     * 
     * // UseCase in action
     * var response = await useCase.Execute(request);
     * 
     * if (response.IsSuccess)
     * {
     *     return Ok(response.Value);
     * }
     * 
     * return BadRequest(response.Errors);
     * 
     * ====================================================
     */
}
