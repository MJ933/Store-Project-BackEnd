using Microsoft.Extensions.Logging;
using StoreDataAccessLayer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StoreBusinessLayer
{
    public interface ICategoriesService
    {
        Task<List<CategoryDTO>> GetAllCategoriesAsync();
        Task<(List<CategoryDTO> CategoriesList, int TotalCount)> GetCategoriesPaginatedWithFiltersAsync(
            int pageNumber, int pageSize, int? categoryID, string? categoryName, int? parentCategoryID, bool? isActive);
        Task<List<CategoryDTO>> GetActiveCategoriesWithProductsAsync();
        Task<CategoryDTO?> GetCategoryByCategoryIDAsync(int categoryID);
        Task<bool> AddCategoryAsync();
        Task<bool> UpdateCategoryAsync();
        Task<bool> DeleteCategoryAsync(int categoryID);
        Task<bool> IsCategoryExistsByCategoryIDAsync(int categoryID);
        Task<bool> IsCategoryExistsByCategoryNameAsync(string name);
        CategoryDTO categoryDTO { get; set; }
    }

    public class CategoriesService : ICategoriesService
    {
        private readonly ICategoriesRepository _categoriesRepository;
        public CategoryDTO categoryDTO { get; set; }

        public CategoriesService(ICategoriesRepository categoriesRepository)
        {
            _categoriesRepository = categoriesRepository ?? throw new ArgumentNullException(nameof(categoriesRepository));
        }

        public async Task<List<CategoryDTO>> GetAllCategoriesAsync()
        {
            return await _categoriesRepository.GetAllCategoriesAsync();
        }

        public async Task<(List<CategoryDTO> CategoriesList, int TotalCount)> GetCategoriesPaginatedWithFiltersAsync(
            int pageNumber, int pageSize, int? categoryID, string? categoryName, int? parentCategoryID, bool? isActive)
        {
            return await _categoriesRepository.GetCategoriesPaginatedWithFiltersAsync(
                pageNumber, pageSize, categoryID, categoryName, parentCategoryID, isActive);
        }

        public async Task<List<CategoryDTO>> GetActiveCategoriesWithProductsAsync()
        {
            return await _categoriesRepository.GetActiveCategoriesWithProductsAsync();
        }

        public async Task<CategoryDTO?> GetCategoryByCategoryIDAsync(int categoryID)
        {
            return await _categoriesRepository.GetCategoryByCategoryIDAsync(categoryID);
        }

        public async Task<bool> AddCategoryAsync()
        {
            int insertedCategoryID = await _categoriesRepository.AddCategoryAsync(this.categoryDTO);
            if (insertedCategoryID > 0)
            {
                this.categoryDTO = new CategoryDTO(insertedCategoryID, this.categoryDTO.CategoryName,
                    this.categoryDTO.ParentCategoryID, this.categoryDTO.IsActive);
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateCategoryAsync()
        {
            return await _categoriesRepository.UpdateCategoryAsync(this.categoryDTO);
        }

        public async Task<bool> DeleteCategoryAsync(int categoryID)
        {
            return await _categoriesRepository.DeleteCategoryAsync(categoryID);
        }

        public async Task<bool> IsCategoryExistsByCategoryIDAsync(int categoryID)
        {
            return await _categoriesRepository.IsCategoryExistsByCategoryIDAsync(categoryID);
        }

        public async Task<bool> IsCategoryExistsByCategoryNameAsync(string name)
        {
            return await _categoriesRepository.IsCategoryExistsByCategoryNameAsync(name);
        }
    }
}