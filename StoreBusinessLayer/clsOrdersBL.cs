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

        public List<OrderDTO> GetAllOrders()
        {
            return _ordersDAL.GetAllOrders();
        }
        public (List<OrderDTO> OrdersList, int TotalCount) GetOrdersPaginatedWithFilters(
           int pageNumber, int pageSize, int? orderID, int? customerID, DateTime? orderDate,
           decimal? total, string? orderStatus, string? shippingAddress, string? notes)
        {
            return _ordersDAL.GetOrdersPaginatedWithFilters(pageNumber, pageSize, orderID, customerID, orderDate, total, orderStatus, shippingAddress, notes);
        }

        public clsOrdersBL GetOrderByOrderID(int id)
        {
            OrderDTO orderDto = _ordersDAL.GetOrderByOrderID(id);
            if (orderDto != null)
            {
                return new clsOrdersBL(orderDto, enMode.Update);
            }
            else return null;
        }

        public List<OrderDTO> GetOrderByCustomerID(int id)
        {
            return _ordersDAL.GetOrderByCustomerID(id);
        }

        private bool _AddNewOrder()
        {
            int orderID = _ordersDAL.AddOrder(this.DTO);
            if (orderID > 0)
            {
                this.DTO.OrderID = orderID;
            }
            return orderID > 0;
        }

        private bool _UpdateOrder()
        {
            return _ordersDAL.UpdateOrder(this.DTO);
        }

        public bool Save()
        {
            switch (Mode)
            {
                case enMode.AddNew:
                    if (_AddNewOrder())
                    {
                        Mode = enMode.Update;
                        return true;
                    }
                    else return false;
                case enMode.Update:
                    return _UpdateOrder();
            }
            return false;
        }

        public bool DeleteOrderByOrderID(int id)
        {
            return _ordersDAL.DeleteOrderByOrderID(id);
        }

        public bool UpdateOrderStatusByOrderID(int id, string status)
        {
            return _ordersDAL.UpdateOrderStatusByOrderID(id, status);
        }
        public bool IsOrderExistsByOrderID(int id)
        {
            return _ordersDAL.IsOrderExistsByOrderID(id);
        }
    }
}