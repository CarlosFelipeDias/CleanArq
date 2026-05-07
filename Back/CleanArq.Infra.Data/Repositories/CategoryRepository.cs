using CleanArq.Domain.Entities;
using CleanArq.Domain.Interfaces;
using CleanArq.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace CleanArq.Infra.Data.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _categoryContext;

        public CategoryRepository(ApplicationDbContext categoryContext)
        {
            _categoryContext = categoryContext;
        }

        public async Task AddCategoryAsync(Category category)
        {
            await _categoryContext.Categories.AddAsync(category);
            await _categoryContext.SaveChangesAsync();
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await _categoryContext.Categories.FindAsync(id);

            if (category is null)
            {
                return;
            }

            _categoryContext.Remove(category);
            await _categoryContext.SaveChangesAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _categoryContext.Categories.FindAsync(id);
        }

        public async Task<IEnumerable<Category>> GetCategoriesAsync()
        {
            return await _categoryContext.Categories.ToListAsync();
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            _categoryContext.Update(category);
            await _categoryContext.SaveChangesAsync();
        }
    }
}
