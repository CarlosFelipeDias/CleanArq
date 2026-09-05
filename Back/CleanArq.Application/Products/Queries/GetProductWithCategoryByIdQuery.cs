using CleanArq.Domain.Common;
using CleanArq.Domain.Entities;
using MediatR;

namespace CleanArq.Application.Products.Queries
{
    public class GetProductWithCategoryByIdQuery : IRequest<Result<Product>>
    {
        public int Id { get; set; }

        public GetProductWithCategoryByIdQuery(int id)
        {
            Id = id;
        }
    }
}
