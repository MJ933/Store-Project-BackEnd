using StoreDataAccessLayer;
using System.Collections.Generic;

namespace StoreBusinessLayer
{
    public class clsOrderItemsBL
    {
        public enum enMode { AddNew = 0, Update = 1 }
        public enMode Mode { get; set; } = enMode.AddNew;

        public OrderItemDTO DTO { get; set; }

        private readonly clsOrderItemsDAL _orderItemsDAL;

        // Constructor now MANDATORY requires an instance of clsOrderItemsDAL for Dependency Injection
        public clsOrderItemsBL(OrderItemDTO orderItemDTO, clsOrderItemsDAL orderItemsDAL, enMode mode = enMode.AddNew)
        {
            DTO = orderItemDTO;
            Mode = mode;
            _orderItemsDAL = orderItemsDAL ?? throw new ArgumentNullException(nameof(orderItemsDAL)); // Ensure DAL is injected
        }
        public clsOrderItemsBL(clsOrderItemsDAL orderItemsDAL)
        {

            _orderItemsDAL = orderItemsDAL ?? throw new ArgumentNullException(nameof(orderItemsDAL)); // Ensure DAL is injected
        }

        // Static factory method for creating BL instances with AddNew mode. Useful for API Controller POST
        public clsOrderItemsBL CreateNewOrderItemBL(OrderItemDTO orderItemDTO, clsOrderItemsDAL orderItemsDAL)
        {
            return new clsOrderItemsBL(orderItemDTO, orderItemsDAL, enMode.AddNew);
        }

        // Static factory method for getting BL instances in Update mode. Useful when retrieving existing order items by ID.
        public clsOrderItemsBL GetOrderItemBLByOrderItemID(int id, clsOrderItemsDAL orderItemsDAL)
        {
            OrderItemDTO orderItemDto = orderItemsDAL.GetOrderItemByOrderItemID(id);

            if (orderItemDto != null)
            {
                return new clsOrderItemsBL(orderItemDto, orderItemsDAL, enMode.Update);
            }
            else return null;
        }


        public List<OrderItemDTO> GetAllOrderItems()
        {
            return _orderItemsDAL.GetAllOrderItems();
        }

        public async Task<List<OrderItemDTO>> GetAllOrderItemsByOrderID(int id)
        {
            return await _orderItemsDAL.GetAllOrderItemsByOrderID(id);
        }

        public clsOrderItemsBL GetOrderItemByOrderItemID(int id)
        {
            OrderItemDTO orderItemDto = _orderItemsDAL.GetOrderItemByOrderItemID(id);

            if (orderItemDto != null)
            {
                return new clsOrderItemsBL(orderItemDto, _orderItemsDAL, enMode.Update); // Pass _orderItemsDAL instance here as well
            }
            else return null;
        }

        private async Task<bool> _AddNewOrderItem()
        {
            int orderItemID = await _orderItemsDAL.AddOrderItem(this.DTO);
            if (orderItemID > 0)
            {
                this.DTO.OrderItemID = orderItemID;
            }
            return orderItemID > 0;
        }

        private async Task<bool> _UpdateOrderItem()
        {
            return await _orderItemsDAL.UpdateOrderItem(this.DTO);
        }

        public async Task<bool> Save()
        {
            switch (Mode)
            {
                case enMode.AddNew:
                    if (await _AddNewOrderItem())
                    {
                        Mode = enMode.Update;
                        return true;
                    }
                    else return false;
                case enMode.Update:
                    return await _UpdateOrderItem();
            }
            return false;
        }

        public async Task<bool> DeleteOrderItemByOrderItemID(int id)
        {
            return await _orderItemsDAL.DeleteOrderItemByOrderItemID(id);
        }

        public bool IsOrderItemExistsByOrderItemID(int id)
        {
            return _orderItemsDAL.IsOrderItemExistsByOrderItemID(id);
        }
    }
}