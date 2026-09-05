using CleanArq.Application.Products.Commands;
using CleanArq.Domain.Common;
using CleanArq.Domain.Entities;
using CleanArq.Domain.Interfaces;
using MediatR;

namespace CleanArq.Application.Products.Handlers
{
    public class ProductRemoveCommandHandler : IRequestHandler<ProductRemoveCommand, Result<Product>>
    {
        private readonly IProductRepository _productRepository;

    
        public ProductRemoveCommandHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        }

        public async Task<Result<Product>> Handle(ProductRemoveCommand request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetProductByIdAsync(request.Id);

            if (product is null)
            {
                return Result.Failure<Product>(
                    new Notification(nameof(request.Id), "Product not found."));
            }

            await _productRepository.DeleteProductAsync(request.Id);

            return Result.Success(product);
        }
    }
}
