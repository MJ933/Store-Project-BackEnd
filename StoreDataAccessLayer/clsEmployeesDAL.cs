using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Npgsql;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dapper;

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

        public List<EmployeeDTO> GetAllEmployees()
        {
            var list = new List<EmployeeDTO>();
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand("SELECT * FROM Employees", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new EmployeeDTO(
                                reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                                reader.GetString(reader.GetOrdinal("UserName")),
                                reader.GetString(reader.GetOrdinal("Password")),
                                reader.GetString(reader.GetOrdinal("Email")),
                                reader.GetString(reader.GetOrdinal("Phone")),
                                reader.GetString(reader.GetOrdinal("Role")),
                                reader.GetBoolean(reader.GetOrdinal("IsActive"))
                            ));
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
            return list;
        }



        public (List<EmployeeDTO> EmployeesList, int TotalCount) GetEmployeesPaginatedWithFilters(
    int pageNumber, int pageSize, int? employeeID, string? userName, string? email,
    string? phone, string? role, bool? isActive)
        {
            var employeeList = new List<EmployeeDTO>();
            int totalCount = 0;

            try
            {
                int offset = (pageNumber - 1) * pageSize;

                // Build the base query and conditions
                var conditions = new List<string>();

                if (employeeID.HasValue)
                    conditions.Add("e.employeeID = @EmployeeID");
                if (!string.IsNullOrEmpty(userName))
                    conditions.Add("e.userName ILIKE @UserName");
                if (!string.IsNullOrEmpty(email))
                    conditions.Add("e.email ILIKE @Email");
                if (!string.IsNullOrEmpty(phone))
                    conditions.Add("e.phone ILIKE @Phone");
                if (!string.IsNullOrEmpty(role))
                    conditions.Add("e.role ILIKE @Role");
                if (isActive.HasValue)
                    conditions.Add("e.isActive = @IsActive");

                // Build the count query
                string countQuery = "SELECT COUNT(*) FROM Employees e";
                if (conditions.Any())
                    countQuery += " WHERE " + string.Join(" AND ", conditions);

                // Parameters for count query
                var parameters = new
                {
                    EmployeeID = employeeID,
                    UserName = !string.IsNullOrEmpty(userName) ? $"%{userName}%" : null,
                    Email = !string.IsNullOrEmpty(email) ? $"%{email}%" : null,
                    Phone = !string.IsNullOrEmpty(phone) ? $"%{phone}%" : null,
                    Role = !string.IsNullOrEmpty(role) ? $"%{role}%" : null,
                    IsActive = isActive
                };

                // Execute the count query
                using var conn = _dataSource.OpenConnection();
                totalCount = conn.ExecuteScalar<int>(countQuery, parameters);

                // Build the employee query
                string employeesQuery = "SELECT * FROM Employees e";
                if (conditions.Any())
                    employeesQuery += " WHERE " + string.Join(" AND ", conditions);
                employeesQuery += " ORDER BY e.EmployeeID LIMIT @PageSize OFFSET @Offset";

                // Parameters for employee query
                var employeeParams = new
                {
                    PageSize = pageSize,
                    Offset = offset,
                    EmployeeID = employeeID,
                    UserName = !string.IsNullOrEmpty(userName) ? $"%{userName}%" : null,
                    Email = !string.IsNullOrEmpty(email) ? $"%{email}%" : null,
                    Phone = !string.IsNullOrEmpty(phone) ? $"%{phone}%" : null,
                    Role = !string.IsNullOrEmpty(role) ? $"%{role}%" : null,
                    IsActive = isActive
                };

                // Execute the employee query
                employeeList = conn.Query<EmployeeDTO>(employeesQuery, employeeParams).AsList();

                return (employeeList, totalCount);
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

        public List<EmployeeDTO> GetEmployeesByRole(string role)
        {
            var list = new List<EmployeeDTO>();
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand("SELECT * FROM Employees WHERE Role = @role", conn))
                {
                    cmd.Parameters.AddWithValue("@role", role.ToLower());
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new EmployeeDTO(
                                reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                                reader.GetString(reader.GetOrdinal("UserName")),
                                reader.GetString(reader.GetOrdinal("Password")),
                                reader.GetString(reader.GetOrdinal("Email")),
                                reader.GetString(reader.GetOrdinal("Phone")),
                                reader.GetString(reader.GetOrdinal("Role")),
                                reader.GetBoolean(reader.GetOrdinal("IsActive"))
                            ));
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
            return list;
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