using CleanArq.Domain.Common;
using CleanArq.Domain.Entities;
using MediatR;

namespace CleanArq.Application.Products.Queries
{
    public class GetProductByIdQuery : IRequest<Result<Product>>
    {
        public int Id { get; set; }

        public GetProductByIdQuery(int id)
        {
            Id = id;
        }

    }
}
