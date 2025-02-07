using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StoreBusinessLayer;
using StoreDataAccessLayer;
using System.Collections.Generic;

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

        [HttpGet("GetAll", Name = "GetAllCustomers")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public ActionResult<IEnumerable<CustomerDTO>> GetAllCustomers()
        {
            var customers = _customersBL.GetAllCustomers();
            if (customers.Count == 0)
                return NotFound("No customers found.");
            return Ok(customers);
        }



        [HttpGet("GetCustomersPaginatedWithFilters", Name = "GetCustomersPaginatedWithFilters")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public ActionResult<IEnumerable<CustomerDTO>> GetCustomersPaginatedWithFilters(
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
            // Call the DAL method with the provided filters
            var result = _customersBL.GetCustomersPaginatedWithFilters(
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

            // Check if any customers were found
            if (result.CustomersList.Count == 0)
            {
                return NotFound("No customers found matching the specified filters.");
            }

            // Return the paginated list of customers
            return Ok(new
            {
                TotalCount = result.TotalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Customers = result.CustomersList
            });
        }




        [HttpGet("GetCustomerByID/{id}", Name = "GetCustomerByCustomerID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public ActionResult<CustomerDTO> GetCustomerByID([FromRoute] int id)
        {
            if (id < 1)
                return BadRequest("Invalid customer ID.");
            var customer = _customersBL.GetCustomerByCustomerID(id);
            if (customer == null)
                return NotFound($"Customer with ID {id} not found.");
            return Ok(customer.DTO);
        }

        [HttpGet("GetCustomerByPhone/{phone}", Name = "GetCustomerByPhone")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize]
        public ActionResult<CustomerDTO> GetCustomerByPhone([FromRoute] string phone)
        {
            if (string.IsNullOrEmpty(phone))
                return BadRequest("Phone number is required.");
            var customer = _customersBL.GetCustomerByCustomerPhone(phone);
            if (customer == null)
                return NotFound($"Customer with phone {phone} not found.");
            return Ok(customer.DTO);
        }

        [HttpGet("GetCustomerByEmail/{email}", Name = "GetCustomerByEmail")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize]
        public ActionResult<CustomerDTO> GetCustomerByEmail([FromRoute] string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email is required.");
            var customer = _customersBL.GetCustomerByCustomerEmail(email);
            if (customer == null)
                return NotFound($"Customer with email {email} not found.");
            return Ok(customer.DTO);
        }

        [HttpPost("Create", Name = "AddCustomer")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public ActionResult<CustomerDTO> AddCustomer([FromBody] CustomerDTO newCustomerDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (_customersBL.IsCustomerExistsByCustomerEmail(newCustomerDTO.Email))
                return Conflict("Customer email already exists.");

            if (_customersBL.IsCustomerExistsByCustomerPhone(newCustomerDTO.Phone))
                return Conflict("Customer phone already exists.");

            var customerBL = new clsCustomersBL(newCustomerDTO);
            if (customerBL.Save())
            {
                return CreatedAtAction(nameof(GetCustomerByID), new { id = newCustomerDTO.CustomerID }, newCustomerDTO);
            }
            else
            {
                return BadRequest("Failed to add customer.");
            }
        }

        [HttpPut("Update/{id}", Name = "UpdateCustomer")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize]
        public ActionResult<CustomerDTO> UpdateCustomer([FromRoute] int id, [FromBody] CustomerDTO updatedCustomerDTO)
        {
            if (id < 1)
                return BadRequest("Invalid customer ID.");
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var customerBL = _customersBL.GetCustomerByCustomerID(id);
            if (customerBL == null)
                return NotFound($"There is no Customer with ID = {id}");

            updatedCustomerDTO.CustomerID = id;
            customerBL.DTO = updatedCustomerDTO;

            if (customerBL.Save())
            {
                return Ok(updatedCustomerDTO);
            }
            else
            {
                return BadRequest("Failed to update customer.");
            }
        }

        [HttpDelete("Delete/{id}", Name = "DeleteCustomer")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "admin")]
        public ActionResult DeleteCustomer([FromRoute] int id)
        {
            if (id < 1)
                return BadRequest("Invalid customer ID.");
            if (!_customersBL.IsCustomerExistsByCustomerID(id))
                return NotFound($"Customer with ID {id} not found.");

            if (_customersBL.DeleteCustomerByCustomerID(id))
            {
                return Ok($"Customer with ID {id} deleted successfully.");
            }
            else
            {
                return BadRequest("Failed to delete customer.");
            }
        }

        [HttpGet("GetCustomerByEmailAndPassword/{email}/{password}", Name = "GetCustomerByEmailAndPassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public ActionResult<CustomerDTO> GetCustomerByEmailAndPassword([FromRoute] string email, [FromRoute] string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return BadRequest("Email and password are required.");

            clsCustomersBL customer = _customersBL.GetCustomerByEmailAndPassword(email, password);
            if (customer == null)
                return NotFound($"No Customer found with the provided email and password.");

            return Ok(customer.DTO);
        }

        [HttpGet("GetCustomerByPhoneAndPassword/{phone}/{password}", Name = "GetCustomerByPhoneAndPassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public ActionResult<CustomerDTO> GetCustomerByPhoneAndPassword([FromRoute] string phone, [FromRoute] string password)
        {
            if (string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(password))
                return BadRequest("Phone and password are required.");

            clsCustomersBL customer = _customersBL.GetCustomerByPhoneAndPassword(phone, password);
            if (customer == null)
                return NotFound($"No Customer found with the provided phone and password.");

            return Ok(customer.DTO);
        }
    }
}