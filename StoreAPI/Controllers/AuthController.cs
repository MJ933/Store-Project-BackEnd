using Microsoft.AspNetCore.Mvc;
using StoreBusinessLayer;

namespace StoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly TokenService _tokenService;
        private readonly clsCustomersBL _customersBL;
        private readonly clsEmployeesBL _employeesBL;

        public AuthController(TokenService tokenService, clsCustomersBL customersBL, clsEmployeesBL employeesBL)
        {
            _tokenService = tokenService;
            _customersBL = customersBL;
            _employeesBL = employeesBL;
        }

        [HttpPost("LoginCustomerByEmail")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LoginCustomerByEmail([FromBody] LoginRequestByEmail request)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Email and password are required." });
            }

            var customer = await _customersBL.GetCustomerByEmailAndPassword(request.Email, request.Password);
            if (customer == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            // Generate token
            var token = _tokenService.GenerateToken(customer.DTO.CustomerID.ToString(), request.Email, null, "customer");
            return Ok(new { Token = token });
        }

        [HttpPost("LoginCustomerByPhone")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LoginCustomerByPhone([FromBody] LoginRequestByPhone request)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Phone number and password are required." });
            }

            var customer = await _customersBL.GetCustomerByPhoneAndPassword(request.Phone, request.Password);
            if (customer == null)
            {
                return Unauthorized(new { message = "Invalid phone number or password." });
            }

            // Generate token
            var token = _tokenService.GenerateToken(customer.DTO.CustomerID.ToString(), null, request.Phone, "customer");
            return Ok(new { Token = token });
        }

        [HttpPost("LoginEmployeeByEmail")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LoginEmployeeByEmail([FromBody] LoginRequestByEmail request)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Email and password are required." });
            }

            var employee = await _employeesBL.GetEmployeeByEmailAndPassword(request.Email, request.Password);
            if (employee == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            string userRole = employee.DTO.Role;

            // Generate token
            var token = _tokenService.GenerateToken(employee.DTO.EmployeeID.ToString(), employee.DTO.Email, null, userRole);
            return Ok(new { Token = token });
        }

        [HttpPost("LoginEmployeeByPhone")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LoginEmployeeByPhone([FromBody] LoginRequestByPhone request)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Phone number and password are required." });
            }

            var employee = await _employeesBL.GetEmployeeByPhoneAndPassword(request.Phone, request.Password);
            if (employee == null)
            {
                return Unauthorized(new { message = "Invalid phone number or password." });
            }

            string userRole = employee.DTO.Role;

            // Generate token
            var token = _tokenService.GenerateToken(employee.DTO.EmployeeID.ToString(), null, request.Phone, userRole);
            return Ok(new { Token = token });
        }
    }

    public class LoginRequestByEmail
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequestByPhone
    {
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
    }
}