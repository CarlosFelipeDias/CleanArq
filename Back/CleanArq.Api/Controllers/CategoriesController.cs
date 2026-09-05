using CleanArq.Application.DTOs;
using CleanArq.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CleanArq.Api.Controllers
{
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
                return NotFound();
            }

            return Ok(category);
        }
    }
}
