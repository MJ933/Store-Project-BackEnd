using Imagekit.Helper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json;
using System.Data.Common;
using Dapper;
namespace StoreDataAccessLayer
{
    public class ProductDTO
    {

        public ProductDTO(int productID = 1, string productName = "Default ProductName", decimal initialPrice = 1
            , decimal sellingPrice = 1, string description = "The Description Of The Product.", int categoryID = 1
            , int stockQuantity = 1, bool isActive = true)
        {
            ProductID = productID;
            ProductName = productName;
            InitialPrice = initialPrice;
            SellingPrice = sellingPrice;
            Description = description;
            CategoryID = categoryID;
            StockQuantity = stockQuantity;
            IsActive = isActive;

        }


        [DefaultValue(1)]
        [Range(1, int.MaxValue, ErrorMessage = "Should be a Positive number!")]
        public int ProductID { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Cannot exceed 100 characters!")]
        [DefaultValue("Default Product Name")]
        public string ProductName { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Should be a Positive number!")]
        public decimal InitialPrice { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Should be a Positive number!")]
        public decimal SellingPrice { get; set; }

        [DefaultValue("The Description of the product.")]
        [StringLength(1000, ErrorMessage = "Cannot exceed 100 characters!")]
        public string Description { get; set; }

        [Required]
        [DefaultValue(1)]
        [Range(1, int.MaxValue, ErrorMessage = "Should be a Positive number!")]
        public int CategoryID { get; set; }

        [Required]
        [DefaultValue(1)]
        [Range(1, int.MaxValue, ErrorMessage = "Should be a Positive number!")]
        public int StockQuantity { get; set; }

        [DefaultValue(true)]
        public bool IsActive { get; set; }

    }

    public class FullProductDTO
    {
        public FullProductDTO(ProductDTO product, List<ImageDatabaseDTO> images)
        {
            Product = product;
            Images = images;
        }
        public FullProductDTO(ProductDTO product, ImageDatabaseDTO image)
        {
            Product = product;
            Images = image != null ? new List<ImageDatabaseDTO>() { image } : new List<ImageDatabaseDTO>();
        }


        // Make these properties public
        public ProductDTO Product { get; set; }
        public List<ImageDatabaseDTO> Images { get; set; }
    }




    public class clsProductsDAL
    {
        private readonly NpgsqlDataSource _dataSource;

        public clsProductsDAL(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }


        public async Task<(List<FullProductDTO> Products, int TotalCount)> GetProductsPaginatedWithFiltersAllImagesAsync(
int pageNumber,
int pageSize,
int? productID, string? productName, int? initialPrice, int? sellingPrice, string? description,
int? categoryId, int? quantity, bool? isActive
)
        {
            var fullProducts = new List<FullProductDTO>();
            int totalCount = 0;

            try
            {
                int offset = (pageNumber - 1) * pageSize;

                await using var conn = await _dataSource.OpenConnectionAsync();

                // Step 1: Build the count query with dynamic conditions (same as before)
                string countQuery = "SELECT COUNT(*) FROM Products p";
                var countConditions = new List<string>();

                if (productID.HasValue)
                    countConditions.Add("p.ProductID = @ProductID");
                if (!string.IsNullOrEmpty(productName))
                    countConditions.Add("p.ProductName ILIKE @ProductName");
                if (initialPrice.HasValue)
                    countConditions.Add("p.InitialPrice = @InitialPrice");
                if (sellingPrice.HasValue)
                    countConditions.Add("p.SellingPrice = @SellingPrice");
                if (!string.IsNullOrEmpty(description))
                    countConditions.Add("p.Description ILIKE @Description");
                if (categoryId.HasValue)
                    countConditions.Add("p.CategoryID = @CategoryID");
                if (quantity.HasValue)
                    countConditions.Add("p.StockQuantity = @Quantity");
                if (isActive.HasValue)
                    countConditions.Add("p.IsActive = @IsActive");


                if (countConditions.Any())
                    countQuery += " WHERE " + string.Join(" AND ", countConditions);

                // Parameters for count query (same as before)
                var countParams = new
                {
                    ProductID = productID,
                    ProductName = !string.IsNullOrEmpty(productName) ? $"%{productName}%" : null,
                    InitialPrice = initialPrice,
                    SellingPrice = sellingPrice,
                    Description = !string.IsNullOrEmpty(description) ? $"%{description}%" : null,
                    CategoryID = categoryId,
                    Quantity = quantity,
                    IsActive = isActive
                };
                totalCount = await conn.ExecuteScalarAsync<int>(countQuery, countParams);

                // Step 2: Build the products query with dynamic conditions (using CTE for all images)
                string productsQuery = @"
            WITH PagedProducts AS (
                SELECT p.productid
                FROM Products p
                WHERE (@ProductID IS NULL OR p.ProductID = @ProductID)
                  AND (@ProductName IS NULL OR p.ProductName ILIKE @ProductName)
                  AND (@InitialPrice IS NULL OR p.InitialPrice = @InitialPrice)
                  AND (@SellingPrice IS NULL OR p.SellingPrice = @SellingPrice)
                  AND (@Description IS NULL OR p.Description ILIKE @Description)
                  AND (@CategoryID IS NULL OR p.CategoryID = @CategoryID)
                  AND (@Quantity IS NULL OR p.StockQuantity = @Quantity)
                  AND (@IsActive IS NULL OR p.IsActive = @IsActive)
                ORDER BY p.productid
                LIMIT @PageSize OFFSET @Offset
            )
            SELECT
                p.*,
                i.ImageID,
                i.ImageURL,
                i.IsPrimary,
                i.FileId
            FROM PagedProducts pp
            JOIN Products p ON pp.productid = p.productid
            LEFT JOIN Images i ON p.ProductID = i.ProductID
            ORDER BY p.productid, i.ImageID;
        ";


                // Parameters for products query (same as before)
                var queryParams = new
                {
                    PageSize = pageSize,
                    Offset = offset,
                    ProductID = productID,
                    ProductName = !string.IsNullOrEmpty(productName) ? $"%{productName}%" : null,
                    InitialPrice = initialPrice,
                    SellingPrice = sellingPrice,
                    Description = !string.IsNullOrEmpty(description) ? $"%{description}%" : null,
                    CategoryID = categoryId,
                    Quantity = quantity,
                    IsActive = isActive
                };

                var results = await conn.QueryAsync<dynamic>(productsQuery, queryParams);

                var productDictionary = new Dictionary<int, FullProductDTO>();

                foreach (var item in results)
                {
                    int productId = item.productid;

                    if (!productDictionary.ContainsKey(productId))
                    {
                        var product = new ProductDTO(
                            item.productid,
                            item.productname,
                            item.initialprice,
                            item.sellingprice,
                            item.description,
                            item.categoryid,
                            item.stockquantity,
                            item.isactive
                        );
                        productDictionary[productId] = new FullProductDTO(product, new List<ImageDatabaseDTO>());
                    }

                    if (item.imageid != null)
                    {
                        ImageDatabaseDTO image = new ImageDatabaseDTO(
                            item.imageid,
                            item.imageurl,
                            item.productid,
                            item.isprimary,
                            item.fileid
                        );
                        productDictionary[productId].Images.Add(image);
                    }
                }
                fullProducts = productDictionary.Values.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return (fullProducts, totalCount);
        }


        public async Task<FullProductDTO> GetProductWithAllImagesByProductIDAsync(int productID)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var results = await conn.QueryAsync<dynamic>(@"
            SELECT
                p.*,
                i.ImageID,
                i.ImageURL,
                i.IsPrimary,
                i.FileId
            FROM Products p
            LEFT JOIN Images i ON p.ProductID = i.ProductID
            WHERE p.ProductID = @productID
        ", new { productID });

                if (!results.Any())
                {
                    return null; // No product found with the given ID
                }

                ProductDTO product = null;
                List<ImageDatabaseDTO> images = new List<ImageDatabaseDTO>();

                foreach (var result in results)
                {
                    if (product == null) // Create product object only once
                    {
                        product = new ProductDTO(
                            result.productid,
                            result.productname,
                            result.initialprice,
                            result.sellingprice,
                            result.description,
                            result.categoryid,
                            result.stockquantity,
                            result.isactive
                        );
                    }

                    if (result.imageid != null)
                    {
                        ImageDatabaseDTO image = new ImageDatabaseDTO(
                            result.imageid,
                            result.imageurl,
                            result.productid,
                            result.isprimary,
                            result.fileid
                        );
                        images.Add(image);
                    }
                }


                return new FullProductDTO(product, images);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null; // Return null in case of an exception
            }
        }


        public async Task<ProductDTO?> GetProductByProductIDAsync(int id)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                return await conn.QueryFirstOrDefaultAsync<ProductDTO>(
                    "SELECT * FROM Products WHERE ProductID = @ProductID",
                    new { ProductID = id }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null; // Now this is valid
            }
        }

        public async Task<int> AddNewProductAsync(ProductDTO dto)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                return await conn.ExecuteScalarAsync<int>(
                    "SELECT public.fn_add_new_product(@ProductName, @InitialPrice, @SellingPrice, @Description, @CategoryID, @StockQuantity)",
                    dto
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public async Task<bool> UpdateProductAsync(ProductDTO dto)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var result = await conn.ExecuteScalarAsync<bool>(
                    "SELECT public.fn_update_product(@ProductID, @ProductName, @InitialPrice, @SellingPrice, @Description, @CategoryID, @StockQuantity, @IsActive)",
                    dto
                );
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> DeleteByProductIDAsync(int id)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var result = await conn.ExecuteAsync(
                    "UPDATE Products SET IsActive = false WHERE ProductID = @ProductID",
                    new { ProductID = id }
                );
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> IsProductExistsByProductIDAsync(int id)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var result = await conn.ExecuteScalarAsync<int>(
                    "SELECT 1 FROM Products WHERE ProductID = @ProductID LIMIT 1",
                    new { ProductID = id }
                );
                return result == 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> UpdateProductQuantity(int id, int quantity, NpgsqlConnection conn, NpgsqlTransaction transaction)
        {
            try
            {
                var result = await conn.ExecuteAsync(
                   "UPDATE Products SET StockQuantity = @StockQuantity WHERE ProductID = @ProductID;",
                   new { ProductID = id, StockQuantity = quantity }, transaction
               );
                return result > 0; // Return true if at least one row was affected
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

    }
}