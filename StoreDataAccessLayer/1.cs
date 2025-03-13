//using Dapper;
//using Microsoft.Extensions.Logging;
//using Npgsql;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace StoreDataAccessLayer
//{
//    public class OrderItemDTO1
//    {
//        public OrderItemDTO1() { }

//        // Constructor for base OrderItem data
//        public OrderItemDTO1(int orderItemId, int orderId, int productId, int quantity, decimal price)
//        {
//            OrderItemId = orderItemId;
//            OrderId = orderId;
//            ProductId = productId;
//            Quantity = quantity;
//            Price = price;
//        }

//        // Constructor including ProductName and ImageUrl
//        public OrderItemDTO1(int orderItemId, int orderId, int productId, int quantity, decimal price, string? imageUrl, string productName)
//            : this(orderItemId, orderId, productId, quantity, price)
//        {
//            ImageUrl = imageUrl;
//            ProductName = productName;
//        }

//        public int OrderItemId { get; set; }
//        public int OrderId { get; set; }
//        public int ProductId { get; set; }
//        public int Quantity { get; set; }
//        public decimal Price { get; set; }
//        public string? ImageUrl { get; set; }
//        public string? ProductName { get; set; }
//    }

//    public interface IOrderItemsRepository
//    {
//        Task<List<OrderItemDTO1>> GetAllOrderItemsAsync();
//        Task<List<OrderItemDTO1>> GetOrderItemsByOrderIdAsync(int orderId);
//        Task<OrderItemDTO1?> GetOrderItemByIdAsync(int orderItemId);
//        Task<int> AddOrderItemAsync(OrderItemDTO1 orderItem);
//        Task<bool> UpdateOrderItemAsync(OrderItemDTO1 orderItem);
//        Task<bool> DeleteOrderItemByIdAsync(int orderItemId);
//        Task<bool> OrderItemExistsByIdAsync(int orderItemId);
//    }

//    public class OrderItemsRepository : IOrderItemsRepository
//    {
//        private readonly NpgsqlDataSource _dataSource;
//        private readonly IProductsRepository _productsRepository;
//        private readonly ILogger<OrderItemsRepository> _logger;

//        public OrderItemsRepository(
//            NpgsqlDataSource dataSource,
//            IProductsRepository productsRepository,
//            ILogger<OrderItemsRepository> logger)
//        {
//            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
//            _productsRepository = productsRepository ?? throw new ArgumentNullException(nameof(productsRepository));
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//        }

//        public async Task<List<OrderItemDTO1>> GetAllOrderItemsAsync()
//        {
//            try
//            {
//                const string sql = "SELECT OrderItemId, OrderId, ProductId, Quantity, Price FROM OrderItems";

//                await using var connection = await _dataSource.OpenConnectionAsync();
//                var results = await connection.QueryAsync<OrderItemDTO1>(sql);

//                return results?.ToList() ?? new List<OrderItemDTO1>();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in GetAllOrderItemsAsync");
//                return new List<OrderItemDTO1>();
//            }
//        }

//        public async Task<List<OrderItemDTO1>> GetOrderItemsByOrderIdAsync(int orderId)
//        {
//            try
//            {
//                const string sql = @"
//                    SELECT oi.OrderItemId, oi.OrderId, oi.ProductId, oi.Quantity, oi.Price, 
//                           p.ImageUrl, p.ProductName
//                    FROM OrderItems oi
//                    JOIN Products p ON oi.ProductId = p.ProductId
//                    WHERE oi.OrderId = @OrderId";

//                await using var connection = await _dataSource.OpenConnectionAsync();
//                var results = await connection.QueryAsync<OrderItemDTO1>(sql, new { OrderId = orderId });

//                return results?.ToList() ?? new List<OrderItemDTO1>();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in GetOrderItemsByOrderIdAsync for OrderId: {OrderId}", orderId);
//                return new List<OrderItemDTO1>();
//            }
//        }

//        public async Task<OrderItemDTO1?> GetOrderItemByIdAsync(int orderItemId)
//        {
//            try
//            {
//                const string sql = @"
//                    SELECT OrderItemId, OrderId, ProductId, Quantity, Price 
//                    FROM OrderItems 
//                    WHERE OrderItemId = @OrderItemId 
//                    LIMIT 1";

//                await using var connection = await _dataSource.OpenConnectionAsync();
//                return await connection.QuerySingleOrDefaultAsync<OrderItemDTO1>(sql, new { OrderItemId = orderItemId });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in GetOrderItemByIdAsync for OrderItemId: {OrderItemId}", orderItemId);
//                return null;
//            }
//        }

//        public async Task<int> AddOrderItemAsync(OrderItemDTO1 orderItem)
//        {
//            await using var connection = await _dataSource.OpenConnectionAsync();
//            await using var transaction = await connection.BeginTransactionAsync();

//            try
//            {
//                const string sql = @"
//                    INSERT INTO OrderItems (OrderId, ProductId, Quantity, Price)
//                    VALUES (@OrderId, @ProductId, @Quantity, @Price)
//                    RETURNING OrderItemId";

//                var parameters = new
//                {
//                    OrderId = orderItem.OrderId,
//                    ProductId = orderItem.ProductId,
//                    Quantity = orderItem.Quantity,
//                    Price = orderItem.Price
//                };

