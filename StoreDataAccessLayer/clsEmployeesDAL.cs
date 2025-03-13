using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Npgsql;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dapper;
using System.Data;
using System.Diagnostics;

namespace StoreDataAccessLayer
{
    public class EmployeeDTO
    {
        public EmployeeDTO() { }

        public EmployeeDTO(int employeeID, string userName, string password, string email, string phone, string role, bool isActive)
        {
            EmployeeID = employeeID;
            UserName = userName;
            Password = password;
            Email = email;
            Phone = phone;
            Role = role;
            IsActive = isActive;
        }

        public int EmployeeID { get; set; }

        [Required(ErrorMessage = "Username Is Required!")]
        [StringLength(255, ErrorMessage = "The Username cannot exceed 255 characters")]
        [DefaultValue("Username")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Password Is Required!")]
        [StringLength(255, ErrorMessage = "The Password cannot exceed 255 characters")]
        [DefaultValue("Password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Email Is Required!")]
        [StringLength(255, ErrorMessage = "The Email cannot exceed 255 characters")]
        [DefaultValue("Email")]
        public string Email { get; set; }

        [StringLength(20, ErrorMessage = "The Phone Number cannot exceed 20 characters")]
        [DefaultValue("999")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Role Is Required!")]
        [StringLength(50, ErrorMessage = "The Role cannot exceed 50 characters")]
        [DefaultValue("admin")]
        public string Role { get; set; }

        [DefaultValue(true)]
        public bool IsActive { get; set; }
    }

    public class clsEmployeesDAL
    {
        private readonly NpgsqlDataSource _dataSource;

        public clsEmployeesDAL(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        }


        public async Task<(List<EmployeeDTO> EmployeesList, int TotalCount)> GetEmployeesPaginatedWithFilters(
            int pageNumber, int pageSize, int? employeeID, string? userName, string? email,
            string? phone, string? role, bool? isActive)
        {

            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var parameters = new
                {
                    p_page_number = pageNumber,
                    p_page_size = pageSize,
                    p_employee_id = employeeID,
                    p_user_name = userName,
                    p_email = email,
                    p_phone = phone,
                    p_role = role,
                    p_is_active = isActive
                };
                var query = @"
                SELECT *
                FROM fn_get_employees_paginated_with_filters(
                    p_page_number := @p_page_number,
                    p_page_size := @p_page_size,
                    p_employee_id := @p_employee_id,
                    p_user_name := @p_user_name,
                    p_email := @p_email,
                    p_phone := @p_phone,
                    p_role := @p_role,
                    p_is_active := @p_is_active
                )";

                var result = await conn.QueryAsync(query, parameters);

                var employeesList = result.Select(row => new EmployeeDTO
                {
                    EmployeeID = row.employeeid,
                    UserName = row.username,
                    Email = row.email,
                    Phone = row.phone,
                    Role = row.role,
                    IsActive = row.isactive
                }).ToList();
                // Extract the total count from the first row (all rows have the same total_count)
                int totalCount = result.Any() ? (int)result.First().total_count : 0;

                return (employeesList, totalCount);
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                throw;
            }
        }
        public async Task<EmployeeDTO> GetEmployeeByEmployeeID(int id)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var employee = await conn.QueryFirstOrDefaultAsync<EmployeeDTO>("SELECT * FROM Employees WHERE EmployeeID = @employeeID LIMIT 1");
                return employee ?? new EmployeeDTO();
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            return new EmployeeDTO();
        }

        public async Task<EmployeeDTO> GetEmployeeByUserName(string userName)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var employee = await conn.QueryFirstOrDefaultAsync<EmployeeDTO>("SELECT * FROM Employees WHERE UserName = @userName LIMIT 1", new { UserName = userName });
                return employee ?? new EmployeeDTO();
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            return new EmployeeDTO();
        }

