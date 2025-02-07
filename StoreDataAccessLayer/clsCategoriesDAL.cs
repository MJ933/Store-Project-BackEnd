using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Dapper;

namespace StoreDataAccessLayer
{

    public class clsCategoriesDAL
    {
        private readonly NpgsqlDataSource _dataSource;

        public clsCategoriesDAL(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        }

        public List<CategoryDTO> GetAllCategories()
        {
            var categoriesList = new List<CategoryDTO>();
            try
            {
                using (var conn = _dataSource.OpenConnection())
                {
                    categoriesList = conn.Query<CategoryDTO>("SELECT * FROM Categories;").AsList();
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Error retrieving categories: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving categories: {ex.Message}");
            }
            return categoriesList;
        }

        public (List<CategoryDTO> CategoriesList, int TotalCount) GetCategoriesPaginatedWithFilters(
      int pageNumber, int pageSize, int? categoryID, string? categoryName, int? parentCategoryID, bool? isActive)
        {
            var categoriesList = new List<CategoryDTO>();
            int totalCount = 0;

            try
            {
                // Calculate offset for pagination
                int offset = (pageNumber - 1) * pageSize;

                // Step 1: Build the count query with dynamic conditions
                string countQuery = "SELECT COUNT(*) FROM Categories c";
                var countConditions = new List<string>();

                // Add conditions based on provided filters
                if (categoryID.HasValue)
                    countConditions.Add("c.CategoryID = @CategoryID");
                if (!string.IsNullOrEmpty(categoryName))
                    countConditions.Add("c.CategoryName ILIKE @CategoryName");
                if (parentCategoryID.HasValue)
                    countConditions.Add("c.ParentCategoryID = @ParentCategoryID");
                if (isActive.HasValue)
                    countConditions.Add("c.IsActive = @IsActive");

                // Append conditions to the count query
                if (countConditions.Any())
                    countQuery += " WHERE " + string.Join(" AND ", countConditions);

                // Parameters for count query
                var countParams = new
                {
                    CategoryID = categoryID,
                    CategoryName = !string.IsNullOrEmpty(categoryName) ? $"%{categoryName}%" : null,
                    ParentCategoryID = parentCategoryID,
                    IsActive = isActive
                };

                // Execute count query
                using var conn = _dataSource.OpenConnection();
                totalCount = conn.ExecuteScalar<int>(countQuery, countParams);

                // Step 2: Build the categories query with dynamic conditions
                string categoriesQuery = "SELECT * FROM Categories c";
                var categoriesConditions = new List<string>();

                // Add conditions based on provided filters (same as count query)
                if (categoryID.HasValue)
                    categoriesConditions.Add("c.CategoryID = @CategoryID");
                if (!string.IsNullOrEmpty(categoryName))
                    categoriesConditions.Add("c.CategoryName ILIKE @CategoryName");
                if (parentCategoryID.HasValue)
                    categoriesConditions.Add("c.ParentCategoryID = @ParentCategoryID");
                if (isActive.HasValue)
                    categoriesConditions.Add("c.IsActive = @IsActive");


                // Append conditions to the categories query
                if (categoriesConditions.Any())
                    categoriesQuery += " WHERE " + string.Join(" AND ", categoriesConditions);

                // Add pagination
                categoriesQuery += " ORDER BY c.CategoryID LIMIT @PageSize OFFSET @Offset;";

                // Parameters for categories query
                var categoryParams = new
                {
                    PageSize = pageSize,
                    Offset = offset,
                    CategoryID = categoryID,
                    CategoryName = !string.IsNullOrEmpty(categoryName) ? $"%{categoryName}%" : null,
                    ParentCategoryID = parentCategoryID,
                    IsActive = isActive
                };

                // Execute categories query
                var result = conn.Query<CategoryDTO>(categoriesQuery, categoryParams);
                categoriesList = result.AsList();

                return (categoriesList, totalCount);
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
            }

            return (categoriesList, totalCount);
        }

        public List<CategoryDTO> GetActiveCategoriesWithProducts()
        {
            var categoriesList = new List<CategoryDTO>();
            try
            {
                using (var conn = _dataSource.OpenConnection())
                {
                    var query = @"
                        SELECT DISTINCT c.categoryID, c.categoryName, c.parentCategoryID, c.isActive
                        FROM Categories c
                        JOIN Products p ON c.categoryID = p.categoryID
                        WHERE c.isActive = true AND c.parentCategoryID IS NULL;";
                    categoriesList = conn.Query<CategoryDTO>(query).AsList();
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Error retrieving categories: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving categories: {ex.Message}");
            }
            return categoriesList;
        }

        public async Task<List<CategoryDTO>> GetActiveCategoriesWithProductsAsync()
        {
            var categoriesList = new List<CategoryDTO>();
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var query = @"
                    SELECT c.categoryID, c.categoryName, c.parentCategoryID, c.isActive
                    FROM Categories c
                    WHERE c.isActive = true 
                      AND c.parentCategoryID IS NULL
                      AND EXISTS (SELECT 1 FROM Products p WHERE p.categoryID = c.categoryID);";
                categoriesList = (await conn.QueryAsync<CategoryDTO>(query)).AsList();
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Error retrieving categories: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving categories: {ex.Message}");
            }
            return categoriesList;
        }

        public CategoryDTO GetCategoryByCategoryID(int id)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                {
                    var query = "SELECT * FROM Categories WHERE CategoryID = @CategoryID LIMIT 1;";
                    return conn.QueryFirstOrDefault<CategoryDTO>(query, new { CategoryID = id });
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Error retrieving category: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving category: {ex.Message}");
            }
            return null;
        }

