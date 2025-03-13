using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreDataAccessLayer
{
    public interface IOrdersRepository
    {
        Task<List<OrderDTO>> GetAllOrdersAsync();
        Task<(List<OrderDTO> OrdersList, int TotalCount)> GetOrdersPaginatedWithFiltersAsync(
         int pageNumber, int pageSize, int? orderID, int? customerID, DateTime? orderDate,
         decimal? total, string? orderStatus, string? shippingAddress, string? notes);
        Task<OrderDTO?> GetOrderByOrderIDAsync(int OrderID);
        Task<List<OrderDTO>> GetOrderByCustomerIDAsync(int CustomerID);
        Task<int> AddOrderAsync(OrderDTO newOrderDTO);
        Task<bool> UpdateOrderAsync(OrderDTO OrderDTO);
        Task<bool> DeleteOrderAsync(int OrderID);
        Task<bool> UpdateOrderStatusByOrderIDAsync(int OrderID, string OrderStatus);
        Task<bool> IsOrderExistsByOrderIDAsync(int OrderID);
    }
    public class OrdersRepository : IOrdersRepository
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger<OrdersRepository> _logger;

        public OrdersRepository(NpgsqlDataSource dataSource, ILogger<OrdersRepository> logger)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<List<OrderDTO>> GetAllOrdersAsync()
        {
            try
            {
                await using var connection = await _dataSource.OpenConnectionAsync();
                var result = await connection.QueryAsync<OrderDTO>("SELECT * FROM Orders");
                return result.ToList();

            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllOrdersAsync");
                return new List<OrderDTO>();
            }
        }
        public async Task<(List<OrderDTO> OrdersList, int TotalCount)> GetOrdersPaginatedWithFiltersAsync(
         int pageNumber, int pageSize, int? orderID, int? customerID, DateTime? orderDate,
         decimal? total, string? orderStatus, string? shippingAddress, string? notes)
        {

            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                const string sql = @"
                            select * from fn_get_orders_paginated_with_filters(
                                @p_page_number,
                                @p_page_size,
                                @p_order_id ,
                                @p_customer_id ,
                                @p_order_date ,
                                @p_total,
                                @p_order_status,
                                @p_shipping_address,
                                @p_notes 
                            );
                            ";
                var parameters = new
                {
                    p_page_number = pageNumber,
                    p_page_size = pageSize,
                    p_order_id = orderID,
                    p_customer_id = customerID,
                    p_order_date = orderDate,
                    p_total = total,
                    p_order_status = orderStatus,
                    p_shipping_address = shippingAddress,
                    p_notes = notes
                };
                var result = await conn.QueryAsync<dynamic>(sql, parameters);
                var ordersList = result.Select(row => new OrderDTO
                {
                    OrderID = row.order_id,
                    CustomerID = row.customer_id,
                    OrderDate = row.order_date,
                    Total = row.total,
                    OrderStatus = row.order_status,
                    ShippingAddress = row.shipping_address,
                    Notes = row.notes
                }).ToList();
                int totalCount = result.Any() ? (int)result.First().total_count : 0;
                return (ordersList, totalCount);

            }

            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetOrdersPaginatedWithFiltersAsync:Page {pageNumber}, Size {pageSize}");
                return (new List<OrderDTO>(), 0);
            }

        }
        public async Task<OrderDTO?> GetOrderByOrderIDAsync(int OrderID)
        {
            try
            {
                await using var connection = await _dataSource.OpenConnectionAsync();
                return await connection.QuerySingleOrDefaultAsync<OrderDTO>(
                    "SELECT * FROM Orders WHERE OrderID = @OrderID LIMIT 1"
                    , new { OrderID = OrderID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetOrderByOrderIDAsync: CustomerID {OrderID}");
                return null;
            }
        }
        public async Task<List<OrderDTO>> GetOrderByCustomerIDAsync(int CustomerID)
        {
            try
            {
                const string sql = "SELECT * FROM Orders WHERE CustomerID = @CustomerID";
                await using var connection = await _dataSource.OpenConnectionAsync();
                var ordersList = await connection.QueryAsync<OrderDTO>(sql, new { CustomerID = CustomerID });
                return ordersList.ToList();
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetOrderByCustomerIDAsync for CustomerID: {CustomerID}");
                return new List<OrderDTO>();
            }

        }

        public async Task<int> AddOrderAsync(OrderDTO newOrderDTO)
        {
            try
            {
                const string sql = @"INSERT INTO Orders (CustomerID, OrderDate, Total, OrderStatus, ShippingAddress, Notes)
                        VALUES (@CustomerID, @OrderDate, @Total, @OrderStatus, @ShippingAddress, @Notes) RETURNING OrderID";
                var parameters = new
                {
                    CustomerID = newOrderDTO.CustomerID,
                    OrderDate = newOrderDTO.OrderDate,
                    Total = newOrderDTO.Total,
                    OrderStatus = newOrderDTO.OrderStatus,
                    ShippingAddress = newOrderDTO.ShippingAddress,
                    Notes = newOrderDTO.Notes
                };
                await using var connection = await _dataSource.OpenConnectionAsync();
                var insertedOrderID = await connection.ExecuteScalarAsync<int>(sql, parameters);
                return insertedOrderID;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in AddOrderAsync for customerID: {newOrderDTO.CustomerID} ");
                return -1;
            }

        }
        public async Task<bool> UpdateOrderAsync(OrderDTO OrderDTO)
        {
            try
            {
                const string sql = @"
                                UPDATE Orders
                                SET 
                                    OrderDate = @OrderDate,
                                    Total = @Total,
                                    OrderStatus = @OrderStatus,
                                    ShippingAddress = @ShippingAddress,
                                    Notes = @Notes
                                WHERE OrderID = @OrderID";
                var parameters = new
                {
                    OrderDTO.OrderID,
                    OrderDTO.CustomerID,
                    OrderDTO.OrderDate,
                    OrderDTO.Total,
                    OrderDTO.OrderStatus,
                    ShippingAddress = OrderDTO.ShippingAddress ?? (object)DBNull.Value,
                    Notes = OrderDTO.Notes ?? (object)DBNull.Value
                };

                await using var conn = await _dataSource.OpenConnectionAsync();
                int rowsAffected = await conn.ExecuteAsync(sql, parameters);
                return rowsAffected > 0;
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, $"Error in UpdateOrderAsync for OrderID: {OrderDTO.OrderID}");
                return false;
            }
        }
        public async Task<bool> DeleteOrderAsync(int OrderID)
        {
            try
            {
                const string sql = "DELETE FROM Orders WHERE OrderID = @OrderID";
                await using var connection = await _dataSource.OpenConnectionAsync();
                int rowsAffected = await connection.ExecuteAsync(sql, new { OrderID = OrderID });
                return rowsAffected > 0;
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in DeleteOrderAsync for OrderID: {OrderID}");
                return false;
            }
        }

        public async Task<bool> UpdateOrderStatusByOrderIDAsync(int OrderID, string OrderStatus)
        {
            try
            {
                const string sql = "UPDATE Orders SET OrderStatus = @OrderStatus WHERE OrderID = @OrderID";
                await using var connection = await _dataSource.OpenConnectionAsync();
                int rowsAffected = await connection.ExecuteAsync(sql, new { OrderID = OrderID, OrderStatus = OrderStatus });
                return rowsAffected > 0;

            }

            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in UpdateOrderStatusByOrderIDAsync for: OrderID = {OrderID}, Status = {OrderStatus}");
                return false;
            }

        }
        public async Task<bool> IsOrderExistsByOrderIDAsync(int OrderID)
        {
            try
            {
                const string sql = "SELECT 1 FROM Orders WHERE OrderID = @OrderID LIMIT 1";
                await using var connection = await _dataSource.OpenConnectionAsync();
                int result = await connection.QueryFirstOrDefaultAsync<int>(sql, new { OrderID = OrderID });
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in IsOrderExistsByOrderIDAsync for OrderID: {OrderID}");
                return false;
            }
        }
    }
    public record OrderDTO(
    int OrderID = 0,
    int CustomerID = 0,
    DateTime OrderDate = default,
    decimal Total = 0,
    string OrderStatus = "Pending",
    string? ShippingAddress = null,
    string? Notes = null);
}
