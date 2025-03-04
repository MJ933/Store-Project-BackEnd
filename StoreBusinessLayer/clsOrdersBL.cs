using StoreDataAccessLayer;
using System;
using System.Collections.Generic;

namespace StoreBusinessLayer
{
    public class clsOrdersBL
    {
        public enum enMode { AddNew = 0, Update = 1 }
        public enMode Mode { get; private set; } = enMode.AddNew;

        public OrderDTO DTO { get; set; }

        private readonly clsOrdersDAL _ordersDAL;

        public clsOrdersBL(clsOrdersDAL ordersDAL)
        {
            _ordersDAL = ordersDAL;
        }
        public clsOrdersBL(OrderDTO orderDTO, enMode mode = enMode.AddNew)
        {
            DTO = orderDTO;
            Mode = mode;
            _ordersDAL = new clsOrdersDAL(clsDataAccessSettingsDAL.CreateDataSource());
        }

        public async Task<List<OrderDTO>> GetAllOrders()
        {
            return await _ordersDAL.GetAllOrders();
        }
        public async Task<(List<OrderDTO> OrdersList, int TotalCount)> GetOrdersPaginatedWithFilters(
           int pageNumber, int pageSize, int? orderID, int? customerID, DateTime? orderDate,
           decimal? total, string? orderStatus, string? shippingAddress, string? notes)
        {
            return await _ordersDAL.GetOrdersPaginatedWithFilters(pageNumber, pageSize, orderID, customerID, orderDate, total, orderStatus, shippingAddress, notes);
        }

        public async Task<clsOrdersBL> GetOrderByOrderID(int id)
        {
            OrderDTO orderDto = await _ordersDAL.GetOrderByOrderID(id);
            if (orderDto != null)
            {
                return new clsOrdersBL(orderDto, enMode.Update);
            }
            else return null;
        }

        public async Task<List<OrderDTO>> GetOrderByCustomerID(int id)
        {
            return await _ordersDAL.GetOrderByCustomerID(id);
        }

        private async Task<bool> _AddNewOrder()
        {
            int orderID = await _ordersDAL.AddOrder(this.DTO);
            if (orderID > 0)
            {
                this.DTO.OrderID = orderID;
            }
            return orderID > 0;
        }

        private async Task<bool> _UpdateOrder()
        {
            return await _ordersDAL.UpdateOrder(this.DTO);
        }

        public async Task<bool> Save()
        {
            switch (Mode)
            {
                case enMode.AddNew:
                    if (await _AddNewOrder())
                    {
                        Mode = enMode.Update;
                        return true;
                    }
                    else return false;
                case enMode.Update:
                    return await _UpdateOrder();
            }
            return false;
        }

        public async Task<bool> DeleteOrderByOrderID(int id)
        {
            return await _ordersDAL.DeleteOrderByOrderID(id);
        }

        public async Task<bool> UpdateOrderStatusByOrderID(int id, string status)
        {
            return await _ordersDAL.UpdateOrderStatusByOrderID(id, status);
        }
        public async Task<bool> IsOrderExistsByOrderID(int id)
        {
            return await _ordersDAL.IsOrderExistsByOrderID(id);
        }
    }
}