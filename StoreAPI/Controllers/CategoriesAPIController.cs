using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StoreBusinessLayer;
using StoreDataAccessLayer;

namespace StoreAPI.Controllers
{
    [Route("API/CategoriesAPI")]
    [ApiController]
    public class CategoriesAPIController : ControllerBase
    {
        private readonly clsCategoriesBL _categoryBL;
        public CategoriesAPIController(clsCategoriesBL categoryBL)
        {
            _categoryBL = categoryBL;
        }
        [HttpGet("GetAll")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CategoryDTO>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public IActionResult GetAllCategories()
        {
            var categories = _categoryBL.GetAllCategories();
            if (categories.Count == 0)
                return NotFound("No categories found.");
            return Ok(categories);
        }

        [HttpGet("GetCategoriesPaginatedWithFilters")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CategoryDTO>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]

        public IActionResult GetCategoriesPaginatedWithFilters([FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10, [FromQuery] int? categoryID = null,
            [FromQuery] string? categoryName = null, [FromQuery] int? parentCategoryID = null, [FromQuery] bool? isActive = null)
        {
            if (pageNumber < 1 || pageSize < 1)
            {
                throw new ArgumentException("Page number and page size must be positive integers.");
            }
            var result = _categoryBL.GetCategoriesPaginatedWithFilters(pageNumber, pageSize, categoryID, categoryName, parentCategoryID, isActive);
            if (result.CategoriesList.Count == 0)
                return NotFound("No categories found.");
            return Ok(new
            {
                TotalCount = result.TotalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                CategoriesList = result.CategoriesList,

            });
        }


        [HttpGet("GetActiveCategoriesWithProductsAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CategoryDTO>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<CategoryDTO>>> GetActiveCategoriesWithProductsAsync()
        {
            var categories = await _categoryBL.GetActiveCategoriesWithProductsAsync();
            if (categories == null || categories.Count == 0)
                return NotFound("No categories found.");
            return Ok(categories);
        }

        [HttpGet("FindByCategoryID/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CategoryDTO))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public IActionResult GetCategoryByID(int id)
        {
            if (id < 1)
                return BadRequest("Invalid category ID.");
            var category = _categoryBL.FindCategoryByCategoryID(id);
            if (category == null)
                return NotFound($"Category with ID {id} not found.");
            return Ok(category.DTO);
        }

        [HttpPost("Create")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CategoryDTO))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "sales,marketing,admin")]
        public IActionResult AddCategory([FromBody] CategoryDTO newCategoryDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            if (_categoryBL.IsCategoryExistsByCategoryName(newCategoryDTO.CategoryName))
                return BadRequest("Category name already exists.");
            var categoryObj = new clsCategoriesBL(newCategoryDTO);

            if (categoryObj.Save())
                return CreatedAtAction(nameof(GetCategoryByID), new { id = newCategoryDTO.CategoryID }, newCategoryDTO);
            return BadRequest("Failed to add category.");
        }

        [HttpPut("Update/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public IActionResult UpdateCategory(int id, [FromBody] CategoryDTO updatedCategory)
        {
            updatedCategory.CategoryID = id;
            if (id < 1)
                return BadRequest("Invalid category ID.");
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var categoryObj = _categoryBL.FindCategoryByCategoryID(id);

            if (categoryObj == null)
                return NotFound($"Category with ID {id} not found.");
            categoryObj.DTO = updatedCategory;
            if (categoryObj.Save())
                return Ok(updatedCategory);
            return BadRequest("Failed to update category.");
        }

        [HttpDelete("Delete/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public IActionResult DeleteCategory(int id)
        {
            if (id < 1)
                return BadRequest("Invalid category ID.");
            if (!_categoryBL.IsCategoryExistsByCategoryID(id))
                return NotFound($"Category with ID {id} not found.");
            if (_categoryBL.DeleteCategory(id))
                return Ok($"Category with ID {id} deleted successfully.");
            return BadRequest("Failed to delete category.");
        }
    }
}