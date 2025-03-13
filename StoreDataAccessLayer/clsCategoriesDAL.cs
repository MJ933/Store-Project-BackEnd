using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace StoreDataAccessLayer
{
    public interface ICategoriesRepository
    {
        Task<List<CategoryDTO>> GetAllCategoriesAsync();
        Task<(List<CategoryDTO> CategoriesList, int TotalCount)> GetCategoriesPaginatedWithFiltersAsync(
            int pageNumber, int pageSize, int? categoryID, string? categoryName, int? parentCategoryID, bool? isActive);
        Task<List<CategoryDTO>> GetActiveCategoriesWithProductsAsync();
        Task<CategoryDTO?> GetCategoryByCategoryIDAsync(int id);
        Task<int> AddCategoryAsync(CategoryDTO newCategoryDTO);
        Task<bool> UpdateCategoryAsync(CategoryDTO categoryDTO);
        Task<bool> DeleteCategoryAsync(int categoryID);
        Task<bool> IsCategoryExistsByCategoryIDAsync(int categoryID);
        Task<bool> IsCategoryExistsByCategoryNameAsync(string name);
    }

    public class CategoriesRepository : ICategoriesRepository
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger<CategoriesRepository> _logger;

        public CategoriesRepository(NpgsqlDataSource dataSource, ILogger<CategoriesRepository> logger)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<CategoryDTO>> GetAllCategoriesAsync()
        {
            try
            {
                const string sql = @"
                    SELECT CategoryID, CategoryName, ParentCategoryID, IsActive
                    FROM Categories
                    ORDER BY CategoryID";

                await using var connection = await _dataSource.OpenConnectionAsync();
                var categories = await connection.QueryAsync<CategoryDTO>(sql);

                return categories?.ToList() ?? new List<CategoryDTO>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllCategoriesAsync");
                return new List<CategoryDTO>();
            }
        }

        public async Task<(List<CategoryDTO> CategoriesList, int TotalCount)> GetCategoriesPaginatedWithFiltersAsync(
            int pageNumber, int pageSize, int? categoryID, string? categoryName, int? parentCategoryID, bool? isActive)
        {
            try
            {
                int offset = (pageNumber - 1) * pageSize;

                var parameters = new DynamicParameters();
                parameters.Add("PageSize", pageSize);
                parameters.Add("Offset", offset);
                parameters.Add("CategoryID", categoryID);
                parameters.Add("CategoryName", !string.IsNullOrEmpty(categoryName) ? $"%{categoryName}%" : null);
                parameters.Add("ParentCategoryID", parentCategoryID);
                parameters.Add("IsActive", isActive);

                var whereConditions = new List<string>();
                if (categoryID.HasValue) whereConditions.Add("c.CategoryID = @CategoryID");
                if (!string.IsNullOrEmpty(categoryName)) whereConditions.Add("c.CategoryName ILIKE @CategoryName");
                if (parentCategoryID.HasValue) whereConditions.Add("c.ParentCategoryID = @ParentCategoryID");
                if (isActive.HasValue) whereConditions.Add("c.IsActive = @IsActive");

                string whereClause = whereConditions.Any() ? $" WHERE {string.Join(" AND ", whereConditions)}" : string.Empty;

                string countQuery = $@"
                    SELECT COUNT(*) 
                    FROM Categories c{whereClause}";

                string categoriesQuery = $@"
                    SELECT CategoryID, CategoryName, ParentCategoryID, IsActive 
                    FROM Categories c{whereClause} 
                    ORDER BY c.CategoryID 
                    LIMIT @PageSize OFFSET @Offset";

                await using var connection = await _dataSource.OpenConnectionAsync();
                int totalCount = await connection.ExecuteScalarAsync<int>(countQuery, parameters);
                var categoriesList = await connection.QueryAsync<CategoryDTO>(categoriesQuery, parameters);

                return (categoriesList?.ToList() ?? new List<CategoryDTO>(), totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetCategoriesPaginatedWithFiltersAsync: Page {pageNumber}, Size {pageSize}",
                    pageNumber, pageSize);
                return (new List<CategoryDTO>(), 0);
            }
        }

        public async Task<List<CategoryDTO>> GetActiveCategoriesWithProductsAsync()
        {
            try
            {
                const string sql = @"
                    SELECT c.CategoryID, c.CategoryName, c.ParentCategoryID, c.IsActive
                    FROM Categories c
                    WHERE c.IsActive = true 
                      AND c.ParentCategoryID IS NULL
                      AND EXISTS (SELECT 1 FROM Products p WHERE p.CategoryID = c.CategoryID)
                    ORDER BY c.CategoryName";

                await using var connection = await _dataSource.OpenConnectionAsync();
                var categories = await connection.QueryAsync<CategoryDTO>(sql);

                return categories?.ToList() ?? new List<CategoryDTO>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetActiveCategoriesWithProductsAsync");
                return new List<CategoryDTO>();
            }
        }

        public async Task<CategoryDTO?> GetCategoryByCategoryIDAsync(int id)
        {
            try
            {
                const string sql = @"
                    SELECT CategoryID, CategoryName, ParentCategoryID, IsActive
                    FROM Categories 
                    WHERE CategoryID = @CategoryID 
                    LIMIT 1";

                await using var connection = await _dataSource.OpenConnectionAsync();
                return await connection.QuerySingleOrDefaultAsync<CategoryDTO>(sql, new { CategoryID = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCategoryByCategoryIDAsync for CategoryID: {CategoryID}", id);
                return null;
            }
        }

        public async Task<int> AddCategoryAsync(CategoryDTO newCategoryDTO)
        {
            try
            {
                const string sql = @"
                    INSERT INTO Categories (CategoryName, ParentCategoryID, IsActive)
                    VALUES (@CategoryName, @ParentCategoryID, @IsActive)
                    RETURNING CategoryID";

                var parameters = new
                {
                    CategoryName = newCategoryDTO.CategoryName,
                    ParentCategoryID = newCategoryDTO.ParentCategoryID,
                    IsActive = newCategoryDTO.IsActive
                };

                await using var connection = await _dataSource.OpenConnectionAsync();
                var insertedCategoryID = await connection.QuerySingleAsync<int>(sql, parameters);
                return insertedCategoryID;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in AddCategoryAsync for CategoryName: {newCategoryDTO.CategoryName}",
                    newCategoryDTO.CategoryName);
                return 0;
            }
        }

        public async Task<bool> UpdateCategoryAsync(CategoryDTO categoryDTO)
        {
            try
            {
                const string sql = @"
                    UPDATE Categories
                    SET CategoryName = @CategoryName, 
                        ParentCategoryID = @ParentCategoryID, 
                        IsActive = @IsActive
                    WHERE CategoryID = @CategoryID";

                var parameters = new
                {
                    CategoryID = categoryDTO.CategoryID,
                    CategoryName = categoryDTO.CategoryName,
                    ParentCategoryID = categoryDTO.ParentCategoryID,
                    IsActive = categoryDTO.IsActive
                };

                await using var connection = await _dataSource.OpenConnectionAsync();
                int rowsAffected = await connection.ExecuteAsync(sql, parameters);

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateCategoryAsync for CategoryID: {CategoryID}",
                    categoryDTO.CategoryID);
                return false;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int categoryID)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // First check if any products are using this category
                const string checkProductsQuery = @"
                    SELECT COUNT(*) 
                    FROM Products 
                    WHERE CategoryID = @CategoryID";

                int productCount = await connection.ExecuteScalarAsync<int>(
                    checkProductsQuery,
                    new { CategoryID = categoryID },
                    transaction);

                // If products exist, just soft delete the category
                const string sql = @"
                    UPDATE Categories 
                    SET IsActive = FALSE 
                    WHERE CategoryID = @CategoryID";

                int rowsAffected = await connection.ExecuteAsync(
                    sql,
                    new { CategoryID = categoryID },
                    transaction);

                if (rowsAffected <= 0)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in DeleteCategoryAsync for CategoryID: {CategoryID}", categoryID);
                return false;
            }
        }

        public async Task<bool> IsCategoryExistsByCategoryIDAsync(int categoryID)
        {
            try
            {
                const string sql = @"
                    SELECT 1 
                    FROM Categories 
                    WHERE CategoryID = @CategoryID 
                    LIMIT 1";

                await using var connection = await _dataSource.OpenConnectionAsync();
                var result = await connection.ExecuteScalarAsync<int?>(sql, new { CategoryID = categoryID });

                return result.HasValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IsCategoryExistsByCategoryIDAsync for CategoryID: {CategoryID}",
                    categoryID);
                return false;
            }
        }

        public async Task<bool> IsCategoryExistsByCategoryNameAsync(string name)
        {
            try
            {
                const string sql = @"
                    SELECT 1 
                    FROM Categories 
                    WHERE LOWER(CategoryName) = LOWER(@CategoryName) 
                    LIMIT 1";

                await using var connection = await _dataSource.OpenConnectionAsync();
                var result = await connection.ExecuteScalarAsync<int?>(sql, new { CategoryName = name });

                return result.HasValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IsCategoryExistsByCategoryNameAsync for CategoryName: {CategoryName}",
                    name);
                return false;
            }
        }
    }

    public record CategoryDTO
    {
        // Parameterless constructor for Dapper
        public CategoryDTO() { }

        // Constructor for creating instances
        public CategoryDTO(int categoryID, string categoryName, int? parentCategoryID, bool isActive)
        {
            CategoryID = categoryID;
            CategoryName = categoryName;
            ParentCategoryID = parentCategoryID;
            IsActive = isActive;
        }

        [DefaultValue(1)]
        public int CategoryID { get; init; }

        [Required(ErrorMessage = "Category Name Is Required!")]
        [StringLength(100, ErrorMessage = "The Name cannot exceed 100 characters")]
        [DefaultValue("Category Name")]
        public string CategoryName { get; init; } = string.Empty;

        public int? ParentCategoryID { get; init; }

        [DefaultValue(true)]
        public bool IsActive { get; init; }
    }
}