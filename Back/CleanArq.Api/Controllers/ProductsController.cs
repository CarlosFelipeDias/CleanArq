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

        [HttpPost]
        [ProducesResponseType(typeof(ProductDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProductDTO>> Post([FromBody] ProductDTO productDto)
        {
            var result = await _productService.AddProductAsync(productDto);

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
        public async Task<IActionResult> Put(int id, [FromBody] ProductDTO productDto)
        {
            if (productDto is null)
            {
                return BadRequest(new { message = "Product data is required." });
            }

            if (id != productDto.Id)
            {
                return BadRequest(new { message = "The route id must match the product id." });
            }

            var result = await _productService.UpdateProductAsync(productDto);

            if (result.IsFailure)
            {
                if (result.Errors.Any(error => error.PropertyName == nameof(ProductDTO.Id)
                    && error.Message == "Product not found."))
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
            var result = await _productService.DeleteProductAsync(id);

            if (result.IsFailure)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
