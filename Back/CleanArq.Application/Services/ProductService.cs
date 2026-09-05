using AutoMapper;
using CleanArq.Application.DTOs;
using CleanArq.Application.Interfaces;
using CleanArq.Application.Products.Commands;
using CleanArq.Application.Products.Queries;
using CleanArq.Domain.Common;
using MediatR;

namespace CleanArq.Application.Services
{
    public class ProductService : IProductService
    {
        
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;

        public ProductService(IMapper mapper, IMediator mediator)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<IEnumerable<ProductDTO>> GetProductsAsync()
        {
            var result = await _mediator.Send(new GetProductsQuery());

            if (result.IsFailure || result.Value is null)
            {
                return Enumerable.Empty<ProductDTO>();
            }

            return _mapper.Map<IEnumerable<ProductDTO>>(result.Value);
        }

        public async Task<ProductDTO?> GetProductByIdAsync(int? id)
        {
            if (id is null)
            {
                return null;
            }

            var result = await _mediator.Send(new GetProductByIdQuery(id.Value));

            if (result.IsFailure || result.Value is null)
            {
                return null;
            }

            return _mapper.Map<ProductDTO>(result.Value);

        }

        public async Task<ProductDTO?> GetProductWithCategoryAsync(int? id)
        {
            if (id is null)
            {
                return null;
            }

            var result = await _mediator.Send(new GetProductWithCategoryByIdQuery(id.Value));

            if (result.IsFailure || result.Value is null)
            {
                return null;
            }

            return _mapper.Map<ProductDTO>(result.Value);

        }

        public async Task<Result<ProductDTO>> AddProductAsync(ProductDTO productDto)
        {
            if (productDto is null)
            {
                return Result.Failure<ProductDTO>(new Notification(nameof(productDto), "Product data is required."));
            }

            var command = _mapper.Map<ProductCreateCommand>(productDto);
            var productResult = await _mediator.Send(command);

            if (productResult.IsFailure || productResult.Value is null)
            {
                return Result.Failure<ProductDTO>(productResult.Errors);
            }

            var createdProductDto = _mapper.Map<ProductDTO>(productResult.Value);
            return Result.Success(createdProductDto);
        }

        public async Task<Result> UpdateProductAsync(ProductDTO productDto)
        {
            if (productDto is null)
            {
                return Result.Failure(new Notification(nameof(productDto), "Product data is required."));
            }

            var command = _mapper.Map<PruductUpdateCommand>(productDto);
            var updateResult = await _mediator.Send(command);

            if (updateResult.IsFailure)
            {
                return Result.Failure(updateResult.Errors);
            }

            return Result.Success();

        }

        public async Task<Result> DeleteProductAsync(int? id)
        {
            if (id is null)
            {
                return Result.Failure(new Notification(nameof(id), "Product id is required."));
            }

            var removeResult = await _mediator.Send(new ProductRemoveCommand(id.Value));

            if (removeResult.IsFailure)
            {
                return Result.Failure(removeResult.Errors);
            }

            return Result.Success();
                
        }
    }
}
