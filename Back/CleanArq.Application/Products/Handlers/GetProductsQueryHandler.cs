using CleanArq.Application.Products.Queries;
using CleanArq.Domain.Common;
using CleanArq.Domain.Entities;
using CleanArq.Domain.Interfaces;
using MediatR;

namespace CleanArq.Application.Products.Handlers
{
    public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, Result<IEnumerable<Product>>>
    {
        private readonly IProductRepository _productRepository;

        public GetProductsQueryHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        }

        public async Task<Result<IEnumerable<Product>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
        {
            var products = await _productRepository.GetProductsAsync();

            return Result.Success(products);
        }
    }
}
