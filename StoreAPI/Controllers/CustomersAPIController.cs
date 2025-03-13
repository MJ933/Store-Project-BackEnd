using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StoreBusinessLayer;
using StoreDataAccessLayer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StoreAPI.Controllers
{
    [Route("API/CustomersAPI")]
    [ApiController]
    public class CustomersAPIController : ControllerBase
    {
        private readonly clsCustomersBL _customersBL;

        public CustomersAPIController(clsCustomersBL customersBL)
        {
            _customersBL = customersBL;
        }

        [HttpGet("GetCustomersPaginatedWithFilters", Name = "GetCustomersPaginatedWithFilters")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public async Task<ActionResult> GetCustomersPaginatedWithFilters(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? customerID = null,
            [FromQuery] string? firstName = null,
            [FromQuery] string? lastName = null,
            [FromQuery] string? email = null,
            [FromQuery] string? phone = null,
            [FromQuery] DateTime? registeredAt = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var result = await _customersBL.GetCustomersPaginatedWithFilters(
                    pageNumber,
                    pageSize,
                    customerID,
                    firstName,
                    lastName,
                    email,
                    phone,
                    registeredAt,
                    isActive
                );

                if (result.CustomersList.Count == 0)
                {
                    return NotFound("No customers found matching the specified filters.");
                }

                return Ok(new
                {
                    TotalCount = result.TotalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Customers = result.CustomersList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("GetCustomerByID/{id}", Name = "GetCustomerByCustomerID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public async Task<ActionResult<CustomerDTO>> GetCustomerByID([FromRoute] int id)
        {
            if (id < 1)
                return BadRequest("Invalid customer ID.");

            try
            {
                var customer = await _customersBL.GetCustomerByCustomerID(id);
                if (customer == null)
                    return NotFound($"Customer with ID {id} not found.");

                return Ok(customer.DTO);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("GetCustomerByPhone/{phone}", Name = "GetCustomerByPhone")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<ActionResult<CustomerDTO>> GetCustomerByPhone([FromRoute] string phone)
        {
            if (string.IsNullOrEmpty(phone))
                return BadRequest("Phone number is required.");

            try
            {
                var customer = await _customersBL.GetCustomerByCustomerPhone(phone);
                if (customer == null)
                    return NotFound($"Customer with phone {phone} not found.");

                return Ok(customer.DTO);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("GetCustomerByEmail/{email}", Name = "GetCustomerByEmail")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<ActionResult<CustomerDTO>> GetCustomerByEmail([FromRoute] string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email is required.");

            try
            {
                var customer = await _customersBL.GetCustomerByCustomerEmail(email);
                if (customer == null)
                    return NotFound($"Customer with email {email} not found.");

                return Ok(customer.DTO);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("Create", Name = "AddCustomer")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<CustomerDTO>> AddCustomer([FromBody] CustomerDTO newCustomerDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                if (await _customersBL.IsCustomerExistsByCustomerEmail(newCustomerDTO.Email))
                    return Conflict("Customer email already exists.");

                if (await _customersBL.IsCustomerExistsByCustomerPhone(newCustomerDTO.Phone))
                    return Conflict("Customer phone already exists.");

                var customerBL = new clsCustomersBL(newCustomerDTO);
                if (await customerBL.Save())
                {
                    return CreatedAtAction(nameof(GetCustomerByID), new { id = newCustomerDTO.CustomerID }, newCustomerDTO);
                }
                else
                {
                    return BadRequest("Failed to add customer.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPut("Update/{id}", Name = "UpdateCustomer")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<ActionResult<CustomerDTO>> UpdateCustomer([FromRoute] int id, [FromBody] CustomerDTO updatedCustomerDTO)
        {
            if (id < 1)
                return BadRequest("Invalid customer ID.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var customerBL = await _customersBL.GetCustomerByCustomerID(id);
                if (customerBL == null)
                    return NotFound($"There is no Customer with ID = {id}");

                updatedCustomerDTO.CustomerID = id;
                customerBL.DTO = updatedCustomerDTO;

                if (await customerBL.Save())
                {
                    return Ok(updatedCustomerDTO);
                }
                else
                {
                    return BadRequest("Failed to update customer.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [HttpDelete("Delete/{id}", Name = "DeleteCustomer")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> DeleteCustomer([FromRoute] int id)
        {
            if (id < 1)
                return BadRequest("Invalid customer ID.");

            try
            {
                if (!await _customersBL.IsCustomerExistsByCustomerID(id))
                    return NotFound($"Customer with ID {id} not found.");

                if (await _customersBL.DeleteCustomerByCustomerID(id))
                {
                    return Ok($"Customer with ID {id} deleted successfully.");
                }
                else
                {
                    return BadRequest("Failed to delete customer.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("GetCustomerByEmailAndPassword/{email}/{password}", Name = "GetCustomerByEmailAndPassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public async Task<ActionResult<CustomerDTO>> GetCustomerByEmailAndPassword([FromRoute] string email, [FromRoute] string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return BadRequest("Email and password are required.");

            try
            {
                var customer = await _customersBL.GetCustomerByEmailAndPassword(email, password);
                if (customer == null)
                    return NotFound($"No Customer found with the provided email and password.");

                return Ok(customer.DTO);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("GetCustomerByPhoneAndPassword/{phone}/{password}", Name = "GetCustomerByPhoneAndPassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public async Task<ActionResult<CustomerDTO>> GetCustomerByPhoneAndPassword([FromRoute] string phone, [FromRoute] string password)
        {
            if (string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(password))
                return BadRequest("Phone and password are required.");

            try
            {
                var customer = await _customersBL.GetCustomerByPhoneAndPassword(phone, password);
                if (customer == null)
                    return NotFound($"No Customer found with the provided phone and password.");

                return Ok(customer.DTO);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }
    }
}