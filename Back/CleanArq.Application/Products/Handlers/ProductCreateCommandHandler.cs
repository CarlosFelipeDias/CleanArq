using CleanArq.Application.Products.Commands;
using CleanArq.Domain.Common;
using CleanArq.Domain.Entities;
using CleanArq.Domain.Interfaces;
using MediatR;

namespace CleanArq.Application.Products.Handlers
{
    public class ProductCreateCommandHandler : IRequestHandler<ProductCreateCommand, Result<Product>>
    {
        private readonly IProductRepository _productRepository;

        public ProductCreateCommandHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Result<Product>> Handle(ProductCreateCommand request, CancellationToken cancellationToken)
        {
            var productResult = Product.Create(
                request.Name,
                request.Description,
                request.Price,
                request.Stock,
                request.ImageUrl,
                request.CategoryId);

            if (productResult.IsFailure)
            {
                return productResult;
            }

            await _productRepository.AddProductAsync(productResult.Value!);

            return productResult;
        }
    }
}
