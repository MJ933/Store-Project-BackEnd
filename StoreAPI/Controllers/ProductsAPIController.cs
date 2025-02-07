using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreBusinessLayer;
using StoreDataAccessLayer;
using System.Collections.Generic;
using System.Threading.Tasks;
using static StoreBusinessLayer.clsProductsBL;

namespace StoreAPI.Controllers
{
    [Route("API/ProductsAPI")]
    [ApiController]
    public class ProductsAPIController : ControllerBase
    {
        private readonly clsProductsBL _productsBL;
        private readonly clsCategoriesBL _categoryBL;

        public ProductsAPIController(clsProductsBL productsBL, clsCategoriesBL categoryBL)
        {
            _productsBL = productsBL;
            _categoryBL = categoryBL;
        }

        [HttpGet("GetALL", Name = "GetAllProducts")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<ProductDTO>>> GetAllProducts()
        {
            var productsList = await _productsBL.GetAllProductsAsync();
            if (productsList.Count == 0)
                return NotFound("There are no products in the database!");
            return Ok(productsList);
        }

        [HttpGet("GetAllProductsWithPrimaryImage", Name = "GetAllProductsWithPrimaryImage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<FullProductDTO>>> GetAllProductsWithPrimaryImage()
        {
            var productsList = await _productsBL.GetAllProductsWithPrimaryImageAsync();
            if (productsList.Count == 0)
                return NotFound("There are no products in the database!");
            return Ok(productsList);
        }

        [HttpGet("GetAllProductsWithAllImages", Name = "GetAllProductsWithAllImages")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<FullProductDTO>>> GetAllProductsWithAllImages()
        {
            var productsList = await _productsBL.GetAllProductsWithAllImagesAsync();
            if (productsList.Count == 0)
                return NotFound("There are no products in the database!");
            return Ok(productsList);
        }

        [HttpGet("GetAllProductsWithPrimaryImagePaged", Name = "GetAllProductsWithPrimaryImagePaged")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PagedResult<FullProductDTO>>> GetAllProductsWithPrimaryImagePaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? categoryId = null)
        {
            if (pageNumber < 1)
                return BadRequest("Page number must be greater than or equal to 1.");

            if (pageSize < 1)
                return BadRequest("Page size must be greater than or equal to 1.");

            var result = await _productsBL.GetAllProductsWithPrimaryImagePagedAsync(pageNumber, pageSize, categoryId);

            if (result.Items == null || result.Items.Count == 0)
                return NotFound("No products found for the requested page.");

            return Ok(result);
        }

        [HttpGet("GetAllProductsWithAllImagesPaged", Name = "GetAllProductsWithAllImagesPaged")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PagedResult<FullProductDTO>>> GetAllProductsWithAllImagesPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? categoryId = null)
        {
            if (pageNumber < 1)
                return BadRequest("Page number must be greater than or equal to 1.");

            if (pageSize < 1)
                return BadRequest("Page size must be greater than or equal to 1.");

            var result = await _productsBL.GetAllProductsWithAllImagesPagedAsync(pageNumber, pageSize, categoryId);

            if (result.Items == null || result.Items.Count == 0)
                return NotFound("No products found for the requested page.");

            return Ok(result);
        }

        [HttpGet("GetAllProductsWithPrimaryImagePagedWithSearch", Name = "GetAllProductsWithPrimaryImagePagedWithSearch")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PagedResult<FullProductDTO>>> GetAllProductsWithPrimaryImagePagedWithSearch(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? searchTerm = null)
        {
            if (pageNumber < 1)
                return BadRequest("Page number must be greater than or equal to 1.");

            if (pageSize < 1)
                return BadRequest("Page size must be greater than or equal to 1.");

            var result = await _productsBL.GetAllProductsWithPrimaryImagePagedWithSearchAsync(pageNumber, pageSize, categoryId, searchTerm);

            if (result.Items == null || result.Items.Count == 0)
                return NotFound("No products found for the requested page.");

            return Ok(result);
        }

        [HttpGet("GetAllProductsWithAllImagesPagedWithSearch", Name = "GetAllProductsWithAllImagesPagedWithSearch")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PagedResult<FullProductDTO>>> GetAllProductsWithAllImagesPagedWithSearch(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? searchTerm = null)
        {
            if (pageNumber < 1)
                return BadRequest("Page number must be greater than or equal to 1.");

            if (pageSize < 1)
                return BadRequest("Page size must be greater than or equal to 1.");

            var result = await _productsBL.GetAllProductsWithAllImagesPagedWithSearchAsync(pageNumber, pageSize, categoryId, searchTerm);

            if (result.Items == null || result.Items.Count == 0)
                return NotFound("No products found for the requested page.");

            return Ok(result);
        }

        [HttpGet("GetProductsPaginatedWithFiltersPrimaryImage", Name = "GetProductsPaginatedWithFiltersPrimaryImage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PagedResult<FullProductDTO>>> GetProductsPaginatedWithFiltersPrimaryImage(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? productID = null,
            [FromQuery] string? productName = null,
            [FromQuery] int? initialPrice = null,
            [FromQuery] int? sellingPrice = null,
            [FromQuery] string? description = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] int? quantity = null,
            [FromQuery] bool? isActive = null)
        {
            if (pageNumber < 1)
                return BadRequest("Page number must be greater than or equal to 1.");

            if (pageSize < 1)
                return BadRequest("Page size must be greater than or equal to 1.");

            var result = await _productsBL.GetProductsPaginatedWithFiltersPrimaryImageAsync(pageNumber, pageSize, productID, productName, initialPrice, sellingPrice,
                description, categoryId, quantity, isActive);

            if (result.Products == null || result.Products.Count == 0)
                return NotFound("No products found for the requested page.");

            return Ok(new
            {
                TotalCount = result.TotalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                ProductList = result.Products
            });
        }

        [HttpGet("GetProductsPaginatedWithFiltersAllImages", Name = "GetProductsPaginatedWithFiltersAllImages")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PagedResult<FullProductDTO>>> GetProductsPaginatedWithFiltersAllImages(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? productID = null,
            [FromQuery] string? productName = null,
            [FromQuery] int? initialPrice = null,
            [FromQuery] int? sellingPrice = null,
            [FromQuery] string? description = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] int? quantity = null,
            [FromQuery] bool? isActive = null)
        {
            if (pageNumber < 1)
                return BadRequest("Page number must be greater than or equal to 1.");

            if (pageSize < 1)
                return BadRequest("Page size must be greater than or equal to 1.");

            var result = await _productsBL.GetProductsPaginatedWithFiltersAllImagesAsync(pageNumber, pageSize, productID, productName, initialPrice, sellingPrice,
                description, categoryId, quantity, isActive);

            if (result.Products == null || result.Products.Count == 0)
                return NotFound("No products found for the requested page.");

            return Ok(new
            {
                TotalCount = result.TotalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                ProductList = result.Products
            });
        }

        [HttpGet("GetProductWithPrimaryImageByID/{id}", Name = "GetProductWithPrimaryImageByID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<FullProductDTO>> GetProductWithPrimaryImageByID([FromRoute] int id)
        {
            if (id < 1)
                return BadRequest($"Not Accepted ID {id}");

            var product = await _productsBL.GetProductWithPrimaryImageByProductIDAsync(id);
            if (product == null)
                return NotFound($"There is no product with ID {id}");

            return Ok(product);
        }

        [HttpGet("GetProductWithAllImagesByID/{id}", Name = "GetProductWithAllImagesByID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<FullProductDTO>> GetProductWithAllImagesByID([FromRoute] int id)
        {
            if (id < 1)
                return BadRequest($"Not Accepted ID {id}");

            var product = await _productsBL.GetProductWithAllImagesByProductIDAsync(id);
            if (product == null)
                return NotFound($"There is no product with ID {id}");

            return Ok(product);
        }

        [HttpGet("GetProductByID/{id}", Name = "GetProductByProductID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductDTO>> GetProduct([FromRoute] int id)
        {
            if (id < 1)
                return BadRequest($"Not Accepted ID {id}");
            var product = await _productsBL.GetProductByProductIDAsync(id);
            if (product == null)
                return NotFound($"There is no product with ID {id}");
            return Ok(product);
        }

        [HttpPost("create", Name = "AddProduct")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "sales,marketing,admin")]
        public async Task<ActionResult<ProductDTO>> AddProduct(ProductDTO newProductDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            if (!_categoryBL.IsCategoryExistsByCategoryID(newProductDTO.CategoryID))
                return BadRequest("The Category Is Not Exists, please select another category!");
            newProductDTO.ProductID = await _productsBL.AddNewProductAsync(newProductDTO);
            if (newProductDTO.ProductID > 0)
                return CreatedAtRoute("GetProductByProductID", new { id = newProductDTO.ProductID }, newProductDTO);
            else
                return BadRequest("Failed to add the product.");
        }

        [HttpPut("Update/{id}", Name = "UpdateProduct")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public async Task<ActionResult<ProductDTO>> UpdateProduct([FromRoute] int id, [FromBody] ProductDTO updatedProductDTO)
        {
            if (id < 1)
                return BadRequest($"Not Accepted ID {id}");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _productsBL.GetProductByProductIDAsync(id);
            if (product == null)
                return NotFound($"There is no product with ID = {id}");

            updatedProductDTO.ProductID = id;
            var result = await _productsBL.UpdateProductAsync(updatedProductDTO);
            if (result)
                return Ok(updatedProductDTO);
            else
                return BadRequest("Failed to update the product.");
        }

        [HttpDelete("Delete/{id}", Name = "DeleteProduct")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "sales,marketing,admin")]
        public async Task<ActionResult> DeleteProduct([FromRoute] int id)
        {
            if (id <= 0)
                return BadRequest($"Please enter a valid ID = {id}");

            if (!await _productsBL.IsProductExistsByProductIDAsync(id))
                return NotFound($"There is no product with ID = {id}");

            if (await _productsBL.DeleteProductByProductIDAsync(id))
                return Ok($"The product was deleted successfully with ID = {id}");
            else
                return StatusCode(500, "ERROR: The product was not deleted.");
        }
    }
}