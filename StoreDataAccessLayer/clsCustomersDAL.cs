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

        public (List<CustomerDTO> CustomersList, int TotalCount) GetCustomersPaginatedWithFilters(
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
            //MeasureExecutionTime();
            try
            {
                int totalCount = 0;
                // Parameters for the stored function
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

                // Query to call the stored function
                string query = "SELECT * FROM fn_get_customers_paginated_with_filters(" +
                               "@p_page_number, @p_page_size, @p_customer_id, @p_first_name, @p_last_name, " +
                               "@p_email, @p_phone, @p_registered_at, @p_is_active)";

                // Execute the stored function
                using var conn = _dataSource.OpenConnection();
                var result = conn.Query(query, parameters);

                // Extract the total count from the first row
                //{{DapperRow, customer_id = '1', first_name = 'First11 Name', last_name = 'Last Name'
                //, email = 'a@a.a', phone = '999', registered_at = '1/2/2025 10:24:06 AM', is_active = 'True', total_count = '1000'}}
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
                if (result.Any())
                {
                    totalCount = (int)result.First().total_count; // Assuming TotalCount is a property in CustomerDTO
                }
                return (customersList, totalCount);

            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                throw;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}"); throw;

            }
        }








        //public void MeasureExecutionTime()
        //{
        //    int pageNumber = 1;
        //    int pageSize = 10;
        //    int? customerID = null;
        //    string? firstName = null;
        //    string? lastName = null;
        //    string? email = null;
        //    string? phone = null;
        //    DateTime? registeredAt = null;
        //    bool? isActive = null;

        //    // Measure execution time for the original method
        //    Stopwatch stopwatch1 = Stopwatch.StartNew();
        //    var result1 = GetCustomersPaginatedWithFiltersWithOutStoredMethodTest(pageNumber, pageSize, customerID, firstName, lastName, email, phone, registeredAt, isActive);
        //    stopwatch1.Stop();
        //    Console.WriteLine($"Original Method Execution Time: {stopwatch1.ElapsedMilliseconds} ms");

        //    // Measure execution time for the updated method (using stored function)
        //    Stopwatch stopwatch2 = Stopwatch.StartNew();
        //    var result2 = GetCustomersPaginatedWithFiltersTest(pageNumber, pageSize, customerID, firstName, lastName, email, phone, registeredAt, isActive);
        //    stopwatch2.Stop();
        //    Console.WriteLine($"Stored Function Method Execution Time: {stopwatch2.ElapsedMilliseconds} ms");
        //    Console.WriteLine("\n----------------------------------\n");

        //}

        public CustomerDTO GetCustomerByCustomerID(int id)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand("SELECT * FROM Customers WHERE CustomerID = @customerID LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@customerID", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new CustomerDTO(
                                reader.GetInt32(reader.GetOrdinal("CustomerID")),
                                reader.GetString(reader.GetOrdinal("FirstName")),
                                reader.GetString(reader.GetOrdinal("LastName")),
                                reader.GetString(reader.GetOrdinal("Email")),
                                reader.GetString(reader.GetOrdinal("Phone")),
                                reader.GetDateTime(reader.GetOrdinal("RegisteredAt")),
                                reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                reader.GetString(reader.GetOrdinal("Password"))
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

        public CustomerDTO GetCustomerByCustomerPhone(string phone)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand("SELECT * FROM Customers WHERE Phone = @phone LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@phone", phone);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new CustomerDTO(
                                reader.GetInt32(reader.GetOrdinal("CustomerID")),
                                reader.GetString(reader.GetOrdinal("FirstName")),
                                reader.GetString(reader.GetOrdinal("LastName")),
                                reader.GetString(reader.GetOrdinal("Email")),
                                reader.GetString(reader.GetOrdinal("Phone")),
                                reader.GetDateTime(reader.GetOrdinal("RegisteredAt")),
                                reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                reader.GetString(reader.GetOrdinal("Password"))
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

        public CustomerDTO GetCustomerByCustomerEmail(string email)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand("SELECT * FROM Customers WHERE Email = @email LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new CustomerDTO(
                                 reader.GetInt32(reader.GetOrdinal("CustomerID")),
                                 reader.GetString(reader.GetOrdinal("FirstName")),
                                 reader.GetString(reader.GetOrdinal("LastName")),
                                 reader.GetString(reader.GetOrdinal("Email")),
                                 reader.GetString(reader.GetOrdinal("Phone")),
                                 reader.GetDateTime(reader.GetOrdinal("RegisteredAt")),
                                 reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                 reader.GetString(reader.GetOrdinal("Password"))
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

        public int AddCustomer(CustomerDTO dto)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO Customers (FirstName, LastName, Email, Phone, RegisteredAt, IsActive, Password) " +
                    "VALUES (@FirstName, @LastName, @Email, @Phone, @RegisteredAt, @IsActive, @Password) RETURNING CustomerID", conn))
                {
                    cmd.Parameters.AddWithValue("@FirstName", dto.FirstName);
                    cmd.Parameters.AddWithValue("@LastName", dto.LastName);
                    cmd.Parameters.AddWithValue("@Email", dto.Email);
                    cmd.Parameters.AddWithValue("@Phone", dto.Phone);
                    cmd.Parameters.AddWithValue("@RegisteredAt", DateTime.Now);
                    cmd.Parameters.AddWithValue("@IsActive", dto.IsActive);
                    cmd.Parameters.AddWithValue("@Password", dto.Password);
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

        public bool UpdateCustomer(CustomerDTO dto)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand(
                    "UPDATE Customers SET FirstName = @FirstName, LastName = @LastName, Email = @Email, " +
                    "Phone = @Phone, IsActive = @IsActive, Password = @Password WHERE CustomerID = @CustomerID", conn))
                {
                    cmd.Parameters.AddWithValue("@CustomerID", dto.CustomerID);
                    cmd.Parameters.AddWithValue("@FirstName", dto.FirstName);
                    cmd.Parameters.AddWithValue("@LastName", dto.LastName);
                    cmd.Parameters.AddWithValue("@Email", dto.Email);
                    cmd.Parameters.AddWithValue("@Phone", dto.Phone);
                    cmd.Parameters.AddWithValue("@IsActive", dto.IsActive);
                    cmd.Parameters.AddWithValue("@Password", dto.Password);
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

        public bool DeleteCustomerByCustomerID(int id)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand("UPDATE Customers SET IsActive = false WHERE CustomerID = @customerID", conn))
                {
                    cmd.Parameters.AddWithValue("@customerID", id);
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

        public bool IsCustomerExistsByCustomerID(int id)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand("SELECT 1 FROM Customers WHERE CustomerID = @customerID LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@customerID", id);
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


        public bool IsCustomerExistsByCustomerPhone(string phone)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand("SELECT 1 FROM Customers WHERE Phone = @Phone LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@Phone", phone);
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

        public bool IsCustomerExistsByCustomerEmail(string email)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand("SELECT 1 FROM Customers WHERE Email = @Email LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
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

        public CustomerDTO GetCustomerByEmailAndPassword(string email, string password)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand(
                    "SELECT * FROM Customers WHERE email = @Email AND password = @Password LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Password", password);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new CustomerDTO(
                                reader.GetInt32(reader.GetOrdinal("CustomerID")),
                                reader.GetString(reader.GetOrdinal("FirstName")),
                                reader.GetString(reader.GetOrdinal("LastName")),
                                reader.GetString(reader.GetOrdinal("Email")),
                                reader.GetString(reader.GetOrdinal("Phone")),
                                reader.GetDateTime(reader.GetOrdinal("RegisteredAt")),
                                reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                reader.GetString(reader.GetOrdinal("Password"))
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

        public CustomerDTO GetCustomerByPhoneAndPassword(string phone, string password)
        {
            try
            {
                using (var conn = _dataSource.OpenConnection())
                using (var cmd = new NpgsqlCommand(
                    "SELECT * FROM Customers WHERE Phone = @Phone AND Password = @Password LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@Phone", phone);
                    cmd.Parameters.AddWithValue("@Password", password);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new CustomerDTO(
                                reader.GetInt32(reader.GetOrdinal("CustomerID")),
                                reader.GetString(reader.GetOrdinal("FirstName")),
                                reader.GetString(reader.GetOrdinal("LastName")),
                                reader.GetString(reader.GetOrdinal("Email")),
                                reader.GetString(reader.GetOrdinal("Phone")),
                                reader.GetDateTime(reader.GetOrdinal("RegisteredAt")),
                                reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                reader.GetString(reader.GetOrdinal("Password"))
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
    }
}