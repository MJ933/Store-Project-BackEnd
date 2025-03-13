using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Npgsql;
using Dapper;
using System.Diagnostics;

namespace StoreDataAccessLayer
{
    public class CustomerDTO
    {
        public CustomerDTO() { }

        public CustomerDTO(int customerID, string firstName, string lastName, string email, string phone, DateTime registeredAt, bool isActive, string password)
        {
            CustomerID = customerID;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            Phone = phone;
            RegisteredAt = registeredAt;
            IsActive = isActive;
            Password = password;
        }

        public int CustomerID { get; set; }

        [Required(ErrorMessage = "First Name Is Required!")]
        [StringLength(100, ErrorMessage = "The Name cannot exceed 100 characters")]
        [DefaultValue("First Name")]
        public string FirstName { get; set; }

        [StringLength(100, ErrorMessage = "The Name cannot exceed 100 characters")]
        [DefaultValue("Last Name")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email Is Required!")]
        [StringLength(100, ErrorMessage = "The Email cannot exceed 100 characters")]
        [DefaultValue("Email Value")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone Number Is Required!")]
        [StringLength(20, ErrorMessage = "The Phone Number cannot exceed 20 characters")]
        [DefaultValue("999")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Register date Is Required!")]
        public DateTime RegisteredAt { get; set; }

        [DefaultValue(true)]
        public bool IsActive { get; set; }

        [Required(ErrorMessage = "Password Is Required!")]
        [StringLength(255, ErrorMessage = "The Password cannot exceed 255 characters")]
        [DefaultValue("1")]
        public string Password { get; set; }
    }


    public class clsCustomersDAL
    {
        private readonly NpgsqlDataSource _dataSource;

        public clsCustomersDAL(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        }

        public async Task<(List<CustomerDTO> CustomersList, int TotalCount)> GetCustomersPaginatedWithFilters(
            int pageNumber,
            int pageSize,
            int? customerID,
            string? firstName,
            string? lastName,
            string? email,
            string? phone,
            DateTime? registeredAt,
            bool? isActive)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var parameters = new
                {
                    p_page_number = pageNumber,
                    p_page_size = pageSize,
                    p_customer_id = customerID,
                    p_first_name = firstName,
                    p_last_name = lastName,
                    p_email = email,
                    p_phone = phone,
                    p_registered_at = registeredAt,
                    p_is_active = isActive,
                };

                string query = @"
                    SELECT *
                    FROM fn_get_customers_paginated_with_filters(
                        @p_page_number, @p_page_size, @p_customer_id, @p_first_name, @p_last_name,
                        @p_email, @p_phone, @p_registered_at, @p_is_active)";

                var result = await conn.QueryAsync(query, parameters);

                var customersList = result.Select(row => new CustomerDTO
                {
                    CustomerID = row.customer_id,
                    FirstName = row.first_name,
                    LastName = row.last_name,
                    Email = row.email,
                    Phone = row.phone,
                    RegisteredAt = row.registered_at,
                    IsActive = row.is_active,
                }).ToList();

                int totalCount = result.Any() ? (int)result.First().total_count : 0;

                return (customersList, totalCount);
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
                throw;
            }
        }

        public async Task<CustomerDTO> GetCustomerByCustomerID(int id)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var query = "SELECT * FROM Customers WHERE CustomerID = @customerID LIMIT 1";
                var customer = await conn.QueryFirstOrDefaultAsync<CustomerDTO>(query, new { customerID = id });
                return customer ?? new CustomerDTO();
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

        public async Task<CustomerDTO> GetCustomerByCustomerPhone(string phone)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var query = "SELECT * FROM Customers WHERE Phone = @phone LIMIT 1";
                var customer = await conn.QueryFirstOrDefaultAsync<CustomerDTO>(query, new { phone = phone });
                return customer ?? new CustomerDTO();
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

        public async Task<CustomerDTO> GetCustomerByCustomerEmail(string email)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var query = "SELECT * FROM Customers WHERE Email = @email LIMIT 1";
                var customer = await conn.QueryFirstOrDefaultAsync<CustomerDTO>(query, new { email = email });
                return customer ?? new CustomerDTO();
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

        public async Task<int> AddCustomer(CustomerDTO dto)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var query = @"
                    INSERT INTO Customers (FirstName, LastName, Email, Phone, RegisteredAt, IsActive, Password)
                    VALUES (@FirstName, @LastName, @Email, @Phone, @RegisteredAt, @IsActive, @Password)
                    RETURNING CustomerID";

                var queryParams = new
                {
                    dto.FirstName,
                    dto.LastName,
                    dto.Email,
                    dto.Phone,
                    RegisteredAt = DateTime.Now,
                    dto.IsActive,
                    dto.Password
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

        public async Task<bool> UpdateCustomer(CustomerDTO dto)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var query = @"
                    UPDATE Customers 
                    SET FirstName = @FirstName, LastName = @LastName, Email = @Email,
                        Phone = @Phone, IsActive = @IsActive, Password = @Password
                    WHERE CustomerID = @CustomerID";

                var queryParams = new
                {
                    dto.CustomerID,
                    dto.FirstName,
                    dto.LastName,
                    dto.Email,
                    dto.Phone,
                    dto.IsActive,
                    dto.Password
                };

                int rowsAffected = await conn.ExecuteAsync(query, queryParams);
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

        public async Task<bool> DeleteCustomerByCustomerID(int id)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var query = "UPDATE Customers SET IsActive = false WHERE CustomerID = @customerID";
                int rowsAffected = await conn.ExecuteAsync(query, new { customerID = id });
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

        public async Task<bool> IsCustomerExistsByCustomerID(int id)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var query = "SELECT 1 FROM Customers WHERE CustomerID = @customerID LIMIT 1";
                var result = await conn.ExecuteScalarAsync<int?>(query, new { customerID = id });
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

        public async Task<bool> IsCustomerExistsByCustomerPhone(string phone)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var query = "SELECT 1 FROM Customers WHERE Phone = @Phone LIMIT 1";
                var result = await conn.ExecuteScalarAsync<int?>(query, new { Phone = phone });
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

        public async Task<bool> IsCustomerExistsByCustomerEmail(string email)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var query = "SELECT 1 FROM Customers WHERE Email = @Email LIMIT 1";
                var result = await conn.ExecuteScalarAsync<int?>(query, new { Email = email });
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

        public async Task<CustomerDTO> GetCustomerByEmailAndPassword(string email, string password)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var query = "SELECT * FROM Customers WHERE Email = @Email AND Password = @Password LIMIT 1";
                var customer = await conn.QueryFirstOrDefaultAsync<CustomerDTO>(query, new { Email = email, Password = password });
                return customer ?? new CustomerDTO();
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

        public async Task<CustomerDTO> GetCustomerByPhoneAndPassword(string phone, string password)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                var query = "SELECT * FROM Customers WHERE Phone = @Phone AND Password = @Password LIMIT 1";
                var customer = await conn.QueryFirstOrDefaultAsync<CustomerDTO>(query, new { Phone = phone, Password = password });
                return customer ?? new CustomerDTO();
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
    }
}
