using StoreDataAccessLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreBusinessLayer
{
    public interface IOrdersService
    {
        Task<List<OrderDTO>> GetAllOrdersAsync();
        Task<(List<OrderDTO> OrdersList, int TotalCount)> GetOrdersPaginatedWithFiltersAsync(
         int pageNumber, int pageSize, int? orderID, int? customerID, DateTime? orderDate,
         decimal? total, string? orderStatus, string? shippingAddress, string? notes);
        Task<OrderDTO?> GetOrderByOrderIDAsync(int OrderID);
        Task<List<OrderDTO>> GetOrderByCustomerIDAsync(int CustomerID);
        Task<bool> AddNewOrderAsync();
        Task<bool> UpdateOrderAsync();
        Task<bool> DeleteOrderAsync(int OrderID);
        Task<bool> UpdateOrderStatusByOrderIDAsync(int OrderID, string OrderStatus);
        Task<bool> IsOrderExistsByOrderIDAsync(int OrderID);
        public OrderDTO Order { get; set; }

    }
    public class OrdersService : IOrdersService
    {
        public enum enMode { AddNew = 1, Update = 2 }
        public enMode Mode = enMode.AddNew;
        public OrderDTO Order { get; set; }

        private readonly IOrdersRepository _ordersRepository;
        public OrdersService(IOrdersRepository ordersRepository)
        {
            _ordersRepository = ordersRepository ?? throw new ArgumentNullException(nameof(ordersRepository));
        }
        public async Task<List<OrderDTO>> GetAllOrdersAsync()
        {
            return await _ordersRepository.GetAllOrdersAsync();
        }
        public async Task<(List<OrderDTO> OrdersList, int TotalCount)> GetOrdersPaginatedWithFiltersAsync(
           int pageNumber, int pageSize, int? orderID, int? customerID, DateTime? orderDate,
           decimal? total, string? orderStatus, string? shippingAddress, string? notes)
        {
            return await _ordersRepository.GetOrdersPaginatedWithFiltersAsync(pageNumber, pageSize, orderID, customerID, orderDate, total, orderStatus, shippingAddress, notes);
        }

        public async Task<OrderDTO?> GetOrderByOrderIDAsync(int id)
        {
            return await _ordersRepository.GetOrderByOrderIDAsync(id);
        }

        public async Task<List<OrderDTO>> GetOrderByCustomerIDAsync(int id)
        {
            return await _ordersRepository.GetOrderByCustomerIDAsync(id);
        }
        public async Task<bool> AddNewOrderAsync()
        {
            int orderID = await _ordersRepository.AddOrderAsync(this.Order);
            if (orderID > 0)
                Order = new OrderDTO(orderID, this.Order.CustomerID, this.Order.OrderDate, this.Order.Total,
               this.Order.OrderStatus, this.Order.ShippingAddress, this.Order.Notes);

            return orderID > 0;
        }
        public async Task<bool> UpdateOrderAsync()
        {
            return await _ordersRepository.UpdateOrderAsync(this.Order);
        }



        public async Task<bool> DeleteOrderAsync(int id)
        {
            return await _ordersRepository.DeleteOrderAsync(id);
        }

        public async Task<bool> UpdateOrderStatusByOrderIDAsync(int id, string status)
        {
            return await _ordersRepository.UpdateOrderStatusByOrderIDAsync(id, status);
        }
        public async Task<bool> IsOrderExistsByOrderIDAsync(int id)
        {
            return await _ordersRepository.IsOrderExistsByOrderIDAsync(id);
        }

    }
}
