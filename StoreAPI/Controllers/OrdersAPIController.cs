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
        private readonly clsOrdersBL _ordersBL;

        public OrdersAPIController(clsOrdersBL ordersBL)
        {
            _ordersBL = ordersBL;
        }

        [HttpGet("GetAll", Name = "GetAllOrders")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetAllOrders()
        {
            var ordersList = await _ordersBL.GetAllOrders();
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
            var result = await _ordersBL.GetOrdersPaginatedWithFilters(pageNumber, pageSize, orderID, customerID, orderDate,
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



        [HttpGet("GetOrderByID/{id}", Name = "GetOrderByOrderID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,marketing,admin")]
        public async Task<ActionResult<OrderDTO>> GetOrderByID([FromRoute] int id)
        {
            if (id < 1)
                return BadRequest($"Invalid ID: {id}");
            var orderBL = await _ordersBL.GetOrderByOrderID(id);
            if (orderBL == null)
                return NotFound($"No order found with ID: {id}");
            return Ok(orderBL.DTO);
        }

        [HttpGet("GetOrdersByCustomerID/{id}", Name = "GetOrdersByCustomerID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetOrdersByCustomerID([FromRoute] int id)
        {
            if (id < 1)
                return BadRequest($"Invalid Customer ID: {id}");
            var ordersList = await _ordersBL.GetOrderByCustomerID(id);
            if (ordersList.Count == 0)
                return NotFound($"No orders found for Customer ID: {id}");
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
            clsOrdersBL orderBL = new clsOrdersBL(newOrderDTO);
            if (await orderBL.Save())
            {
                return CreatedAtRoute("GetOrderByOrderID", new { id = orderBL.DTO.OrderID }, newOrderDTO);
            }
            else
            {
                return BadRequest("Failed to add the order.");
            }
        }

        [HttpPut("Update/{id}", Name = "UpdateOrder")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "sales,admin")]
        public async Task<ActionResult<OrderDTO>> UpdateOrder([FromRoute] int id, [FromBody] OrderDTO updatedOrderDTO)
        {
            if (id < 1)
                return BadRequest($"Invalid ID: {id}");
            updatedOrderDTO.OrderID = id;
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var orderBL = await _ordersBL.GetOrderByOrderID(id);
            if (orderBL == null)
                return NotFound($"No order found with ID: {id}");
            orderBL.DTO = updatedOrderDTO;
            if (await orderBL.Save())
            {
                return Ok(updatedOrderDTO);
            }
            else
            {
                return BadRequest("Failed to update the order.");
            }
        }

        [HttpDelete("Delete/{id}", Name = "DeleteOrder")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize]
        public async Task<ActionResult> DeleteOrder([FromRoute] int id)
        {
            if (id <= 0)
                return BadRequest($"Invalid ID: {id}");
            if (!await _ordersBL.IsOrderExistsByOrderID(id))
                return NotFound($"No order found with ID: {id}");
            if (await _ordersBL.DeleteOrderByOrderID(id))
                return Ok($"Order with ID: {id} was deleted successfully.");
            else
                return StatusCode(500, "Failed to delete the order.");
        }

        [HttpPatch("UpdateStatus/{id}", Name = "UpdateOrderStatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<ActionResult> UpdateOrderStatus([FromRoute] int id, [FromBody] string status)
        {
            if (id < 1)
                return BadRequest($"Invalid ID: {id}");
            if (string.IsNullOrEmpty(status))
                return BadRequest("Status cannot be empty.");
            if (!await _ordersBL.IsOrderExistsByOrderID(id))
                return NotFound($"No order found with ID: {id}");
            if (await _ordersBL.UpdateOrderStatusByOrderID(id, status))
                return Ok($"Order status updated to: {status}");
            else
                return BadRequest("Failed to update the order status.");
        }
    }
}