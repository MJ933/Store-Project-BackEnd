using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StoreBusinessLayer;
using StoreDataAccessLayer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StoreAPI.Controllers
{
    [Route("API/CategoriesAPI")]
    [ApiController]
    public class CategoriesAPIController : ControllerBase
    {
        private readonly ICategoriesService _categoriesService;
        private readonly ILogger<CategoriesAPIController> _logger;

        public CategoriesAPIController(ICategoriesService categoriesService, ILogger<CategoriesAPIController> logger)
        {
            _categoriesService = categoriesService ?? throw new ArgumentNullException(nameof(categoriesService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        [HttpGet("GetAll")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CategoryDTO>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "sales,marketing,admin")]
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                var categories = await _categoriesService.GetAllCategoriesAsync();
                if (categories.Count == 0)
                {
                    return NotFound("No categories found.");
                }
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllCategories");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }


        [HttpGet("GetCategoriesPaginatedWithFilters")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "sales,marketing,admin")]
        public async Task<IActionResult> GetCategoriesPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? categoryID = null,
            [FromQuery] string? categoryName = null,
            [FromQuery] int? parentCategoryID = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                if (pageNumber < 1 || pageSize < 1)
                {
                    return BadRequest("Page number and page size must be positive integers.");
                }

                var result = await _categoriesService.GetCategoriesPaginatedWithFiltersAsync(
                    pageNumber, pageSize, categoryID, categoryName, parentCategoryID, isActive);

                if (result.CategoriesList.Count == 0)
                {
                    return NotFound("No categories found matching the specified criteria.");
                }

                var paginatedResult = new
                {
                    TotalCount = result.TotalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(result.TotalCount / (double)pageSize),
                    Categories = result.CategoriesList
                };

                return Ok(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCategoriesPaginated: Page {PageNumber}, Size {PageSize}",
                    pageNumber, pageSize);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        [HttpGet("GetActiveCategoriesWithProductsAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CategoryDTO>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetActiveCategoriesWithProducts()
        {
            try
            {
                var categories = await _categoriesService.GetActiveCategoriesWithProductsAsync();
                if (categories.Count == 0)
                {
                    return NotFound("No active categories with products found.");
                }
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetActiveCategoriesWithProducts");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        [HttpGet("FindByCategoryID/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CategoryDTO))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "sales,marketing,admin")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            try
            {
                if (id < 1)
                {
                    return BadRequest("Invalid category ID. ID must be a positive integer.");
                }

                var category = await _categoriesService.GetCategoryByCategoryIDAsync(id);
                if (category == null)
                {
                    return NotFound($"Category with ID {id} not found.");
                }

                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCategoryById for CategoryID: {CategoryID}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        [HttpPost("Create")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CategoryDTO))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "sales,marketing,admin")]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryDTO newCategory)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(newCategory.CategoryName))
                {
                    return BadRequest("Category name is required.");
                }

                // Check if category with same name already exists
                if (await _categoriesService.IsCategoryExistsByCategoryNameAsync(newCategory.CategoryName))
                {
                    return BadRequest($"Category with name '{newCategory.CategoryName}' already exists.");
                }

                // Check if parent category exists if specified
                if (newCategory.ParentCategoryID.HasValue)
                {
                    bool parentExists = await _categoriesService.IsCategoryExistsByCategoryIDAsync(newCategory.ParentCategoryID.Value);
                    if (!parentExists)
                    {
                        return BadRequest($"Parent category with ID {newCategory.ParentCategoryID.Value} does not exist.");
                    }
                }
                _categoriesService.categoryDTO = newCategory;
                if (await _categoriesService.AddCategoryAsync())
                {
                    //var createdCategory = await _categoriesService.GetCategoryByCategoryIDAsync(categoryId);
                    return CreatedAtAction(nameof(GetCategoryById), new { id = _categoriesService.categoryDTO.CategoryID }, _categoriesService.categoryDTO);
                }

                return BadRequest("Failed to create category.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateCategory for CategoryName: {CategoryName}",
                    newCategory?.CategoryName ?? "null");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        [HttpPut("Update/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "sales,marketing,admin")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDTO updatedCategory)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (id < 1)
                {
                    return BadRequest("Invalid category ID. ID must be a positive integer.");
                }

                if (string.IsNullOrWhiteSpace(updatedCategory.CategoryName))
                {
                    return BadRequest("Category name is required.");
                }
                var existingCategory = await _categoriesService.GetCategoryByCategoryIDAsync(id);
                if (existingCategory == null)
                {
                    return NotFound($"Category with ID {id} not found.");
                }
                if (updatedCategory.ParentCategoryID.HasValue)
                {
                    // Prevent circular reference
                    if (updatedCategory.ParentCategoryID.Value == id)
                    {
                        return BadRequest("A category cannot be its own parent.");
                    }

                    bool parentExists = await _categoriesService.IsCategoryExistsByCategoryIDAsync(updatedCategory.ParentCategoryID.Value);
                    if (!parentExists)
                    {
                        return BadRequest($"Parent category with ID {updatedCategory.ParentCategoryID.Value} does not exist.");
                    }
                }

                // Create a new DTO with the route ID and updated values
                updatedCategory = new CategoryDTO(
                   id,
                   updatedCategory.CategoryName,
                   updatedCategory.ParentCategoryID,
                   updatedCategory.IsActive
               );
                _categoriesService.categoryDTO = updatedCategory;
                bool result = await _categoriesService.UpdateCategoryAsync();
                if (result)
                {
                    return Ok(updatedCategory);
                }

                return BadRequest("Failed to update category.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateCategory for CategoryID: {CategoryID}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }


        [HttpDelete("Delete/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "sales,marketing,admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                if (id < 1)
                {
                    return BadRequest("Invalid category ID. ID must be a positive integer.");
                }

                // Check if category exists
                if (!await _categoriesService.IsCategoryExistsByCategoryIDAsync(id))
                {
                    return NotFound($"Category with ID {id} not found.");
                }

                bool result = await _categoriesService.DeleteCategoryAsync(id);
                if (result)
                {
                    return Ok($"Category with ID: {id} was deleted successfully.");
                }

                return BadRequest("Failed to delete category. It may be referenced by products.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteCategory for CategoryID: {CategoryID}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }
    }
}