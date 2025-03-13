using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StoreBusinessLayer;
using StoreDataAccessLayer;

namespace StoreAPI.Controllers
{
    [Route("API/EmployeesAPI")]
    [ApiController]
    public class EmployeesAPIController : ControllerBase
    {
        private readonly clsEmployeesBL _employeesBL;

        public EmployeesAPIController(clsEmployeesBL employeesBL)
        {
            _employeesBL = employeesBL;
        }

        [HttpGet("GetEmployeesPaginatedWithFilters", Name = "GetEmployeesPaginatedWithFilters")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public async Task<ActionResult<IEnumerable<EmployeeDTO>>> GetEmployeesPaginatedWithFilters(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? employeeID = null,
            [FromQuery] string? userName = null,
            [FromQuery] string? email = null,
            [FromQuery] string? phone = null,
            [FromQuery] string? role = null,
            [FromQuery] bool? isActive = null)
        {
            var result = await _employeesBL.GetEmployeesPaginatedWithFilters(pageNumber, pageSize, employeeID, userName, email, phone, role, isActive);
            if (result.EmployeesList.Count == 0)
                return NotFound("No Employees found matching the specified filters.");
            return Ok(new
            {
                TotalCount = result.TotalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Employees = result.EmployeesList
            });
        }

        [HttpGet("GetEmployeeByUserName/{userName}", Name = "GetEmployeeByUserName")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public async Task<ActionResult<EmployeeDTO>> GetEmployeeByUserName([FromRoute] string userName)
        {
            if (string.IsNullOrEmpty(userName))
                return BadRequest("Username cannot be empty.");
            var employee = await _employeesBL.GetEmployeeByUserName(userName);
            if (employee == null)
                return NotFound($"There is no employee with username {userName}");
            return Ok(employee);
        }

        [HttpGet("GetEmployeeByEmail/{email}", Name = "GetEmployeeByEmail")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public async Task<ActionResult<EmployeeDTO>> GetEmployeeByEmail([FromRoute] string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email cannot be empty.");
            var employee = await _employeesBL.GetEmployeeByEmail(email);
            if (employee == null)
                return NotFound($"There is no employee with email {email}");
            return Ok(employee);
        }

        [HttpGet("GetEmployeeByPhone/{phone}", Name = "GetEmployeeByPhone")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public async Task<ActionResult<EmployeeDTO>> GetEmployeeByPhone([FromRoute] string phone)
        {
            if (string.IsNullOrEmpty(phone))
                return BadRequest("Phone cannot be empty.");
            var employee = await _employeesBL.GetEmployeeByPhone(phone);
            if (employee == null)
                return NotFound($"There is no employee with phone {phone}");
            return Ok(employee);
        }

        [HttpPost("Create", Name = "AddEmployee")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<EmployeeDTO>> AddEmployee(EmployeeDTO newEmployeeDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            clsEmployeesBL employeeBL = new clsEmployeesBL(newEmployeeDTO);
            if (await employeeBL.Save())
            {
                return CreatedAtRoute("GetEmployeeByEmployeeID", new { id = employeeBL.DTO.EmployeeID }, newEmployeeDTO);
            }
            else
            {
                return BadRequest("Failed to add the employee.");
            }
        }

        [HttpPut("Update/{id}", Name = "UpdateEmployee")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<EmployeeDTO>> UpdateEmployee([FromRoute] int id, [FromBody] EmployeeDTO updatedEmployeeDTO)
        {
            if (id < 1)
                return BadRequest($"Not Accepted ID {id}");
            updatedEmployeeDTO.EmployeeID = id;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            clsEmployeesBL employeeBL = await _employeesBL.FindEmployeeByEmployeeID(id);
            if (employeeBL == null)
                return NotFound($"There is no employee with ID = {id}");
            employeeBL.DTO = updatedEmployeeDTO;

            if (await employeeBL.Save())
            {
                return Ok(updatedEmployeeDTO);
            }
            else
            {
                return BadRequest("Failed to update the employee.");
            }
        }

        [HttpDelete("Delete/{id}", Name = "DeleteEmployee")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> DeleteEmployee([FromRoute] int id)
        {
            if (id <= 0)
                return BadRequest($"Please enter a valid ID = {id}");
            if (!await _employeesBL.IsEmployeeExistsByEmployeeID(id))
                return NotFound($"There is no employee with ID = {id}");
            if (await _employeesBL.DeleteEmployeeByEmployeeID(id))
                return Ok($"The employee was deleted successfully with ID = {id}");
            else
                return StatusCode(500, "ERROR: The employee was not deleted. No rows were affected.");
        }

        [HttpGet("IsEmployeeAdmin/{employeeID}", Name = "IsEmployeeAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public async Task<ActionResult<bool>> IsEmployeeAdmin([FromRoute] int employeeID)
        {
            if (employeeID < 1)
                return BadRequest($"Not Accepted ID {employeeID}");

            bool isAdmin = await _employeesBL.IsEmployeeAdmin(employeeID);
            return Ok(isAdmin);
        }
    }
}