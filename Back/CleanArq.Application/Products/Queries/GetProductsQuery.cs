using CleanArq.Domain.Common;
using CleanArq.Domain.Entities;
using MediatR;

namespace CleanArq.Application.Products.Queries
{
    public class GetProductsQuery : IRequest<Result<IEnumerable<Product>>>
    {
        
    }
}
