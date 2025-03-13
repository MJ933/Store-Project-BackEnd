//using Dapper;
//using Npgsql;
//using System;
//using System.Data.Common;
//using System.Diagnostics;

//namespace StoreDataAccessLayer
//{
//    public class OrderDTO
//    {
//        public OrderDTO() { }

//        public OrderDTO(int orderID, int customerID, DateTime orderDate, decimal total, string orderStatus, string? shippingAddress = null, string? notes = null)
//        {
//            OrderID = orderID;
//            CustomerID = customerID;
//            OrderDate = orderDate;
//            Total = total;
//            OrderStatus = orderStatus;
//            ShippingAddress = shippingAddress;
//            Notes = notes;
//        }

//        public int OrderID { get; set; }
//        public int CustomerID { get; set; }
//        public DateTime OrderDate { get; set; }
//        public decimal Total { get; set; }
//        public string OrderStatus { get; set; }
//        public string ShippingAddress { get; set; }
//        public string Notes { get; set; }
//    }

//    public class clsOrdersDAL
//    {
//        private readonly NpgsqlDataSource _dataSource;

//        public clsOrdersDAL(NpgsqlDataSource dataSource)
//        {
//            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
//        }


//        public async Task<List<OrderDTO>> GetAllOrders()
//        {
//            try
//            {
//                await using var connection = await _dataSource.OpenConnectionAsync();
//                var result = await connection.QueryAsync<OrderDTO>("SELECT * FROM Orders");
//                return result.ToList();

//            }
//            catch (NpgsqlException ex)
//            {
//                Console.WriteLine($"{ex.Message}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"{ex.Message}");
//            }
//            return new List<OrderDTO>();
//        }


//        public async Task<(List<OrderDTO> OrdersList, int TotalCount)> GetOrdersPaginatedWithFilters(
//           int pageNumber, int pageSize, int? orderID, int? customerID, DateTime? orderDate,
//           decimal? total, string? orderStatus, string? shippingAddress, string? notes)
//        {

//            try
//            {
//                await using var conn = await _dataSource.OpenConnectionAsync();
//                var functionQuery = @"
//                            select * from fn_get_orders_paginated_with_filters(
//                                @p_page_number,
//                                @p_page_size,
//                                @p_order_id ,
//                                @p_customer_id ,
//                                @p_order_date ,
//                                @p_total,
//                                @p_order_status,
//                                @p_shipping_address,
//                                @p_notes 
//                            );
//                            ";
//                var queryParams = new
//                {
//                    p_page_number = pageNumber,
//                    p_page_size = pageSize,
//                    p_order_id = orderID,
//                    p_customer_id = customerID,
//                    p_order_date = orderDate,
//                    p_total = total,
//                    p_order_status = orderStatus,
//                    p_shipping_address = shippingAddress,
//                    p_notes = notes
//                };
//                var result = await conn.QueryAsync<dynamic>(functionQuery, queryParams);
//                var ordersList = result.Select(row => new OrderDTO
//                {
//                    OrderID = row.order_id,
//                    CustomerID = row.customer_id,
//                    OrderDate = row.order_date,
//                    Total = row.total,
//                    OrderStatus = row.order_status,
//                    ShippingAddress = row.shipping_address,
//                    Notes = row.notes
//                }).ToList();
//                int totalCount = result.Any() ? (int)result.First().total_count : 0;
//                return (ordersList, totalCount);

//            }
//            catch (NpgsqlException ex)
//            {
//                Console.WriteLine($"Database error: {ex.Message}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"General error: {ex.Message}");
//            }

//            return (new List<OrderDTO>(), 0);
//        }


//        public async Task<OrderDTO> GetOrderByOrderID(int id)
//        {
//            try
//            {
//                await using var connection = await _dataSource.OpenConnectionAsync();
//                var order = await connection.QueryFirstOrDefaultAsync<OrderDTO>(
//                    "SELECT * FROM Orders WHERE OrderID = @OrderID LIMIT 1"
//                    , new { OrderID = id });
//                if (order != null)
//                    return order;
//            }
//            catch (NpgsqlException ex)
//            {
//                Console.WriteLine($"{ex.Message}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"{ex.Message}");
//            }
//            return new OrderDTO();
//        }

