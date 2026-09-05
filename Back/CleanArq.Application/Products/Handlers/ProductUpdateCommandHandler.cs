using CleanArq.Application.Products.Commands;
using CleanArq.Domain.Common;
using CleanArq.Domain.Entities;
using CleanArq.Domain.Interfaces;
using MediatR;

namespace CleanArq.Application.Products.Handlers
{
    public class ProductUpdateCommandHandler : IRequestHandler<PruductUpdateCommand, Result<Product>>
    {
        private readonly IProductRepository _productRepository;

        public ProductUpdateCommandHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        }

        public async Task<Result<Product>> Handle(PruductUpdateCommand request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetProductByIdAsync(request.Id);

            if (product is null)
            {
                return Result.Failure<Product>(
                    new Notification(nameof(request.Id), "Product not found."));
            }

            var updateResult = product.Update(
                request.Name,
                request.Description,
                request.Price,
                request.Stock,
                request.ImageUrl,
                request.CategoryId);

            if (updateResult.IsFailure)
            {
                return Result.Failure<Product>(updateResult.Errors);
            }

            await _productRepository.UpdateProductAsync(product);

            return Result.Success(product);
        }
    }
}
