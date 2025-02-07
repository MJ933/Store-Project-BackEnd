using Microsoft.Extensions.Configuration;
using StoreDataAccessLayer;
using System;
using System.Collections.Generic;

namespace StoreBusinessLayer
{
    public class clsImagesBL
    {
        public enum enMode { AddNew = 0, Update = 1 }
        public enMode Mode { get; set; } = enMode.AddNew;

        public ImageDatabaseDTO DatabaseDTO { get; set; }
        public ImageDTO DTO { get; set; }

        private readonly clsImagesDAL _imagesDAL;

        // Constructor for AddNew mode
        public clsImagesBL(ImageDatabaseDTO dto, IConfiguration configuration, enMode mode = enMode.AddNew)
        {
            this.DatabaseDTO = dto;
            this.Mode = mode;

            // Initialize the DAL instance
            _imagesDAL = new clsImagesDAL(clsDataAccessSettingsDAL.CreateDataSource(), configuration);
        }

        // Constructor for Update mode
        public clsImagesBL(ImageDTO dto, IConfiguration configuration, enMode mode = enMode.Update)
        {
            this.DTO = dto;
            this.Mode = mode;

            // Initialize the DAL instance
            _imagesDAL = new clsImagesDAL(clsDataAccessSettingsDAL.CreateDataSource(), configuration);
        }
        public clsImagesBL(clsImagesDAL imageDAL)
        {

            _imagesDAL = imageDAL;
        }

        // Get all images
        public List<ImageDatabaseDTO> GetAllImages()
        {
            // Create a DAL instance to call the method
            return _imagesDAL.GetAllImages();
        }

        // Find an image by ID
        public clsImagesBL FindImageByImageID(int imageID, IConfiguration configuration)
        {
            // Create a DAL instance to call the method
            ImageDatabaseDTO dto = _imagesDAL.GetImageByImageID(imageID);

            if (dto != null)
            {
                return new clsImagesBL(dto, configuration, enMode.Update);
            }
            else
            {
                return null;
            }
        }

        // Find an image by ProductID
        public clsImagesBL FindImageByProductID(int productID, IConfiguration configuration)
        {
            // Create a DAL instance to call the method
            ImageDatabaseDTO dto = _imagesDAL.GetPrimaryImageByProductID(productID);

            if (dto != null)
            {
                return new clsImagesBL(dto, configuration, enMode.Update);
            }
            else
            {
                return null;
            }
        }

        // Add a new image
        private bool _AddNewImage()
        {
            int imageID = _imagesDAL.CreateImage(this.DTO);
            if (imageID > 0)
            {
                this.DTO.ImageID = imageID;
                return true;
            }
            return false;
        }

        // Update an existing image
        private bool _UpdateImage()
        {
            return _imagesDAL.UpdateImage(this.DTO);
        }

        // Save the image (Add or Update)
        public bool Save()
        {
            switch (this.Mode)
            {
                case enMode.AddNew:
                    if (_AddNewImage())
                    {
                        this.Mode = enMode.Update;
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                case enMode.Update:
                    return _UpdateImage();

                default:
                    return false;
            }
        }

        // Delete an image by ID
        public bool DeleteImage(int imageID)
        {
            // Create a DAL instance to call the method
            return _imagesDAL.DeleteImage(imageID);
        }

        public bool UpdateIsPrimaryState(int imageID, bool primaryState)
        {
            return _imagesDAL.UpdateIsPrimaryState(imageID, primaryState);
        }

        // Check if an image exists by ID
        public bool IsImageExistsByID(int imageID)
        {
            // Create a DAL instance to call the method
            return _imagesDAL.IsImageExistsByID(imageID);
        }
    }
}