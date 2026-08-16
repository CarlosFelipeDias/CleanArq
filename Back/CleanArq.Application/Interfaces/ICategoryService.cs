using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CleanArq.Application.DTOs;
using CleanArq.Domain.Common;

namespace CleanArq.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDTO>> GetCategoriesAsync();
        Task<CategoryDTO?> GetCategoryByIdAsync(int? id);
        Task<Result<CategoryDTO>> AddCategoryAsync(CategoryDTO categoryDto);
        Task<Result> UpdateCategoryAsync(CategoryDTO categoryDto);
        Task<Result> DeleteCategoryAsync(int? id);
    }
}
