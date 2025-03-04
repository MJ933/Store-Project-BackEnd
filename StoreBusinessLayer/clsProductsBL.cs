using Npgsql;
using StoreDataAccessLayer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StoreBusinessLayer
{
    public class clsProductsBL
    {
        private readonly clsProductsDAL _productsDAL;
        public class PagedResult<T>
        {
            public List<T> Items { get; set; }
            public int TotalCount { get; set; }
        }
        public clsProductsBL(clsProductsDAL productsDAL)
        {
            _productsDAL = productsDAL;
        }



        public async Task<(List<FullProductDTO> Products, int TotalCount)> GetProductsPaginatedWithFiltersAllImagesAsync(
            int pageNumber, int pageSize, int? productID, string? productName, decimal? initialPrice, decimal? sellingPrice, string? description,
            int? categoryId, int? quantity, bool? isActive)
        {
            return await _productsDAL.GetProductsPaginatedWithFiltersAllImagesAsync(pageNumber, pageSize, productID, productName, initialPrice,
                sellingPrice, description, categoryId, quantity, isActive);
        }

        // Get a product by ID with all its images
        public async Task<FullProductDTO> GetProductWithAllImagesByProductIDAsync(int id)
        {
            return await _productsDAL.GetProductWithAllImagesByProductIDAsync(id);
        }

        public async Task<ProductDTO> GetProductByProductIDAsync(int id)
        {
            var product = await _productsDAL.GetProductByProductIDAsync(id);
            if (product != null)
                return product;
            else
            {
                Console.WriteLine("Product not found or an error occurred.");
                return new ProductDTO();
            }
        }

        public async Task<bool> DeleteProductByProductIDAsync(int id)
        {
            return await _productsDAL.DeleteByProductIDAsync(id);
        }

        public async Task<bool> IsProductExistsByProductIDAsync(int id)
        {
            return await _productsDAL.IsProductExistsByProductIDAsync(id);
        }


        public async Task<int> AddNewProductAsync(ProductDTO dto)
        {
            return await _productsDAL.AddNewProductAsync(dto);
        }

        public async Task<bool> UpdateProductAsync(ProductDTO dto)
        {
            return await _productsDAL.UpdateProductAsync(dto);
        }
        public async Task<bool> UpdateProductQuantity(int id, int quantity, NpgsqlConnection conn, NpgsqlTransaction transaction)
        {
            return await _productsDAL.UpdateProductQuantity(id, quantity, conn, transaction);
        }

    }
}