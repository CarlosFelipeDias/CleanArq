using CleanArq.Application.DTOs;
using CleanArq.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CleanArq.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProductDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ProductDTO>>> Get()
        {
            var products = await _productService.GetProductsAsync();
            return Ok(products);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ProductDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductDTO>> GetById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);

            if (product is null)
            {
                return NotFound();
            }

            return Ok(product);
        }
    }
}
