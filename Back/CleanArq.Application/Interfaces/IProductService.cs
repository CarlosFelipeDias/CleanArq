using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CleanArq.Application.DTOs;
using CleanArq.Domain.Common;

namespace CleanArq.Application.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDTO>> GetProductsAsync();
        Task<ProductDTO?> GetProductByIdAsync(int? id);
        Task<ProductDTO?> GetProductWithCategoryAsync(int? id); 
        Task<Result<ProductDTO>> AddProductAsync(ProductDTO productDto);
        Task<Result> UpdateProductAsync(ProductDTO productDto);
        Task<Result> DeleteProductAsync(int? id);
    }
}
