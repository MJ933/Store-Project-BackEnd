using System;
using System.Collections; // For Hashtable
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.IO;
using Microsoft.AspNetCore.Http; // For IFormFile
using Newtonsoft.Json;
using Npgsql;
using Imagekit;
using Imagekit.Models;
using Imagekit.Sdk;
using Microsoft.Extensions.Configuration;

namespace StoreDataAccessLayer
{
    // DTO for representing an image in the application
    public class ImageDTO
    {
        public ImageDTO() { }

        // Parameterized constructor
        public ImageDTO(int imageID, int productID, bool isPrimary, IFormFile file)
        {
            ImageID = imageID;
            ProductID = productID;
            IsPrimary = isPrimary;
            File = file;
        }

        public int ImageID { get; set; }

        [DefaultValue(2)]
        [Required(ErrorMessage = "Product ID is required!")]
        public int ProductID { get; set; }

        [DefaultValue(false)]
        [Required]
        public bool IsPrimary { get; set; }

        public IFormFile File { get; set; } // Represents the image file
    }

    // DTO for representing an image in the database
    public class ImageDatabaseDTO
    {
        public ImageDatabaseDTO() { }

        // Parameterized constructor
        public ImageDatabaseDTO(int imageID, string url, int productID, bool isPrimary, string fileId)
        {
            ImageID = imageID;
            ImageURL = url;
            ProductID = productID;
            IsPrimary = isPrimary;
            FileID = fileId;
        }

        public int ImageID { get; set; }

        [DefaultValue(2)]
        [Required(ErrorMessage = "Product ID is required!")]
        public int ProductID { get; set; }

        [DefaultValue(false)]
        [Required]
        public bool IsPrimary { get; set; }

        public string ImageURL { get; set; } // URL of the image in ImageKit

        public string FileID { get; set; } // Unique file ID in ImageKit
    }


    public class ImageKitSettings
    {
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public string UrlEndpoint { get; set; }
    }


    public class clsImagesDAL
    {
        // Initialize ImageKit SDK
        private readonly ImagekitClient _imagekit;
        private readonly NpgsqlDataSource _dataSource;

