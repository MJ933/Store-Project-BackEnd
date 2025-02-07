using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StoreBusinessLayer;
using StoreDataAccessLayer;

namespace StoreAPI.Controllers
{
    [Route("API/ImagesAPI")]
    [ApiController]
    public class ImagesAPIController : ControllerBase
    {
        private readonly clsImagesBL _imagesBL;
        private readonly IConfiguration _configuration;
        // Inject clsImagesDAL via constructor
        public ImagesAPIController(clsImagesBL imagesBL, IConfiguration configuration)
        {
            _imagesBL = imagesBL;
            _configuration = configuration;
        }

        [HttpGet("GetAll/", Name = "GetAllImages")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<ImageDatabaseDTO> GetAllImages()
        {
            List<ImageDatabaseDTO> imagesList = _imagesBL.GetAllImages();
            if (imagesList.Count == 0)
                return NotFound("There is no Images in the data base!");
            return Ok(imagesList);
        }

        [HttpGet("FindByImageID/{id}", Name = "GetImageByImageID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<ImageDatabaseDTO> GetImage([FromRoute] int id)
        {
            if (id < 1)
                return BadRequest($"Invalid ID: {id}");

            var imageObj = _imagesBL.FindImageByImageID(id, _configuration);
            if (imageObj == null)
                return NotFound($"There is no image with ID {id}");

            return Ok(imageObj.DTO);
        }

        [HttpPost("create/{id}/{isPrimary}", Name = "AddImage")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<ImageDTO> AddImage([FromRoute] int id = 1, [FromRoute] bool isPrimary = false, IFormFile imageFile = null)
        {
            // Validate the model
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ImageDTO newImageDTO = new ImageDTO
            {
                File = imageFile,
                ProductID = id,
                IsPrimary = isPrimary
            };

            // Check if the file is provided
            if (newImageDTO.File == null || newImageDTO.File.Length == 0)
            {
                return BadRequest("File is required.");
            }
            _imagesBL.DTO = newImageDTO;

            // Save the image to the database
            try
            {

                if (_imagesBL.Save())
                {
                    return CreatedAtRoute("GetImageByImageID", new { id = _imagesBL.DTO.ImageID }, newImageDTO);
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to save the image.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPut("Update/{id}/{isPrimary}", Name = "UpdateImage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<ImageDTO> UpdateImage([FromRoute] int id = 1, [FromRoute] bool isPrimary = false, IFormFile imageFile = null)
        {
            // Validate the ID
            if (id < 1)
                return BadRequest($"Invalid ID: {id}");

            // Validate the model state
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if the image exists
            var existingImage = _imagesBL.FindImageByImageID(id, _configuration);

            if (existingImage == null)
                return NotFound($"There is no image with ID {id}");

            // Check if a new image file is provided
            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest("File is required for updating the image.");
            }

            try
            {
                // Update the image DTO with the new URL and other properties
                ImageDTO updatedImageDTO = new ImageDTO
                {
                    ImageID = id,
                    ProductID = existingImage.DatabaseDTO.ProductID, // Retain the existing ProductID
                    IsPrimary = isPrimary,
                    File = imageFile
                };
                _imagesBL.DTO = updatedImageDTO;
                _imagesBL.Mode = existingImage.Mode;
                // Update the image in the database
                if (_imagesBL.Save())
                {
                    // Return the updated image DTO
                    var updatedImgObj = _imagesBL.FindImageByImageID(updatedImageDTO.ImageID, _configuration).DatabaseDTO;
                    return Ok(updatedImgObj);
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update the image in the database.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpDelete("Delete/{id}", Name = "DeleteImage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult DeleteImage([FromRoute] int id)
        {
            if (id <= 0)
                return BadRequest($"Invalid ID: {id}");

            if (!_imagesBL.IsImageExistsByID(id))
                return NotFound($"There is no image with ID {id}");

            if (_imagesBL.DeleteImage(id))
            {
                return Ok($"The image was deleted successfully with ID: {id}");
            }
            else
            {
                return StatusCode(500, "An error occurred while deleting the image.");
            }
        }

        [HttpPut("UpdateIsPrimaryState/{id}/{primaryState}", Name = "UpdateIsPrimaryState")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult SetImageAsPrimary([FromRoute] int id, [FromRoute] bool primaryState)
        {
            if (id <= 0)
                return BadRequest($"Invalid ID: {id}");

            if (!_imagesBL.IsImageExistsByID(id))
                return NotFound($"There is no image with ID {id}");

            if (_imagesBL.UpdateIsPrimaryState(id, primaryState))
            {
                return Ok($"The image was Updated successfully with ID: {id}");
            }
            else
            {
                return StatusCode(500, "An error occurred while Updating the image.");
            }
        }
    }
}