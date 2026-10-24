using Microsoft.AspNetCore.Authorization;
using CleanArq.Api.Security;
using CleanArq.Application.DTOs;
using CleanArq.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CleanArq.Api.Controllers
{
    [Authorize(Policy = KeycloakAuthentication.CatalogPolicy)]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CategoryDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CategoryDTO>>> Get()
        {
            var categories = await _categoryService.GetCategoriesAsync();
            if (categories is null || !categories.Any())
            {
                return NotFound("No categories found.");
            }
            return Ok(categories);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CategoryDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CategoryDTO>> GetById(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);

            if (category is null)
            {
                return NotFound("category not found.");
            }

            return Ok(category);
        }

        [HttpPost]
        [ProducesResponseType(typeof(CategoryDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CategoryDTO>> Post([FromBody] CategoryDTO categoryDto)
        {
            var result = await _categoryService.AddCategoryAsync(categoryDto);

            if (result.IsFailure)
            {
                return BadRequest(result.Errors);
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Put(int id, [FromBody] CategoryDTO categoryDto)
        {
            if (categoryDto is null)
            {
                return BadRequest(new { message = "Category data is required." });
            }

            if (id != categoryDto.Id)
            {
                return BadRequest(new { message = "The route id must match the category id." });
            }

            var result = await _categoryService.UpdateCategoryAsync(categoryDto);

            if (result.IsFailure)
            {
                if (result.Errors.Any(error => error.PropertyName == nameof(CategoryDTO.Id)
                    && error.Message == "Category not found."))
                {
                    return NotFound();
                }

                return BadRequest(result.Errors);
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _categoryService.DeleteCategoryAsync(id);

            if (result.IsFailure)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
