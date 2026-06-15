using CleanArq.Domain.Entities;
using CleanArq.Domain.Interfaces;
using CleanArq.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace CleanArq.Infra.Data.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _productContext;

        public ProductRepository(ApplicationDbContext productContext)
        {
            _productContext = productContext;
        }

        public async Task AddProductAsync(Product product)
        {
            await _productContext.Products.AddAsync(product);
            await _productContext.SaveChangesAsync();
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _productContext.Products.FindAsync(id);

            if (product is null)
            {
                return;
            }

            _productContext.Remove(product);
            await _productContext.SaveChangesAsync();
        }

        public async Task<Product?> GetProductWithCategoryByIdAsync(int id)
        {
            return await _productContext.Products
                .Include(p => p.Category)
                .SingleOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Product>> GetProductsAsync()
        {
            return await _productContext.Products.ToListAsync();
        }

        public async Task UpdateProductAsync(Product product)
        {
            _productContext.Update(product);
            await _productContext.SaveChangesAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _productContext.Products.FindAsync(id);
        }
    }
}
