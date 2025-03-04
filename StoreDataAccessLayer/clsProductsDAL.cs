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
using Newtonsoft.Json;

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
        [Range(0.001, float.MaxValue, ErrorMessage = "Should be a Positive number!")]
        public decimal InitialPrice { get; set; }

        [Required]
        [Range(0.001, float.MaxValue, ErrorMessage = "Should be a Positive number!")]
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
            int? productID, string? productName, decimal? initialPrice, decimal? sellingPrice, string? description,
            int? categoryId, int? quantity, bool? isActive
        )
        {
            List<FullProductDTO> ProductsList = new List<FullProductDTO>();
            int totalCount = 0;

            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                string functionQuery = @"
                    SELECT * FROM fn_get_products_and_all_images_paginated_with_filters(
                        @p_page_number,
                        @p_page_size,
                        @p_product_id,
                        @p_product_name,
                        @p_initial_price,
                        @p_selling_price,
                        @p_description,
                        @p_category_id,
                        @p_quantity,
                        @p_is_active
                    );";
                var queryParams = new
                {
                    p_page_number = pageNumber,
                    p_page_size = pageSize,
                    p_product_id = productID,
                    p_product_name = productName,
                    p_initial_price = initialPrice,
                    p_selling_price = sellingPrice,
                    p_description = description,
                    p_category_id = categoryId,
                    p_quantity = quantity,
                    p_is_active = isActive
                };
                var result = await conn.QueryAsync<dynamic>(functionQuery, queryParams);

                foreach (var row in result)
                {

                    var product = new ProductDTO
                    {
                        ProductID = (int)row.product_id,
                        ProductName = (string)row.product_name,
                        InitialPrice = (decimal)row.initial_price,
                        SellingPrice = (decimal)row.selling_price,
                        Description = (string)row.description,
                        CategoryID = (int)row.category_id,
                        StockQuantity = (int)row.quantity,
                        IsActive = (bool)row.is_active
                    };
                    var images = new List<ImageDatabaseDTO>();

                    if (row.images != null)
                    {
                        images = JsonConvert.DeserializeObject<List<ImageDatabaseDTO>>(row.images?.ToString() ?? string.Empty);
                    }
                    ProductsList.Add(new FullProductDTO(product, images));
                    if (totalCount == 0 && (row.total_count != null))
                    {
                        totalCount = Convert.ToInt32(row.total_count);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return (ProductsList, totalCount);
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