using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CleanArq.Domain.Common;
using CleanArq.Domain.Entities;
using MediatR;

namespace CleanArq.Application.Products.Commands
{
    public abstract class ProductCommand : IRequest<Result<Product>>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        
    }
}
