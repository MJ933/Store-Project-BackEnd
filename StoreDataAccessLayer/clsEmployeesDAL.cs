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


        public (List<EmployeeDTO> EmployeesList, int TotalCount) GetEmployeesPaginatedWithFilters(
            int pageNumber, int pageSize, int? employeeID, string? userName, string? email,
            string? phone, string? role, bool? isActive)
        {
            try
            {
                //MeasureExecutionTime();

                // Define the parameters for the stored function
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

                // Execute the stored function
                using var conn = _dataSource.OpenConnection();
                var result = conn.Query(@"
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
            )", parameters);

                // Map the result to EmployeeDTO objects
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


        // Method to measure execution time
        //public void MeasureExecutionTime()
        //{
        //    int pageNumber = 1;
        //    int pageSize = 100;
        //    int? employeeID = null;
        //    string? userName = null;
        //    string? email = null;
        //    string? phone = null;
        //    string? role = null;
        //    bool? isActive = null;

        //    // Measure execution time for the original method
        //    Stopwatch stopwatch1 = Stopwatch.StartNew();
        //    var result1 = GetEmployeesPaginatedWithFilters1(pageNumber, pageSize, employeeID, userName, email, phone, role, isActive);
        //    stopwatch1.Stop();
        //    Console.WriteLine($"Original Method Execution Time: {stopwatch1.ElapsedMilliseconds} ms");

        //    // Measure execution time for the updated method (using stored function)
        //    Stopwatch stopwatch2 = Stopwatch.StartNew();
        //    var result2 = GetEmployeesPaginatedWithFiltersUsingFunction(pageNumber, pageSize, employeeID, userName, email, phone, role, isActive);
        //    stopwatch2.Stop();
        //    Console.WriteLine($"Stored Function Method Execution Time: {stopwatch2.ElapsedMilliseconds} ms");
        //    Console.WriteLine("\n----------------------------------\n");

        //}

        public EmployeeDTO GetEmployeeByEmployeeID(int id)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand("SELECT * FROM Employees WHERE EmployeeID = @employeeID LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@employeeID", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new EmployeeDTO(
                                reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                                reader.GetString(reader.GetOrdinal("UserName")),
                                reader.GetString(reader.GetOrdinal("Password")),
                                reader.GetString(reader.GetOrdinal("Email")),
                                reader.GetString(reader.GetOrdinal("Phone")),
                                reader.GetString(reader.GetOrdinal("Role")),
                                reader.GetBoolean(reader.GetOrdinal("IsActive"))
                            );
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            return null;
        }

        public EmployeeDTO GetEmployeeByUserName(string userName)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand("SELECT * FROM Employees WHERE UserName = @userName LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@userName", userName);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new EmployeeDTO(
                                reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                                reader.GetString(reader.GetOrdinal("UserName")),
                                reader.GetString(reader.GetOrdinal("Password")),
                                reader.GetString(reader.GetOrdinal("Email")),
                                reader.GetString(reader.GetOrdinal("Phone")),
                                reader.GetString(reader.GetOrdinal("Role")),
                                reader.GetBoolean(reader.GetOrdinal("IsActive"))
                            );
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            return null;
        }

        public EmployeeDTO GetEmployeeByEmail(string email)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand("SELECT * FROM Employees WHERE Email = @email LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new EmployeeDTO(
                                reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                                reader.GetString(reader.GetOrdinal("UserName")),
                                reader.GetString(reader.GetOrdinal("Password")),
                                reader.GetString(reader.GetOrdinal("Email")),
                                reader.GetString(reader.GetOrdinal("Phone")),
                                reader.GetString(reader.GetOrdinal("Role")),
                                reader.GetBoolean(reader.GetOrdinal("IsActive"))
                            );
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            return null;
        }

        public EmployeeDTO GetEmployeeByPhone(string phone)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand("SELECT * FROM Employees WHERE Phone = @phone LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@phone", phone);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new EmployeeDTO(
                                reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                                reader.GetString(reader.GetOrdinal("UserName")),
                                reader.GetString(reader.GetOrdinal("Password")),
                                reader.GetString(reader.GetOrdinal("Email")),
                                reader.GetString(reader.GetOrdinal("Phone")),
                                reader.GetString(reader.GetOrdinal("Role")),
                                reader.GetBoolean(reader.GetOrdinal("IsActive"))
                            );
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            return null;
        }

        public int AddEmployee(EmployeeDTO dto)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO Employees (UserName, Password, Email, Phone, Role, IsActive) " +
                    "VALUES (@UserName, @Password, @Email, @Phone, @Role, @IsActive) RETURNING EmployeeID", conn))
                {
                    cmd.Parameters.AddWithValue("@UserName", dto.UserName);
                    cmd.Parameters.AddWithValue("@Password", dto.Password);
                    cmd.Parameters.AddWithValue("@Email", dto.Email);
                    cmd.Parameters.AddWithValue("@Phone", dto.Phone);
                    cmd.Parameters.AddWithValue("@Role", dto.Role.ToLower());
                    cmd.Parameters.AddWithValue("@IsActive", dto.IsActive);
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        return (int)result;
                    }
                }
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

        public bool UpdateEmployee(EmployeeDTO dto)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand(
                    "UPDATE Employees SET UserName = @UserName, Password = @Password, Email = @Email, " +
                    "Phone = @Phone, Role = @Role, IsActive = @IsActive WHERE EmployeeID = @EmployeeID", conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeID", dto.EmployeeID);
                    cmd.Parameters.AddWithValue("@UserName", dto.UserName);
                    cmd.Parameters.AddWithValue("@Password", dto.Password);
                    cmd.Parameters.AddWithValue("@Email", dto.Email);
                    cmd.Parameters.AddWithValue("@Phone", dto.Phone);
                    cmd.Parameters.AddWithValue("@Role", dto.Role.ToLower());
                    cmd.Parameters.AddWithValue("@IsActive", dto.IsActive);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
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

        public bool DeleteEmployeeByEmployeeID(int id)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand("UPDATE Employees SET IsActive = false WHERE EmployeeID = @employeeID", conn))
                {
                    cmd.Parameters.AddWithValue("@employeeID", id);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
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

        public bool IsEmployeeExistsByEmployeeID(int id)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand("SELECT 1 FROM Employees WHERE EmployeeID = @employeeID LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@employeeID", id);
                    var result = cmd.ExecuteScalar();
                    return result != null;
                }
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

        public bool IsEmployeeExistsByUserName(string userName)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand("SELECT 1 FROM Employees WHERE UserName = @userName LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@userName", userName);
                    var result = cmd.ExecuteScalar();
                    return result != null;
                }
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

        public EmployeeDTO GetEmployeeByEmailAndPassword(string email, string password)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand(
                    "SELECT * FROM Employees WHERE Email = @Email AND Password = @Password LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Password", password);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new EmployeeDTO(
                                reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                                reader.GetString(reader.GetOrdinal("UserName")),
                                reader.GetString(reader.GetOrdinal("Password")),
                                reader.GetString(reader.GetOrdinal("Email")),
                                reader.GetString(reader.GetOrdinal("Phone")),
                                reader.GetString(reader.GetOrdinal("Role")),
                                reader.GetBoolean(reader.GetOrdinal("IsActive"))
                            );
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            return null;
        }

        public EmployeeDTO GetEmployeeByPhoneAndPassword(string phone, string password)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand(
                    "SELECT * FROM Employees WHERE Phone = @Phone AND Password = @Password LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@Phone", phone);
                    cmd.Parameters.AddWithValue("@Password", password);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new EmployeeDTO(
                                reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                                reader.GetString(reader.GetOrdinal("UserName")),
                                reader.GetString(reader.GetOrdinal("Password")),
                                reader.GetString(reader.GetOrdinal("Email")),
                                reader.GetString(reader.GetOrdinal("Phone")),
                                reader.GetString(reader.GetOrdinal("Role")),
                                reader.GetBoolean(reader.GetOrdinal("IsActive"))
                            );
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            return null;
        }


        public bool IsEmployeeAdmin(int employeeID)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand("SELECT 1 FROM Employees WHERE EmployeeID = @employeeID AND Role = lower('Admin') LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@employeeID", employeeID);
                    var result = cmd.ExecuteScalar();
                    return result != null;
                }
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