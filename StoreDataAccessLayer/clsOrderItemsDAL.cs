using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;

namespace StoreDataAccessLayer
{
    public class OrderItemDTO
    {
        public OrderItemDTO() { }

        // Constructor for base OrderItem data (without ProductName and ImageUrl)
        public OrderItemDTO(int orderItemID, int orderID, int productID, int quantity, decimal price)
        {
            OrderItemID = orderItemID;
            OrderID = orderID;
            ProductID = productID;
            Quantity = quantity;
            Price = price;
        }

        // Constructor including ProductName and ImageUrl (for GetAllOrderItemsByOrderID)
        public OrderItemDTO(int orderItemID, int orderID, int productID, int quantity, decimal price, string? imageUrl, string productName) : this(orderItemID, orderID, productID, quantity, price)
        {
            ImageUrl = imageUrl;
            ProductName = productName;
        }

        public int OrderItemID { get; set; }
        public int OrderID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; } // Nullable string as it might not always be populated
        public string? ProductName { get; set; } // Nullable string as it might not always be populated
    }


    public class clsOrderItemsDAL
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly clsProductsDAL _productsDAL;

        public clsOrderItemsDAL(NpgsqlDataSource dataSource, clsProductsDAL productsDAL)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _productsDAL = productsDAL;
        }

        public List<OrderItemDTO> GetAllOrderItems()
        {
            var list = new List<OrderItemDTO>();
            try
            {
                using (var connection = _dataSource.OpenConnection())
                using (var command = new NpgsqlCommand("SELECT * FROM OrderItems", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        int orderItemIdOrdinal = reader.GetOrdinal("OrderItemID");
                        int orderIdOrdinal = reader.GetOrdinal("OrderID");
                        int productIdOrdinal = reader.GetOrdinal("ProductID");
                        int quantityOrdinal = reader.GetOrdinal("Quantity");
                        int priceOrdinal = reader.GetOrdinal("Price");

                        while (reader.Read())
                        {
                            list.Add(new OrderItemDTO(
                                reader.GetInt32(orderItemIdOrdinal),
                                reader.GetInt32(orderIdOrdinal),
                                reader.GetInt32(productIdOrdinal),
                                reader.GetInt32(quantityOrdinal),
                                reader.GetDecimal(priceOrdinal)
                            ));
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"NpgsqlException in GetAllOrderItems: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetAllOrderItems: {ex.Message}");
            }
            return list;
        }

        public async Task<List<OrderItemDTO>> GetAllOrderItemsByOrderID(int id)
        {
            try
            {
                await using var connection = _dataSource.OpenConnection();
                var orders = await connection.QueryAsync<OrderItemDTO>("select * from fn_get_all_order_items_by_order_id(@p_order_id)", new { p_order_item_id = id });
                return orders.ToList();

            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"NpgsqlException in GetAllOrderItemsByOrderID (OrderID: {id}): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetAllOrderItemsByOrderID (OrderID: {id}): {ex.Message}");
            }
            return new List<OrderItemDTO>();
        }

        public OrderItemDTO GetOrderItemByOrderItemID(int id)
        {
            try
            {
                using (var connection = _dataSource.OpenConnection())
                using (var command = new NpgsqlCommand("SELECT * FROM OrderItems WHERE OrderItemID = @OrderItemID LIMIT 1", connection))
                {
                    command.Parameters.AddWithValue("@OrderItemID", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int orderItemIdOrdinal = reader.GetOrdinal("OrderItemID");
                            int orderIdOrdinal = reader.GetOrdinal("OrderID");
                            int productIdOrdinal = reader.GetOrdinal("ProductID");
                            int quantityOrdinal = reader.GetOrdinal("Quantity");
                            int priceOrdinal = reader.GetOrdinal("Price");

                            return new OrderItemDTO(
                               reader.GetInt32(orderItemIdOrdinal),
                               reader.GetInt32(orderIdOrdinal),
                               reader.GetInt32(productIdOrdinal),
                               reader.GetInt32(quantityOrdinal),
                               reader.GetDecimal(priceOrdinal)
                           );
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"NpgsqlException in GetOrderItemByOrderItemID (OrderItemID: {id}): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetOrderItemByOrderItemID (OrderItemID: {id}): {ex.Message}");
            }
            return null;
        }


        public async Task<int> AddOrderItem(OrderItemDTO dto)
        {
            NpgsqlTransaction transaction = null;
            try
            {
                using (var connection = _dataSource.OpenConnection())
                {
                    transaction = connection.BeginTransaction();
                    using (var command = new NpgsqlCommand("INSERT INTO OrderItems (OrderID, ProductID, Quantity, Price)" +
                        " VALUES (@OrderID, @ProductID, @Quantity, @Price) RETURNING OrderItemID", connection, transaction))
                    {
                        command.Parameters.AddWithValue("@OrderID", dto.OrderID);
                        command.Parameters.AddWithValue("@ProductID", dto.ProductID);
                        command.Parameters.AddWithValue("@Quantity", dto.Quantity);
                        command.Parameters.AddWithValue("@Price", dto.Price);
                        var result = command.ExecuteScalar();
                        if (result == null)
                        {
                            transaction.Rollback();
                            return (0);
                        }

                        int orderItemID = (int)result;
                        var product = await _productsDAL.GetProductByProductIDAsync(dto.ProductID);
                        bool isStockUpdated = await _productsDAL.UpdateProductQuantity(dto.ProductID, product.StockQuantity - dto.Quantity, connection, transaction);
                        if (!isStockUpdated)
                        {
                            // Rollback if the stock update fails
                            transaction.Rollback();
                            return 0;
                        }
                        transaction.Commit();
                        return orderItemID;
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"NpgsqlException in AddOrderItem: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in AddOrderItem: {ex.Message}");
            }
            return 0;
        }

        public async Task<bool> UpdateOrderItem(OrderItemDTO dto)
        {
            NpgsqlTransaction transaction = null;
            try
            {
                using (var connection = _dataSource.OpenConnection())
                {
                    transaction = connection.BeginTransaction();
                    using (var command = new NpgsqlCommand("UPDATE OrderItems SET  Quantity = @Quantity," +
                        " Price = @Price WHERE OrderItemID = @OrderItemID", connection, transaction))
                    {
                        command.Parameters.AddWithValue("@OrderItemID", dto.OrderItemID);
                        command.Parameters.AddWithValue("@ProductID", dto.ProductID);
                        command.Parameters.AddWithValue("@Quantity", dto.Quantity);
                        command.Parameters.AddWithValue("@Price", dto.Price);
                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected <= 0)
                        {
                            transaction.Rollback();
                            return false;
                        }
                        int orderItemID = dto.OrderItemID;
                        var product = await _productsDAL.GetProductByProductIDAsync(dto.ProductID);
                        int OldQuantity = GetOrderItemByOrderItemID(orderItemID).Quantity;
                        int CurrentQuantity = OldQuantity - dto.Quantity;
                        bool isStockUpdated = await _productsDAL.UpdateProductQuantity(dto.ProductID, product.StockQuantity + CurrentQuantity, connection, transaction);
                        if (!isStockUpdated)
                        {
                            // Rollback if the stock update fails
                            transaction.Rollback();
                            return false;
                        }
                        transaction.Commit();
                        return true;
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"NpgsqlException in UpdateOrderItem (OrderItemID: {dto.OrderItemID}): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in UpdateOrderItem (OrderItemID: {dto.OrderItemID}): {ex.Message}");
            }
            return false;
        }

        public async Task<bool> DeleteOrderItemByOrderItemID(int id)
        {
            NpgsqlTransaction transaction = null;
            try
            {
                using (var connection = _dataSource.OpenConnection())
                {
                    transaction = connection.BeginTransaction();
                    using (var command = new NpgsqlCommand("DELETE FROM OrderItems WHERE OrderItemID = @OrderItemID", connection))
                    {
                        command.Parameters.AddWithValue("@OrderItemID", id);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected <= 0)
                        {
                            transaction.Rollback();
                            return false;
                        }

                        int orderItemID = id;
                        var OrderItem = GetOrderItemByOrderItemID(id);
                        int OldQuantity = GetOrderItemByOrderItemID(orderItemID).Quantity;
                        var product = await _productsDAL.GetProductByProductIDAsync(OrderItem.ProductID);
                        bool isStockUpdated = await _productsDAL.UpdateProductQuantity(product.ProductID, product.StockQuantity + OrderItem.Quantity, connection, transaction);
                        if (!isStockUpdated)
                        {
                            // Rollback if the stock update fails
                            transaction.Rollback();
                            return false;
                        }
                        transaction.Commit();
                        return true;
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"NpgsqlException in DeleteOrderItemByOrderItemID (OrderItemID: {id}): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in DeleteOrderItemByOrderItemID (OrderItemID: {id}): {ex.Message}");
            }
            return false;
        }

        public bool IsOrderItemExistsByOrderItemID(int id)
        {
            try
            {
                using (var connection = _dataSource.OpenConnection())
                using (var command = new NpgsqlCommand("SELECT 1 FROM OrderItems WHERE OrderItemID = @OrderItemID LIMIT 1", connection))
                {
                    command.Parameters.AddWithValue("@OrderItemID", id);
                    var result = command.ExecuteScalar();
                    return result != null;
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"NpgsqlException in IsOrderItemExistsByOrderItemID (OrderItemID: {id}): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in IsOrderItemExistsByOrderItemID (OrderItemID: {id}): {ex.Message}");
            }
            return false;
        }
    }
}