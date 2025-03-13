using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StoreBusinessLayer;
using StoreDataAccessLayer;
using System.Collections.Generic;

namespace StoreAPI.Controllers
{
    [Route("API/OrdersAPI")]
    [ApiController]
    public class OrdersAPIController : ControllerBase
    {
        private readonly IOrdersService _ordersService;

        public OrdersAPIController(IOrdersService ordersBL)
        {
            _ordersService = ordersBL;
        }

        [HttpGet("GetAll", Name = "GetAllOrders")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetAllOrders()
        {
            var ordersList = await _ordersService.GetAllOrdersAsync();
            if (ordersList.Count == 0)
                return NotFound("There are no orders in the database!");
            return Ok(ordersList);
        }


        [HttpGet("GetOrdersPaginatedWithFilters", Name = "GetOrdersPaginatedWithFilters")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetOrdersPaginatedWithFilters( // Changed return type to object or a custom PagedResult<OrderDTO>
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? orderID = null,
            [FromQuery] int? customerID = null,
            [FromQuery] DateTime? orderDate = null,
            [FromQuery] decimal? total = null,
            [FromQuery] string? orderStatus = null,
            [FromQuery] string? shippingAddress = null,
            [FromQuery] string? notes = null
        )
        {
            // Validate input
            if (pageNumber < 1)
                return BadRequest("Page number must be greater than or equal to 1.");

            if (pageSize < 1)
                return BadRequest("Page size must be greater than or equal to 1.");

            // Call the paging method from clsOrdersDAL
            var result = await _ordersService.GetOrdersPaginatedWithFiltersAsync(pageNumber, pageSize, orderID, customerID, orderDate,
                total, orderStatus, shippingAddress, notes);

            // Handle empty results
            if (result.OrdersList == null || result.OrdersList.Count == 0)
                return NotFound("No orders found for the requested page.");

            // Return the paged results and total count
            return Ok(new
            {
                TotalCount = result.TotalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                OrderList = result.OrdersList // Changed ProductList to OrderList
            });
        }
        [HttpGet("GetOrderByID/{OrderID}", Name = "GetOrderByID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public async Task<ActionResult<OrderDTO>> GetOrderByID([FromRoute] int OrderID)
        {
            if (OrderID < 1)
                return BadRequest($"Invalid OrderID: {OrderID}");
            var orderDTO = await _ordersService.GetOrderByOrderIDAsync(OrderID);
            if (orderDTO == null)
                return NotFound($"No order found with OrderID: {OrderID}");
            return Ok(orderDTO);
        }

        [HttpGet("GetOrdersByCustomerID/{CustomerID}", Name = "GetOrdersByCustomerID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetOrdersByCustomerID([FromRoute] int CustomerID)
        {
            if (CustomerID < 1)
                return BadRequest($"Invalid Customer ID: {CustomerID}");
            var ordersList = await _ordersService.GetOrderByCustomerIDAsync(CustomerID);
            if (ordersList.Count == 0)
                return NotFound($"No orders found for Customer ID: {CustomerID}");
            return Ok(ordersList);
        }


        [HttpPost("Create", Name = "AddOrder")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult<OrderDTO>> AddOrder([FromBody] OrderDTO newOrderDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _ordersService.Order = newOrderDTO;
            if (await _ordersService.AddNewOrderAsync())
            {
                return CreatedAtRoute("GetOrderByID", new { OrderID = _ordersService.Order.OrderID }, _ordersService.Order);
            }
            else
            {
                return BadRequest("Failed to add the order.");
            }
        }

        [HttpPut("Update/{OrderID}", Name = "UpdateOrderAsync")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,admin")]
        public async Task<ActionResult<OrderDTO>> UpdateOrder([FromRoute] int OrderID, [FromBody] OrderDTO updatedOrderDTO)
        {
            if (OrderID < 1)
                return BadRequest($"Invalid ID: {OrderID}");
            updatedOrderDTO = new OrderDTO(OrderID, updatedOrderDTO.CustomerID, updatedOrderDTO.OrderDate,
                     updatedOrderDTO.Total, updatedOrderDTO.OrderStatus, updatedOrderDTO.ShippingAddress, updatedOrderDTO.Notes);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            OrderDTO? oldOrderDto = await _ordersService.GetOrderByOrderIDAsync(OrderID);
            if (oldOrderDto == null)
                return NotFound($"No order found with ID: {OrderID}");
            _ordersService.Order = updatedOrderDTO;
            if (await _ordersService.UpdateOrderAsync())
            {
                return Ok(updatedOrderDTO);
            }
            else
            {
                return BadRequest("Failed to update the order.");
            }
        }

        [HttpDelete("Delete/{OrderID}", Name = "DeleteOrder")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize]
        public async Task<ActionResult> DeleteOrder([FromRoute] int OrderID)
        {
            if (OrderID <= 0)
                return BadRequest($"Invalid ID: {OrderID}");
            if (!await _ordersService.IsOrderExistsByOrderIDAsync(OrderID))
                return NotFound($"No order found with ID: {OrderID}");
            if (await _ordersService.DeleteOrderAsync(OrderID))
                return Ok($"Order with ID: {OrderID} was deleted successfully.");
            else
                return StatusCode(500, "Failed to delete the order.");
        }

        [HttpPatch("UpdateStatus/{OrderID}", Name = "UpdateOrderStatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<ActionResult> UpdateOrderStatus([FromRoute] int OrderID, [FromBody] string status)
        {
            if (OrderID < 1)
                return BadRequest($"Invalid ID: {OrderID}");
            if (string.IsNullOrEmpty(status))
                return BadRequest("Status cannot be empty.");
            if (!await _ordersService.IsOrderExistsByOrderIDAsync(OrderID))
                return NotFound($"No order found with ID: {OrderID}");
            if (await _ordersService.UpdateOrderStatusByOrderIDAsync(OrderID, status))
                return Ok($"Order status updated to: {status}");
            else
                return BadRequest("Failed to update the order status.");
        }
    }
}