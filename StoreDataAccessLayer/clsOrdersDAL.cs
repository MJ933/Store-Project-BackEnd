using Dapper;
using Npgsql;
using System;
using System.Data.Common;
using System.Diagnostics;

namespace StoreDataAccessLayer
{
    public class OrderDTO
    {
        public OrderDTO() { }

        public OrderDTO(int orderID, int customerID, DateTime orderDate, decimal total, string orderStatus, string shippingAddress = null, string notes = null)
        {
            OrderID = orderID;
            CustomerID = customerID;
            OrderDate = orderDate;
            Total = total;
            OrderStatus = orderStatus;
            ShippingAddress = shippingAddress;
            Notes = notes;
        }

        public int OrderID { get; set; }
        public int CustomerID { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Total { get; set; }
        public string OrderStatus { get; set; }
        public string ShippingAddress { get; set; }
        public string Notes { get; set; }
    }

    public class clsOrdersDAL
    {
        private readonly NpgsqlDataSource _dataSource;

        public clsOrdersDAL(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        }

        public List<OrderDTO> GetAllOrders()
        {
            var list = new List<OrderDTO>();
            try
            {
                using (var connection = _dataSource.OpenConnection())
                using (var command = new NpgsqlCommand("SELECT * FROM Orders", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new OrderDTO(
                                reader.GetInt32(reader.GetOrdinal("OrderID")),
                                reader.GetInt32(reader.GetOrdinal("CustomerID")),
                                reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                                reader.GetDecimal(reader.GetOrdinal("Total")),
                                reader.GetString(reader.GetOrdinal("OrderStatus")),
                                reader.IsDBNull(reader.GetOrdinal("ShippingAddress")) ? null : reader.GetString(reader.GetOrdinal("ShippingAddress")),
                                reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes"))
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

        public (List<OrderDTO> OrdersList, int TotalCount) GetOrdersPaginatedWithFilters(
           int pageNumber, int pageSize, int? orderID, int? customerID, DateTime? orderDate,
           decimal? total, string? orderStatus, string? shippingAddress, string? notes)
        {
            var ordersList = new List<OrderDTO>();
            int totalCount = 0;

            try
            {
                // Calculate offset for pagination
                int offset = (pageNumber - 1) * pageSize;

                // Step 1: Build the count query with dynamic conditions
                string countQuery = "SELECT COUNT(*) FROM Orders o";
                var countConditions = new List<string>();

                // Add conditions based on provided filters
                if (orderID.HasValue)
                    countConditions.Add("o.OrderID = @OrderID");
                if (customerID.HasValue)
                    countConditions.Add("o.CustomerID = @CustomerID");
                if (orderDate.HasValue)
                {
                    // Check if the input is a full timestamp or just a date
                    if (orderDate.Value.TimeOfDay == TimeSpan.Zero)
                    {
                        // If only a date is provided, compare the date portion
                        countConditions.Add("DATE(o.OrderDate) = @OrderDate");
                    }
                    else
                    {
                        // If a full timestamp is provided, compare the exact value
                        countConditions.Add("o.OrderDate = @OrderDate");
                    }
                }
                if (total.HasValue)
                    countConditions.Add("o.Total = @Total");
                if (!string.IsNullOrEmpty(orderStatus))
                    countConditions.Add("o.OrderStatus ILIKE @OrderStatus");
                if (!string.IsNullOrEmpty(shippingAddress))
                    countConditions.Add("o.ShippingAddress ILIKE @ShippingAddress");
                if (!string.IsNullOrEmpty(notes))
                    countConditions.Add("o.Notes ILIKE @Notes");

                // Append conditions to the count query
                if (countConditions.Any())
                    countQuery += " WHERE " + string.Join(" AND ", countConditions);

                // Parameters for count query
                var countParams = new
                {
                    OrderID = orderID,
                    CustomerID = customerID,
                    OrderDate = orderDate,
                    Total = total,
                    OrderStatus = !string.IsNullOrEmpty(orderStatus) ? $"%{orderStatus}%" : null,
                    ShippingAddress = !string.IsNullOrEmpty(shippingAddress) ? $"%{shippingAddress}%" : null,
                    Notes = !string.IsNullOrEmpty(notes) ? $"%{notes}%" : null
                };

                // Execute count query
                using var conn = _dataSource.OpenConnection();
                totalCount = conn.ExecuteScalar<int>(countQuery, countParams);

                // Step 2: Build the orders query with dynamic conditions
                string ordersQuery = "SELECT * FROM Orders o";
                var ordersConditions = new List<string>(countConditions); // Reuse conditions

                // Append conditions to the orders query
                if (ordersConditions.Any())
                    ordersQuery += " WHERE " + string.Join(" AND ", ordersConditions);

                // Add pagination
                ordersQuery += " ORDER BY o.OrderID LIMIT @PageSize OFFSET @Offset;";

                // Parameters for orders query
                var orderParams = new
                {
                    PageSize = pageSize,
                    Offset = offset,
                    OrderID = orderID,
                    CustomerID = customerID,
                    OrderDate = orderDate,
                    Total = total,
                    OrderStatus = !string.IsNullOrEmpty(orderStatus) ? $"%{orderStatus}%" : null,
                    ShippingAddress = !string.IsNullOrEmpty(shippingAddress) ? $"%{shippingAddress}%" : null,
                    Notes = !string.IsNullOrEmpty(notes) ? $"%{notes}%" : null
                };

                // Execute orders query
                var result = conn.Query<OrderDTO>(ordersQuery, orderParams);
                ordersList = result.AsList();

                return (ordersList, totalCount);
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
            }

            return (ordersList, totalCount);
        }

        public OrderDTO GetOrderByOrderID(int id)
        {
            try
            {
                using (var connection = _dataSource.OpenConnection())
                using (var command = new NpgsqlCommand("SELECT * FROM Orders WHERE OrderID = @OrderID LIMIT 1", connection))
                {
                    command.Parameters.AddWithValue("@OrderID", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new OrderDTO(
                               reader.GetInt32(reader.GetOrdinal("OrderID")),
                               reader.GetInt32(reader.GetOrdinal("CustomerID")),
                               reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                               reader.GetDecimal(reader.GetOrdinal("Total")),
                               reader.GetString(reader.GetOrdinal("OrderStatus")),
                               reader.IsDBNull(reader.GetOrdinal("ShippingAddress")) ? null : reader.GetString(reader.GetOrdinal("ShippingAddress")),
                               reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes"))
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

        public List<OrderDTO> GetOrderByCustomerID(int id)
        {
            var list = new List<OrderDTO>();
            try
            {
                using (var connection = _dataSource.OpenConnection())
                using (var command = new NpgsqlCommand("SELECT * FROM Orders WHERE CustomerID = @CustomerID", connection))
                {
                    command.Parameters.AddWithValue("@CustomerID", id);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new OrderDTO(
                                reader.GetInt32(reader.GetOrdinal("OrderID")),
                                reader.GetInt32(reader.GetOrdinal("CustomerID")),
                                reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                                reader.GetDecimal(reader.GetOrdinal("Total")),
                                reader.GetString(reader.GetOrdinal("OrderStatus")),
                                reader.IsDBNull(reader.GetOrdinal("ShippingAddress")) ? null : reader.GetString(reader.GetOrdinal("ShippingAddress")),
                                reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes"))
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

        public int AddOrder(OrderDTO dto)
        {
            try
            {
                using (var connection = _dataSource.OpenConnection())
                using (var command = new NpgsqlCommand("INSERT INTO Orders (CustomerID, OrderDate, Total, OrderStatus, ShippingAddress, Notes) VALUES (@CustomerID, @OrderDate, @Total, @OrderStatus, @ShippingAddress, @Notes) RETURNING OrderID", connection))
                {
                    command.Parameters.AddWithValue("@CustomerID", dto.CustomerID);
                    command.Parameters.AddWithValue("@OrderDate", dto.OrderDate);
                    command.Parameters.AddWithValue("@Total", dto.Total);
                    command.Parameters.AddWithValue("@OrderStatus", dto.OrderStatus);
                    command.Parameters.AddWithValue("@ShippingAddress", (object)dto.ShippingAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Notes", (object)dto.Notes ?? DBNull.Value);
                    var result = command.ExecuteScalar();
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

        //public bool UpdateOrder(OrderDTO dto)
        //{
        //    try
        //    {
        //        using (var connection = _dataSource.OpenConnection())
        //        using (var command = new NpgsqlCommand("UPDATE Orders SET CustomerID = @CustomerID, OrderDate = @OrderDate, Total = @Total, OrderStatus = @OrderStatus, ShippingAddress = @ShippingAddress, Notes = @Notes WHERE OrderID = @OrderID", connection))
        //        {
        //            command.Parameters.AddWithValue("@OrderID", dto.OrderID);
        //            command.Parameters.AddWithValue("@CustomerID", dto.CustomerID);
        //            command.Parameters.AddWithValue("@OrderDate", dto.OrderDate);
        //            command.Parameters.AddWithValue("@Total", dto.Total);
        //            command.Parameters.AddWithValue("@OrderStatus", dto.OrderStatus);
        //            command.Parameters.AddWithValue("@ShippingAddress", (object)dto.ShippingAddress ?? DBNull.Value);
        //            command.Parameters.AddWithValue("@Notes", (object)dto.Notes ?? DBNull.Value);
        //            int rowsAffected = command.ExecuteNonQuery();
        //            return rowsAffected > 0;
        //        }
        //    }
        //    catch (NpgsqlException ex)
        //    {
        //        Console.WriteLine($"{ex.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"{ex.Message}");
        //    }
        //    return false;
        //}


        public bool UpdateOrder(OrderDTO dto)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Console.WriteLine("Starting UpdateOrder function");

            const string sql = @"
UPDATE Orders
SET 
    OrderDate = @OrderDate,
    Total = @Total,
    OrderStatus = @OrderStatus,
    ShippingAddress = @ShippingAddress,
    Notes = @Notes
WHERE OrderID = @OrderID";

            TimeSpan sqlDefinitionTime = stopwatch.Elapsed;
            Console.WriteLine($"Stage: SQL Definition - Time: {sqlDefinitionTime.TotalMilliseconds}ms");

            try
            {
                stopwatch.Restart(); // Restart for connection opening
                Console.WriteLine("Stage: Opening database connection");
                using var conn = _dataSource.OpenConnection();
                TimeSpan connectionOpenTime = stopwatch.Elapsed;
                Console.WriteLine($"Stage: Connection Open - Time: {connectionOpenTime.TotalMilliseconds}ms");

                stopwatch.Restart(); // Restart for SQL execution
                Console.WriteLine("Stage: Executing SQL query");
                int rowsAffected = conn.Execute(sql, new
                {
                    dto.OrderID,
                    dto.CustomerID,
                    dto.OrderDate,
                    dto.Total,
                    dto.OrderStatus,
                    ShippingAddress = dto.ShippingAddress ?? (object)DBNull.Value,
                    Notes = dto.Notes ?? (object)DBNull.Value
                });
                TimeSpan sqlExecutionTime = stopwatch.Elapsed;
                Console.WriteLine($"Stage: SQL Execution - Time: {sqlExecutionTime.TotalMilliseconds}ms");

                stopwatch.Restart(); // Restart for return result stage
                bool result = rowsAffected > 0;
                TimeSpan resultCalculationTime = stopwatch.Elapsed;
                Console.WriteLine($"Stage: Result Calculation - Time: {resultCalculationTime.TotalMilliseconds}ms");

                stopwatch.Stop(); // Stop the overall timer
                TimeSpan totalTime = stopwatch.Elapsed + sqlDefinitionTime + connectionOpenTime + sqlExecutionTime + resultCalculationTime; // Calculate total time including all stages
                Console.WriteLine($"Function UpdateOrder completed successfully. Total Time: {totalTime.TotalMilliseconds}ms");
                return result;
            }
            catch (NpgsqlException ex)
            {
                stopwatch.Restart(); // Restart for error handling stage
                Console.WriteLine("Stage: Database Error Handling");
                Console.WriteLine($"Database error: {ex.Message}");
                TimeSpan errorHandlingTime = stopwatch.Elapsed;
                Console.WriteLine($"Stage: Error Handling - Time: {errorHandlingTime.TotalMilliseconds}ms");

                stopwatch.Stop(); // Stop the overall timer
                TimeSpan totalTimeOnError = stopwatch.Elapsed + sqlDefinitionTime + errorHandlingTime; // Calculate total time including stages before error and error handling
                Console.WriteLine($"Function UpdateOrder completed with error. Total Time: {totalTimeOnError.TotalMilliseconds}ms");
                return false;
            }

        }
        public bool DeleteOrderByOrderID(int id)
        {
            try
            {
                using (var connection = _dataSource.OpenConnection())
                using (var command = new NpgsqlCommand("DELETE FROM Orders WHERE OrderID = @OrderID", connection))
                {
                    command.Parameters.AddWithValue("@OrderID", id);
                    int rowsAffected = command.ExecuteNonQuery();
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

        public bool UpdateOrderStatusByOrderID(int id, string status)
        {
            try
            {
                using (var connection = _dataSource.OpenConnection())
                using (var command = new NpgsqlCommand("UPDATE Orders SET OrderStatus = @OrderStatus WHERE OrderID = @OrderID", connection))
                {
                    command.Parameters.AddWithValue("@OrderID", id);
                    command.Parameters.AddWithValue("@OrderStatus", status);
                    int rowsAffected = command.ExecuteNonQuery();
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

        public bool IsOrderExistsByOrderID(int id)
        {
            try
            {
                using (var connection = _dataSource.OpenConnection())
                using (var command = new NpgsqlCommand("SELECT 1 FROM Orders WHERE OrderID = @OrderID LIMIT 1", connection))
                {
                    command.Parameters.AddWithValue("@OrderID", id);
                    var result = command.ExecuteScalar();
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