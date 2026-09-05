using CleanArq.Domain.Common;
using CleanArq.Domain.Entities;
using MediatR;

namespace CleanArq.Application.Products.Commands
{
    public class ProductRemoveCommand : IRequest<Result<Product>>
    {
        public int Id { get; set; }

        public ProductRemoveCommand(int id)
        {
            Id = id;
        }
    }
}
