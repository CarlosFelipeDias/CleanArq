using CleanArq.Application.Products.Queries;
using CleanArq.Domain.Common;
using CleanArq.Domain.Entities;
using CleanArq.Domain.Interfaces;
using MediatR;

namespace CleanArq.Application.Products.Handlers
{
    public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<Product>>
    {
        private readonly IProductRepository _productRepository;

        public GetProductByIdQueryHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        }

        public async Task<Result<Product>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetProductByIdAsync(request.Id);

            if (product is null)
            {
                return Result.Failure<Product>(
                    new Notification(nameof(request.Id), "Product not found."));
            }

            return Result.Success(product);
        }
    }
}
