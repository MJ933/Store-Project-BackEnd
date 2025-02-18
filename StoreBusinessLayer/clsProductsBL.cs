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

        public async Task<List<ProductDTO>> GetAllProductsAsync()
        {
            return await _productsDAL.GetAllProductsAsync();
        }

        // Method to get all products with primary images
        public async Task<List<FullProductDTO>> GetAllProductsWithPrimaryImageAsync()
        {
            return await _productsDAL.GetAllProductsWithPrimaryImageAsync();
        }

        // Method to get all products with all images
        public async Task<List<FullProductDTO>> GetAllProductsWithAllImagesAsync()
        {
            return await _productsDAL.GetAllProductsWithAllImagesAsync();
        }

        // Paged method to get all products with primary images
        public async Task<PagedResult<FullProductDTO>> GetAllProductsWithPrimaryImagePagedAsync(int pageNumber, int pageSize, int? categoryId = null)
        {
            var result = await _productsDAL.GetAllProductsWithPrimaryImagePagedAsync(pageNumber, pageSize, categoryId);
            return new PagedResult<FullProductDTO>
            {
                Items = result.Products,
                TotalCount = result.TotalCount
            };
        }

        // Paged method to get all products with all images
        public async Task<PagedResult<FullProductDTO>> GetAllProductsWithAllImagesPagedAsync(int pageNumber, int pageSize, int? categoryId = null)
        {
            var result = await _productsDAL.GetAllProductsWithAllImagesPagedAsync(pageNumber, pageSize, categoryId);
            return new PagedResult<FullProductDTO>
            {
                Items = result.Products,
                TotalCount = result.TotalCount
            };
        }

        // Paged and search method to get all products with primary images
        public async Task<PagedResult<FullProductDTO>> GetAllProductsWithPrimaryImagePagedWithSearchAsync(int pageNumber, int pageSize, int? categoryId = null, string? searchTerm = null)
        {
            var result = await _productsDAL.GetAllProductsWithPrimaryImagePagedWithSearchAsync(pageNumber, pageSize, categoryId, searchTerm);
            return new PagedResult<FullProductDTO>
            {
                Items = result.Products,
                TotalCount = result.TotalCount
            };
        }

        // Paged and search method to get all products with all images
        public async Task<PagedResult<FullProductDTO>> GetAllProductsWithAllImagesPagedWithSearchAsync(int pageNumber, int pageSize, int? categoryId = null, string? searchTerm = null)
        {
            var result = await _productsDAL.GetAllProductsWithAllImagesPagedWithSearchAsync(pageNumber, pageSize, categoryId, searchTerm);
            return new PagedResult<FullProductDTO>
            {
                Items = result.Products,
                TotalCount = result.TotalCount
            };
        }

        // Filtered and paged method to get all products with primary images
        public async Task<(List<FullProductDTO> Products, int TotalCount)> GetProductsPaginatedWithFiltersPrimaryImageAsync(
            int pageNumber, int pageSize, int? productID, string? productName, int? initialPrice, int? sellingPrice, string? description,
            int? categoryId, int? quantity, bool? isActive)
        {
            return await _productsDAL.GetProductsPaginatedWithFiltersPrimaryImageAsync(pageNumber, pageSize, productID, productName, initialPrice,
                sellingPrice, description, categoryId, quantity, isActive);
        }

        // Filtered and paged method to get all products with all images
        public async Task<(List<FullProductDTO> Products, int TotalCount)> GetProductsPaginatedWithFiltersAllImagesAsync(
            int pageNumber, int pageSize, int? productID, string? productName, int? initialPrice, int? sellingPrice, string? description,
            int? categoryId, int? quantity, bool? isActive)
        {
            return await _productsDAL.GetProductsPaginatedWithFiltersAllImagesAsync(pageNumber, pageSize, productID, productName, initialPrice,
                sellingPrice, description, categoryId, quantity, isActive);
        }

        // Get a product by ID with its primary image
        public async Task<FullProductDTO> GetProductWithPrimaryImageByProductIDAsync(int id)
        {
            return await _productsDAL.GetProductWithPrimaryImageByProductIDAsync(id);
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
                return null;
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

        public async Task<bool> IsProductExistsByProductNameAsync(string name)
        {
            return await _productsDAL.IsProductExistsByProductNameAsync(name);
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