//        public async Task<List<OrderDTO>> GetOrderByCustomerID(int id)
//        {
//            try
//            {
//                await using var connection = await _dataSource.OpenConnectionAsync();
//                var ordersList = await connection.QueryAsync<OrderDTO>("SELECT * FROM Orders WHERE CustomerID = @CustomerID", new { CustomerID = id });
//                return ordersList.ToList();
//            }
//            catch (NpgsqlException ex)
//            {
//                Console.WriteLine($"{ex.Message}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"{ex.Message}");
//            }
//            return new List<OrderDTO>();
//        }

//        public async Task<int> AddOrder(OrderDTO dto)
//        {
//            try
//            {
//                await using var connection = await _dataSource.OpenConnectionAsync();
//                var insertedOrderID = connection.ExecuteScalarAsync<int>(
//                        @"INSERT INTO Orders (CustomerID, OrderDate, Total, OrderStatus, ShippingAddress, Notes)
//                        VALUES (@CustomerID, @OrderDate, @Total, @OrderStatus, @ShippingAddress, @Notes) RETURNING OrderID",
//                        new
//                        {
//                            CustomerID = dto.CustomerID,
//                            OrderDate = dto.OrderDate,
//                            Total = dto.Total,
//                            OrderStatus = dto.OrderStatus,
//                            ShippingAddress = dto.ShippingAddress,
//                            Notes = dto.Notes
//                        });
//                return await insertedOrderID;

//            }
//            catch (NpgsqlException ex)
//            {
//                Console.WriteLine($"{ex.Message}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"{ex.Message}");
//            }
//            return 0;
//        }
//        public async Task<bool> UpdateOrder(OrderDTO dto)
//        {
//            try
//            {
//                await using var conn = await _dataSource.OpenConnectionAsync();
//                const string query = @"
//                                UPDATE Orders
//                                SET 
//                                    OrderDate = @OrderDate,
//                                    Total = @Total,
//                                    OrderStatus = @OrderStatus,
//                                    ShippingAddress = @ShippingAddress,
//                                    Notes = @Notes
//                                WHERE OrderID = @OrderID";
//                int rowsAffected = await conn.ExecuteAsync(query, new
//                {
//                    dto.OrderID,
//                    dto.CustomerID,
//                    dto.OrderDate,
//                    dto.Total,
//                    dto.OrderStatus,
//                    ShippingAddress = dto.ShippingAddress ?? (object)DBNull.Value,
//                    Notes = dto.Notes ?? (object)DBNull.Value
//                });

//                return rowsAffected > 0;
//            }
//            catch (NpgsqlException ex)
//            {
//                return false;
//            }
//        }
//        public async Task<bool> DeleteOrderByOrderID(int id)
//        {
//            try
//            {
//                await using var connection = await _dataSource.OpenConnectionAsync();
//                int rowsAffected = await connection.ExecuteAsync("DELETE FROM Orders WHERE OrderID = @OrderID", new { OrderID = id });
//                return rowsAffected > 0;
//            }
//            catch (NpgsqlException ex)
//            {
//                Console.WriteLine($"{ex.Message}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"{ex.Message}");
//            }
//            return false;
//        }

//        public async Task<bool> UpdateOrderStatusByOrderID(int id, string status)
//        {
//            try
//            {
//                await using var connection = await _dataSource.OpenConnectionAsync();
//                int rowsAffected = await connection.ExecuteAsync("UPDATE Orders SET OrderStatus = @OrderStatus WHERE OrderID = @OrderID", new { OrderID = id, OrderStatus = status });
//                return rowsAffected > 0;

//            }
//            catch (NpgsqlException ex)
//            {
//                Console.WriteLine($"{ex.Message}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"{ex.Message}");
//            }
//            return false;
//        }

//        public async Task<bool> IsOrderExistsByOrderID(int id)
//        {
//            try
//            {
//                await using var connection = await _dataSource.OpenConnectionAsync();
//                int result = await connection.QueryFirstOrDefaultAsync<int>("SELECT 1 FROM Orders WHERE OrderID = @OrderID LIMIT 1", new { OrderID = id });
//                return result > 0;
//            }
//            catch (NpgsqlException ex)
//            {
//                Console.WriteLine($"{ex.Message}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"{ex.Message}");
//            }
//            return false;
//        }
//    }
//}