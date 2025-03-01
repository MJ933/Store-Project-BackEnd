using StoreDataAccessLayer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StoreBusinessLayer
{
    public class clsCategoriesBL
    {
        public enum enMode { AddNew = 0, Update = 1 }
        public enMode Mode { get; set; }

        public CategoryDTO DTO { get; set; }

        private readonly clsCategoriesDAL _categoriesDAL;

        public clsCategoriesBL(CategoryDTO dto, enMode mode = enMode.AddNew)
        {
            this.DTO = dto;
            this.Mode = mode;
            _categoriesDAL = new clsCategoriesDAL(clsDataAccessSettingsDAL.CreateDataSource());
        }
        public clsCategoriesBL(clsCategoriesDAL CategoriesDAL)
        {
            _categoriesDAL = CategoriesDAL;
        }
        public List<CategoryDTO> GetAllCategories()
        {
            return _categoriesDAL.GetAllCategories();
        }


        public (List<CategoryDTO> CategoriesList, int TotalCount) GetCategoriesPaginatedWithFilters(
      int pageNumber, int pageSize, int? categoryID, string? categoryName, int? parentCategoryID, bool? isActive)
        {
            return _categoriesDAL.GetCategoriesPaginatedWithFilters(pageNumber, pageSize, categoryID, categoryName, parentCategoryID, isActive);
        }


        public async Task<List<CategoryDTO>> GetActiveCategoriesWithProductsAsync()
        {
            return await _categoriesDAL.GetActiveCategoriesWithProductsAsync();
        }

        public clsCategoriesBL FindCategoryByCategoryID(int categoryID)
        {
            CategoryDTO dto = _categoriesDAL.GetCategoryByCategoryID(categoryID);

            if (dto != null)
            {
                return new clsCategoriesBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }

        private bool _AddNewCategory()
        {
            int categoryID = _categoriesDAL.AddCategory(this.DTO);
            if (categoryID > 0)
            {
                this.DTO.CategoryID = categoryID;
                return true;
            }
            return false;
        }

        private bool _UpdateCategory()
        {
            return _categoriesDAL.UpdateCategory(this.DTO);
        }

        public bool Save()
        {
            switch (this.Mode)
            {
                case enMode.AddNew:
                    if (_AddNewCategory())
                    {
                        this.Mode = enMode.Update;
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                case enMode.Update:
                    return _UpdateCategory();

                default:
                    return false;
            }
        }

        public bool DeleteCategory(int categoryID)
        {
            return _categoriesDAL.DeleteCategory(categoryID);
        }

        public bool IsCategoryExistsByCategoryID(int categoryID)
        {
            return _categoriesDAL.IsCategoryExistsByCategoryID(categoryID);
        }

        public bool IsCategoryExistsByCategoryName(string name)
        {
            return _categoriesDAL.IsCategoryExistsByCategoryName(name);
        }
    }
}