//                var orderItemId = await connection.QuerySingleAsync<int>(sql, parameters, transaction);

//                // Update product stock
//                var product = await _productsRepository.GetProductByIdAsync(orderItem.ProductId);
//                if (product == null)
//                {
//                    await transaction.RollbackAsync();
//                    return 0;
//                }

//                bool stockUpdated = await _productsRepository.UpdateProductQuantityAsync(
//                    orderItem.ProductId,
//                    product.StockQuantity - orderItem.Quantity,
//                    connection,
//                    transaction);

//                if (!stockUpdated)
//                {
//                    await transaction.RollbackAsync();
//                    return 0;
//                }

//                await transaction.CommitAsync();
//                return orderItemId;
//            }
//            catch (Exception ex)
//            {
//                await transaction.RollbackAsync();
//                _logger.LogError(ex, "Error in AddOrderItemAsync for OrderId: {OrderId}, ProductId: {ProductId}",
//                    orderItem.OrderId, orderItem.ProductId);
//                return 0;
//            }
//        }

//        public async Task<bool> UpdateOrderItemAsync(OrderItemDTO1 orderItem)
//        {
//            await using var connection = await _dataSource.OpenConnectionAsync();
//            await using var transaction = await connection.BeginTransactionAsync();

//            try
//            {
//                // Get current order item to calculate stock difference
//                var currentOrderItem = await GetOrderItemByIdAsync(orderItem.OrderItemId);
//                if (currentOrderItem == null)
//                {
//                    await transaction.RollbackAsync();
//                    return false;
//                }

//                const string sql = @"
//                    UPDATE OrderItems 
//                    SET Quantity = @Quantity, Price = @Price 
//                    WHERE OrderItemId = @OrderItemId";

//                var parameters = new
//                {
//                    OrderItemId = orderItem.OrderItemId,
//                    Quantity = orderItem.Quantity,
//                    Price = orderItem.Price
//                };

//                int rowsAffected = await connection.ExecuteAsync(sql, parameters, transaction);
//                if (rowsAffected <= 0)
//                {
//                    await transaction.RollbackAsync();
//                    return false;
//                }

//                // Calculate stock adjustment
//                int quantityDifference = currentOrderItem.Quantity - orderItem.Quantity;

//                // Update product stock
//                var product = await _productsRepository.GetProductByIdAsync(orderItem.ProductId);
//                if (product == null)
//                {
//                    await transaction.RollbackAsync();
//                    return false;
//                }

//                bool stockUpdated = await _productsRepository.UpdateProductQuantityAsync(
//                    orderItem.ProductId,
//                    product.StockQuantity + quantityDifference,
//                    connection,
//                    transaction);

//                if (!stockUpdated)
//                {
//                    await transaction.RollbackAsync();
//                    return false;
//                }

//                await transaction.CommitAsync();
//                return true;
//            }
//            catch (Exception ex)
//            {
//                await transaction.RollbackAsync();
//                _logger.LogError(ex, "Error in UpdateOrderItemAsync for OrderItemId: {OrderItemId}", orderItem.OrderItemId);
//                return false;
//            }
//        }

//        public async Task<bool> DeleteOrderItemByIdAsync(int orderItemId)
//        {
//            await using var connection = await _dataSource.OpenConnectionAsync();
//            await using var transaction = await connection.BeginTransactionAsync();

//            try
//            {
//                // Get current order item to restore stock
//                var orderItem = await GetOrderItemByIdAsync(orderItemId);
//                if (orderItem == null)
//                {
//                    await transaction.RollbackAsync();
//                    return false;
//                }

//                const string sql = "DELETE FROM OrderItems WHERE OrderItemId = @OrderItemId";

//                int rowsAffected = await connection.ExecuteAsync(sql, new { OrderItemId = orderItemId }, transaction);
//                if (rowsAffected <= 0)
//                {
//                    await transaction.RollbackAsync();
//                    return false;
//                }

//                // Update product stock
//                var product = await _productsRepository.GetProductByIdAsync(orderItem.ProductId);
//                if (product == null)
//                {
//                    await transaction.RollbackAsync();
//                    return false;
//                }

//                bool stockUpdated = await _productsRepository.UpdateProductQuantityAsync(
//                    orderItem.ProductId,
//                    product.StockQuantity + orderItem.Quantity,
//                    connection,
//                    transaction);

//                if (!stockUpdated)
//                {
//                    await transaction.RollbackAsync();
//                    return false;
//                }

//                await transaction.CommitAsync();
//                return true;
//            }
//            catch (Exception ex)
//            {
//                await transaction.RollbackAsync();
//                _logger.LogError(ex, "Error in DeleteOrderItemByIdAsync for OrderItemId: {OrderItemId}", orderItemId);
//                return false;
//            }
//        }

//        public async Task<bool> OrderItemExistsByIdAsync(int orderItemId)
//        {
//            try
//            {
//                const string sql = "SELECT 1 FROM OrderItems WHERE OrderItemId = @OrderItemId LIMIT 1";

//                await using var connection = await _dataSource.OpenConnectionAsync();
//                var result = await connection.ExecuteScalarAsync<int?>(sql, new { OrderItemId = orderItemId });

//                return result.HasValue;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in OrderItemExistsByIdAsync for OrderItemId: {OrderItemId}", orderItemId);
//                return false;
//            }
//        }
//    }
//}