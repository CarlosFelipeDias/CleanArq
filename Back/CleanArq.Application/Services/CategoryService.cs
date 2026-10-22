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
    public class CategoryService : ICategoryService
    {

        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;

        public CategoryService(ICategoryRepository categoryRepository, IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }


        public async Task<Result<CategoryDTO>> AddCategoryAsync(CategoryDTO categoryDto)
        {
            if (categoryDto is null)
            {
                return Result.Failure<CategoryDTO>(new Notification(nameof(categoryDto), "Category data is required."));
            }

            var categoryResult = Category.Create(categoryDto.Name);

            if (categoryResult.IsFailure || categoryResult.Value is null)
            {
                return Result.Failure<CategoryDTO>(categoryResult.Errors);
            }

            await _categoryRepository.AddCategoryAsync(categoryResult.Value);

            var createdCategoryDto = _mapper.Map<CategoryDTO>(categoryResult.Value);
            return Result.Success(createdCategoryDto);
        }

        public async Task<Result> DeleteCategoryAsync(int? id)
        {
            if (id is null)
            {
                return Result.Failure(new Notification(nameof(id), "Category id is required."));
            }

            var category = await _categoryRepository.GetCategoryByIdAsync(id.Value);

            if (category is null)
            {
                return Result.Failure(new Notification(nameof(id), "Category not found."));
            }

            await _categoryRepository.DeleteCategoryAsync(id.Value);
            return Result.Success();
        }

        public async Task<IEnumerable<CategoryDTO>> GetCategoriesAsync()
        {
            var categoryEntities = await _categoryRepository.GetCategoriesAsync();
            return _mapper.Map<IEnumerable<CategoryDTO>>(categoryEntities);
        }

        public async Task<CategoryDTO?> GetCategoryByIdAsync(int? id)
        {
            if (id is null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var categoryEntity = await _categoryRepository.GetCategoryByIdAsync(id.Value);
            return _mapper.Map<CategoryDTO?>(categoryEntity);
        }

        public async Task<Result> UpdateCategoryAsync(CategoryDTO categoryDto)
        {
            if (categoryDto is null)
            {
                return Result.Failure(new Notification(nameof(categoryDto), "Category data is required."));
            }

            var category = await _categoryRepository.GetCategoryByIdAsync(categoryDto.Id);

            if (category is null)
            {
                return Result.Failure(new Notification(nameof(categoryDto.Id), "Category not found."));
            }

            var updateResult = category.Update(categoryDto.Name);

            if (updateResult.IsFailure)
            {
                return updateResult;
            }

            await _categoryRepository.UpdateCategoryAsync(category);
            return Result.Success();
        }
    }
}