        public clsImagesDAL(NpgsqlDataSource dataSource, IConfiguration configuration)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));

            var imageKitSettings = configuration.GetSection("ImageKit").Get<ImageKitSettings>();
            if (imageKitSettings == null)
            {
                throw new ArgumentNullException(nameof(imageKitSettings), "ImageKit settings are missing in the configuration.");
            }

            _imagekit = new ImagekitClient(
                imageKitSettings.PublicKey,
                imageKitSettings.PrivateKey,
                imageKitSettings.UrlEndpoint
            );
        }
        public static string FileID { get; set; }

        // Uploads an image to ImageKit and returns the URL
        public string UploadImageToImageKit(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("File is null or empty.");
                }

                if (file.Length > 50 * 1024 * 1024) // 50MB limit
                {
                    throw new ArgumentException("File size exceeds the limit of 50MB.");
                }

                using (var stream = file.OpenReadStream())
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    byte[] fileBytes = memoryStream.ToArray();

                    var customMetadata = new Hashtable
                    {
                        { "isPrimary", false }
                    };

                    var uploadRequest = new FileCreateRequest
                    {
                        file = fileBytes,
                        fileName = file.FileName ?? "default.png",
                        folder = "/Products/",
                        customMetadata = customMetadata
                    };

                    var response = _imagekit.Upload(uploadRequest);

                    FileID = response.fileId;
                    Console.WriteLine($"ImageKit Response: {JsonConvert.SerializeObject(response)}");

                    if (response == null || string.IsNullOrEmpty(response.url))
                    {
                        throw new Exception("ImageKit upload failed: No URL returned.");
                    }

                    return response.url;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading image to ImageKit: {ex.Message}");
                throw new Exception("Failed to upload image to ImageKit. See logs for details.", ex);
            }
        }

        // Creates a new image record in the database
        public int CreateImage(ImageDTO imageDTO)
        {
            var imageUrl = UploadImageToImageKit(imageDTO.File);

            using (var conn = _dataSource.OpenConnection())
            {
                var sql = @"
            INSERT INTO Images (ImageURL, ProductID, IsPrimary, FileID)
            VALUES (@ImageURL, @ProductID, @IsPrimary, @FileID)
            RETURNING ImageID;";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ImageURL", NpgsqlTypes.NpgsqlDbType.Text, imageUrl);
                    cmd.Parameters.AddWithValue("@ProductID", imageDTO.ProductID);
                    cmd.Parameters.AddWithValue("@IsPrimary", imageDTO.IsPrimary);
                    cmd.Parameters.AddWithValue("@FileID", FileID);

                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        // Retrieves all images from the database
        public List<ImageDatabaseDTO> GetAllImages()
        {
            var images = new List<ImageDatabaseDTO>();
            try
            {
                using (var conn = _dataSource.OpenConnection())
                {
                    var sql = "SELECT * FROM Images i WHERE i.isPrimary = true;";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var image = new ImageDatabaseDTO(
                                imageID: reader.GetInt32(0),
                                url: reader.GetString(1),
                                productID: reader.GetInt32(2),
                                isPrimary: reader.GetBoolean(3),
                                fileId: reader.GetString(4)
                            );
                            images.Add(image);
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return images;
        }

        // Retrieves an image by its ID
        public ImageDatabaseDTO GetImageByImageID(int imageID)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                {
                    var sql = "SELECT * FROM Images WHERE ImageID = @ImageID LIMIT 1;";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("ImageID", imageID);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new ImageDatabaseDTO(
                                    imageID: reader.GetInt32(0),
                                    url: reader.GetString(1),
                                    productID: reader.GetInt32(2),
                                    isPrimary: reader.GetBoolean(3),
                                    fileId: reader.GetString(4)
                                );
                            }
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return null; // Image not found
        }

        // Retrieves the primary image for a product
        public ImageDatabaseDTO GetPrimaryImageByProductID(int productID)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                {
                    var sql = "SELECT * FROM Images i WHERE i.productid = @ProductID AND i.isprimary = true LIMIT 1;";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("ProductID", productID);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new ImageDatabaseDTO(
                                    imageID: reader.GetInt32(0),
                                    url: reader.GetString(1),
                                    productID: reader.GetInt32(2),
                                    isPrimary: reader.GetBoolean(3),
                                    fileId: reader.GetString(4)
                                );
                            }
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return null; // Image not found
        }

        // Updates an image record in the database
        public bool UpdateImage(ImageDTO imageDTO)
        {
            try
            {
                string oldFileID = null;
                using (var conn = _dataSource.OpenConnection())
                {
                    var sql = "SELECT FileID FROM Images WHERE ImageID = @ImageID LIMIT 1;";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("ImageID", imageDTO.ImageID);
                        oldFileID = cmd.ExecuteScalar()?.ToString();
                    }
                }

                if (!string.IsNullOrEmpty(oldFileID))
                {
                    _imagekit.DeleteFile(oldFileID);
                }

                var newImageUrl = UploadImageToImageKit(imageDTO.File);

                using (var conn = _dataSource.OpenConnection())
                {
                    var sql = @"
                UPDATE Images
                SET ImageURL = @ImageURL, ProductID = @ProductID, IsPrimary = @IsPrimary, FileID = @FileID
                WHERE ImageID = @ImageID;";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("ImageID", imageDTO.ImageID);
                        cmd.Parameters.AddWithValue("ImageURL", NpgsqlTypes.NpgsqlDbType.Text, newImageUrl);
                        cmd.Parameters.AddWithValue("ProductID", imageDTO.ProductID);
                        cmd.Parameters.AddWithValue("IsPrimary", imageDTO.IsPrimary);
                        cmd.Parameters.AddWithValue("@FileID", FileID);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0; // Return true if the update was successful
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        // Deletes an image from the database and ImageKit
        public bool DeleteImage(int imageID)
        {
            try
            {
                string oldFileID = null;
                using (var conn = _dataSource.OpenConnection())
                {
                    var sql = "SELECT FileID FROM Images WHERE ImageID = @ImageID LIMIT 1;";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("ImageID", imageID);
                        oldFileID = cmd.ExecuteScalar()?.ToString();
                    }
                }

                if (!string.IsNullOrEmpty(oldFileID))
                {
                    _imagekit.DeleteFile(oldFileID);
                }

                using (var conn = _dataSource.OpenConnection())
                {
                    var sql = "DELETE FROM Images WHERE ImageID = @ImageID;";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("ImageID", imageID);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0; // Return true if the delete was successful
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        // Checks if an image exists by its ID
        public bool IsImageExistsByID(int imageID)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                {
                    var sql = "SELECT 1 FROM Images WHERE ImageID = @ImageID LIMIT 1;";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("ImageID", imageID);

                        var result = cmd.ExecuteScalar();
                        return result != null; // Return true if the image exists
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }



        // Sets the IsPrimary flag of an image to true in the database
        public bool UpdateIsPrimaryState(int imageID, bool primaryState)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                {
                    var sql = @"
                        UPDATE Images
                        SET IsPrimary = @primaryState
                        WHERE ImageID = @ImageID;";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("ImageID", imageID);
                        cmd.Parameters.AddWithValue("primaryState", primaryState);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0; // Return true if the update was successful
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}