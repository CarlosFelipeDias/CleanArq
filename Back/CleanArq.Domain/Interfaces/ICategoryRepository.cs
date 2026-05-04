using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CleanArq.Domain.Entities;

namespace CleanArq.Domain.Interfaces
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>> GetCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int? id);
        Task AddCategoryAsync(Category category);
        Task UpdateCategoryAsync(Category category);
        Task DeleteCategoryAsync(int id);
    }
}