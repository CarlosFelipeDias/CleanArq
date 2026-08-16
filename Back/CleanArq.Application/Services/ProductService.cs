using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CleanArq.Application.DTOs;
using CleanArq.Application.Interfaces;
using CleanArq.Domain.Common;
using CleanArq.Domain.Entities;
using CleanArq.Domain.Interfaces;

namespace CleanArq.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;

        public ProductService(IMapper mapper, IProductRepository productRepository)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        }

        public async Task<IEnumerable<ProductDTO>> GetProductsAsync()
        {
            var productEntities = await _productRepository.GetProductsAsync();
            var productDtos = _mapper.Map<IEnumerable<ProductDTO>>(productEntities);    
            return productDtos;

        }

        public async Task<ProductDTO?> GetProductByIdAsync(int? id)
        {
            if (id is null)
            {
                return null;
            }

            var productEntity = await _productRepository.GetProductByIdAsync(id.Value);
            if (productEntity is null)
            {
                return null;
            }
            var productDto = _mapper.Map<ProductDTO>(productEntity);
            return productDto;

        }

        public async Task<ProductDTO?> GetProductWithCategoryAsync(int? id)
        {
            if (id is null)
            {
                return null;
            }

            var productEntity = await _productRepository.GetProductWithCategoryByIdAsync(id.Value);
            if (productEntity is null)  
            {
                return null;
            }
            var productDto = _mapper.Map<ProductDTO>(productEntity);
            return productDto;

        }

        public async Task<Result<ProductDTO>> AddProductAsync(ProductDTO productDto)
        {
            if (productDto is null)
            {
                return Result.Failure<ProductDTO>(new Notification(nameof(productDto), "Product data is required."));
            }

            var productResult = Product.Create(
                productDto.Name,
                productDto.Description,
                productDto.Price,
                productDto.Stock,
                productDto.ImageUrl,
                productDto.CategoryId);

            if (productResult.IsFailure || productResult.Value is null)
            {
                return Result.Failure<ProductDTO>(productResult.Errors);
            }

            await _productRepository.AddProductAsync(productResult.Value);

            var createdProductDto = _mapper.Map<ProductDTO>(productResult.Value);
            return Result.Success(createdProductDto);
        }

        public async Task<Result> UpdateProductAsync(ProductDTO productDto)
        {
            if (productDto is null)
            {
                return Result.Failure(new Notification(nameof(productDto), "Product data is required."));
            }

            var productEntity = await _productRepository.GetProductByIdAsync(productDto.Id);

            if (productEntity is null)
            {
                return Result.Failure(new Notification(nameof(productDto.Id), "Product not found."));
            }

            var updateResult = productEntity.Update(
                productDto.Name,
                productDto.Description,
                productDto.Price,
                productDto.Stock,
                productDto.ImageUrl,
                productDto.CategoryId);

            if (updateResult.IsFailure)
            {
                return updateResult;
            }

            await _productRepository.UpdateProductAsync(productEntity);
            return Result.Success();

        }

        public async Task<Result> DeleteProductAsync(int? id)
        {
            if (id is null)
            {
                return Result.Failure(new Notification(nameof(id), "Product id is required."));
            }

            var productEntity = await _productRepository.GetProductByIdAsync(id.Value);
            if (productEntity is null)  
            {
                return Result.Failure(new Notification(nameof(id), "Product not found."));
            }

            await _productRepository.DeleteProductAsync(id.Value);
            return Result.Success();
                
        }
    }
}
