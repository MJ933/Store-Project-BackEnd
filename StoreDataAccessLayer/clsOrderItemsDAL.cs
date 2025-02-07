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

        public clsOrderItemsDAL(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
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

        public List<OrderItemDTO> GetAllOrderItemsByOrderID(int id)
        {
            var list = new List<OrderItemDTO>();
            try
            {
                using (var connection = _dataSource.OpenConnection())
                using (NpgsqlCommand command = new NpgsqlCommand(@"
SELECT DISTINCT ON (oi.orderitemid)
    p.productName,
    oi.orderitemid,
    oi.orderid,
    oi.productid,
    oi.quantity,
    oi.price,
    img.imageUrl
FROM
    orderItems oi
INNER JOIN
    Products p ON p.productid = oi.productid
LEFT JOIN  -- Changed from INNER JOIN to LEFT JOIN
    images img ON oi.productid = img.productid
WHERE
    oi.orderid = @OrderID
ORDER BY
    oi.orderitemid,
    CASE WHEN img.isprimary = TRUE THEN 0 ELSE 1 END,  -- Prioritize primary images when available
    img.imageUrl;", connection))
                {
                    command.Parameters.AddWithValue("@OrderID", id);
                    using (var reader = command.ExecuteReader())
                    {
                        // Get ordinals once outside the loop for efficiency
                        int orderItemIdOrdinal = reader.GetOrdinal("OrderItemID");
                        int orderIdOrdinal = reader.GetOrdinal("OrderID");
                        int productIdOrdinal = reader.GetOrdinal("ProductID");
                        int quantityOrdinal = reader.GetOrdinal("Quantity");
                        int priceOrdinal = reader.GetOrdinal("Price");
                        int imageUrlOrdinal = reader.GetOrdinal("ImageUrl");
                        int productNameOrdinal = reader.GetOrdinal("ProductName");


                        while (reader.Read())
                        {


                            list.Add(new OrderItemDTO(
                                reader.GetInt32(orderItemIdOrdinal),
                                reader.GetInt32(orderIdOrdinal),
                                reader.GetInt32(productIdOrdinal),
                                reader.GetInt32(quantityOrdinal),
                                reader.GetDecimal(priceOrdinal),
                                reader.IsDBNull(imageUrlOrdinal) ? null : (string?)reader.GetString(imageUrlOrdinal),
                                reader.GetString(productNameOrdinal)
                            ));
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"NpgsqlException in GetAllOrderItemsByOrderID (OrderID: {id}): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetAllOrderItemsByOrderID (OrderID: {id}): {ex.Message}");
            }
            return list;
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

        public int AddOrderItem(OrderItemDTO dto)
        {
            try
            {
                using (var connection = _dataSource.OpenConnection())
                using (var command = new NpgsqlCommand("INSERT INTO OrderItems (OrderID, ProductID, Quantity, Price) VALUES (@OrderID, @ProductID, @Quantity, @Price) RETURNING OrderItemID", connection))
                {
                    command.Parameters.AddWithValue("@OrderID", dto.OrderID);
                    command.Parameters.AddWithValue("@ProductID", dto.ProductID);
                    command.Parameters.AddWithValue("@Quantity", dto.Quantity);
                    command.Parameters.AddWithValue("@Price", dto.Price);
                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        return (int)result;
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

        public bool UpdateOrderItem(OrderItemDTO dto)
        {
            try
            {
                using (var connection = _dataSource.OpenConnection())
                using (var command = new NpgsqlCommand("UPDATE OrderItems SET  Quantity = @Quantity, Price = @Price WHERE OrderItemID = @OrderItemID", connection))
                {
                    command.Parameters.AddWithValue("@OrderItemID", dto.OrderItemID);
                    command.Parameters.AddWithValue("@ProductID", dto.ProductID);
                    command.Parameters.AddWithValue("@Quantity", dto.Quantity);
                    command.Parameters.AddWithValue("@Price", dto.Price);
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
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

        public bool DeleteOrderItemByOrderItemID(int id)
        {
            try
            {
                using (var connection = _dataSource.OpenConnection())
                using (var command = new NpgsqlCommand("DELETE FROM OrderItems WHERE OrderItemID = @OrderItemID", connection))
                {
                    command.Parameters.AddWithValue("@OrderItemID", id);
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
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