        public int AddCategory(CategoryDTO newCategoryDTO)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                {
                    var query = @"
                        INSERT INTO Categories (CategoryName, ParentCategoryID, IsActive)
                        VALUES (@CategoryName, @ParentCategoryID, @IsActive)
                        RETURNING CategoryID;";
                    return conn.ExecuteScalar<int>(query, newCategoryDTO);
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Error adding category: {ex.Message}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding category: {ex.Message}");
                return 0;
            }
        }

        public bool UpdateCategory(CategoryDTO categoryDTO)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                {
                    var query = @"
                        UPDATE Categories
                        SET CategoryName = @CategoryName, 
                            ParentCategoryID = @ParentCategoryID, 
                            IsActive = @IsActive
                        WHERE CategoryID = @CategoryID;";
                    int rowsAffected = conn.Execute(query, categoryDTO);
                    return rowsAffected > 0;
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Error updating category: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating category: {ex.Message}");
                return false;
            }
        }

        public bool DeleteCategory(int categoryID)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                {
                    var query = "UPDATE Categories SET IsActive = FALSE WHERE CategoryID = @CategoryID;";
                    int rowsAffected = conn.Execute(query, new { CategoryID = categoryID });
                    return rowsAffected > 0;
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Error deleting category: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting category: {ex.Message}");
                return false;
            }
        }

        public bool IsCategoryExistsByCategoryID(int categoryID)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                {
                    var query = "SELECT 1 FROM Categories WHERE CategoryID = @CategoryID LIMIT 1;";
                    return conn.ExecuteScalar<int?>(query, new { CategoryID = categoryID }) != null;
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Error checking if category exists: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if category exists: {ex.Message}");
                return false;
            }
        }

        public bool IsCategoryExistsByCategoryName(string name)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                {
                    var query = "SELECT 1 FROM Categories WHERE CategoryName = @CategoryName LIMIT 1;";
                    return conn.ExecuteScalar<int?>(query, new { CategoryName = name }) != null;
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Error checking if category exists: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if category exists: {ex.Message}");
                return false;
            }
        }
    }

    public class CategoryDTO
    {
        public CategoryDTO() { }

        public CategoryDTO(int categoryID, string categoryName, int? parentCategoryID, bool isActive)
        {
            CategoryID = categoryID;
            CategoryName = categoryName;
            ParentCategoryID = parentCategoryID;
            IsActive = isActive;
        }

        [DefaultValue(1)]
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Category Name Is Required!")]
        [StringLength(100, ErrorMessage = "The Name cannot exceed 100 characters")]
        [DefaultValue("Category Name")]
        public string CategoryName { get; set; }

        public int? ParentCategoryID { get; set; }

        [DefaultValue(true)]
        public bool IsActive { get; set; }
    }

}