        public async Task<EmployeeDTO> GetEmployeeByEmail(string email)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var employee = await conn.QueryFirstOrDefaultAsync<EmployeeDTO>("SELECT * FROM Employees WHERE Email = @Email LIMIT 1", new { Email = email });
                return employee ?? new EmployeeDTO();
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            return new EmployeeDTO();
        }

        public async Task<EmployeeDTO> GetEmployeeByPhone(string phone)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var employee = await conn.QueryFirstOrDefaultAsync<EmployeeDTO>("SELECT * FROM Employees WHERE Phone = @phone LIMIT 1", new { phone = phone });
                return employee ?? new EmployeeDTO();
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            return new EmployeeDTO();
        }

        public async Task<int> AddEmployee(EmployeeDTO dto)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var query = @"
                            INSERT INTO Employees (UserName, Password, Email, Phone, Role, IsActive) 
                            VALUES (@UserName, @Password, @Email, @Phone, @Role, @IsActive) 
                            RETURNING EmployeeID";
                var queryParams = new
                {
                    UserName = dto.UserName,
                    Password = dto.Password,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    Role = dto.Role.ToLower(),
                    IsActive = dto.IsActive
                };
                return await conn.ExecuteScalarAsync<int>(query, queryParams);
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            return 0;
        }

        public async Task<bool> UpdateEmployee(EmployeeDTO dto)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var query = @"UPDATE Employees SET UserName = @UserName, Password = @Password, Email = @Email,
                                Phone = @Phone, Role = @Role, IsActive = @IsActive 
                                WHERE EmployeeID = @EmployeeID";
                var queryParams = new
                {
                    EmployeeID = dto.EmployeeID,
                    UserName = dto.UserName,
                    Password = dto.Password,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    Role = dto.Role.ToLower(),
                    IsActive = dto.IsActive

                };
                var rowsAffected = await conn.ExecuteAsync(query, queryParams);
                return rowsAffected > 0;
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            return false;
        }

        public async Task<bool> DeleteEmployeeByEmployeeID(int id)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var rowsAffected = await conn.ExecuteAsync("UPDATE Employees SET IsActive = false WHERE EmployeeID = @employeeID", new { employeeID = id });
                return rowsAffected > 0;
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            return false;
        }

        public async Task<bool> IsEmployeeExistsByEmployeeID(int id)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var result = await conn.ExecuteScalarAsync<int>("SELECT 1 FROM Employees WHERE EmployeeID = @employeeID LIMIT 1", new { employeeID = id });
                return result > 0;
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            return false;
        }

        public async Task<bool> IsEmployeeExistsByUserName(string userName)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var result = await conn.ExecuteScalarAsync<int?>("SELECT 1 FROM Employees WHERE UserName = @userName LIMIT 1", new { userName = userName });
                return result.HasValue;
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            return false;
        }

        public async Task<EmployeeDTO> GetEmployeeByEmailAndPassword(string email, string password)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var result = await conn.QueryFirstOrDefaultAsync<EmployeeDTO>("SELECT * FROM Employees WHERE Email = @Email AND Password = @Password LIMIT 1", new { email = email, password = password });
                return result ?? new EmployeeDTO();
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            return new EmployeeDTO();
        }

        public async Task<EmployeeDTO> GetEmployeeByPhoneAndPassword(string phone, string password)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var result = await conn.QueryFirstOrDefaultAsync<EmployeeDTO>("SELECT * FROM Employees WHERE Phone = @Phone AND Password = @Password LIMIT 1", new { Phone = phone, password = password });
                return result ?? new EmployeeDTO();
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            return new EmployeeDTO();
        }


        public async Task<bool> IsEmployeeAdmin(int employeeID)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var result = await conn.ExecuteScalarAsync<int?>("SELECT 1 FROM Employees WHERE EmployeeID = @employeeID AND Role = lower('Admin') LIMIT 1", new { employeeID = employeeID });
                return result.HasValue;
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            return false;
        }

    }
}