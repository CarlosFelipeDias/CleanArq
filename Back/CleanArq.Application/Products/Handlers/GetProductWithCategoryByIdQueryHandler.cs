using CleanArq.Application.Products.Queries;
using CleanArq.Domain.Common;
using CleanArq.Domain.Entities;
using CleanArq.Domain.Interfaces;
using MediatR;

namespace CleanArq.Application.Products.Handlers
{
    public class GetProductWithCategoryByIdQueryHandler : IRequestHandler<GetProductWithCategoryByIdQuery, Result<Product>>
    {
        private readonly IProductRepository _productRepository;

        public GetProductWithCategoryByIdQueryHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        }

        public async Task<Result<Product>> Handle(GetProductWithCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetProductWithCategoryByIdAsync(request.Id);

            if (product is null)
            {
                return Result.Failure<Product>(
                    new Notification(nameof(request.Id), "Product not found."));
            }

            return Result.Success(product);
        }
    }
}
