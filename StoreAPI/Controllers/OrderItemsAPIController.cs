using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StoreBusinessLayer;
using StoreDataAccessLayer;
using System.Collections.Generic;

namespace StoreAPI.Controllers
{
    [Route("API/OrderItemsAPI")]
    [ApiController]
    //[Authorize] // Apply Authorize attribute at the controller level to require authentication for all actions
    public class OrderItemsAPIController : ControllerBase
    {
        private readonly clsOrderItemsBL _orderItemsBL;

        public OrderItemsAPIController(clsOrderItemsBL orderItemsBL)
        {
            _orderItemsBL = orderItemsBL;
        }


        [HttpGet("GetAll", Name = "GetAllOrderItems")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        //[Authorize(Roles = "sales,marketing,admin")] // Optional: Restrict access to specific roles

        public ActionResult<IEnumerable<OrderItemDTO>> GetAllOrderItems()
        {
            var orderItemsList = _orderItemsBL.GetAllOrderItems();
            if (orderItemsList.Count == 0)
                return NotFound("There are no order items in the database!");
            return Ok(orderItemsList);
        }

        [HttpGet("GetOrderItemByOrderItemID/{id}", Name = "GetOrderItemByOrderItemID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        //[Authorize] // Already authorized at controller level, keep this if you want to override controller level auth.

        public ActionResult<OrderItemDTO> GetOrderItemByOrderItemID([FromRoute] int id)
        {
            if (id < 1)
                return BadRequest($"Not Accepted ID {id}");
            var orderItemBL = _orderItemsBL.GetOrderItemByOrderItemID(id);
            if (orderItemBL == null)
                return NotFound($"There is no order item with ID {id}");
            return Ok(orderItemBL.DTO);
        }

        [HttpGet("GetOrderItemByOrderID/{id}", Name = "GetOrderItemByOrderID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize] // Already authorized at controller level, keep this if you want to override controller level auth.

        public async Task<ActionResult<IEnumerable<OrderItemDTO>>> GetAllOrderItemsByOrderID([FromRoute] int id)
        {
            if (id < 1)
                return BadRequest($"Not Accepted ID {id}");
            var orderItemsList = await _orderItemsBL.GetAllOrderItemsByOrderID(id);
            if (orderItemsList.Count == 0)
                return NotFound("There are no order items in the database!");
            return Ok(orderItemsList);
        }





        [HttpPost("Create", Name = "AddOrderItem")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        //[Authorize] // Restrict creation to admin and manager roles

        public async Task<ActionResult<OrderItemDTO>> AddOrderItem(OrderItemDTO newOrderItemDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            _orderItemsBL.DTO = newOrderItemDTO;
            if (await _orderItemsBL.Save())
            {

                return CreatedAtRoute("GetOrderItemByOrderItemID", new { id = _orderItemsBL.DTO.OrderItemID }, newOrderItemDTO);
            }
            else
            {
                return BadRequest("Failed to add the order item.");
            }
        }

        [HttpPut("Update/{id}", Name = "UpdateOrderItem")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize] // Restrict update to admin and manager roles

        public async Task<ActionResult<OrderItemDTO>> UpdateOrderItem([FromRoute] int id, [FromBody] OrderItemDTO updatedOrderItemDTO)
        {
            if (id < 1)
                return BadRequest($"Not Accepted ID {id}");
            updatedOrderItemDTO.OrderItemID = id;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            clsOrderItemsBL orderItemBL = _orderItemsBL.GetOrderItemByOrderItemID(id);
            if (orderItemBL == null)
                return NotFound($"There is no order item with ID = {id}");
            orderItemBL.DTO = updatedOrderItemDTO;
            if (await orderItemBL.Save())
            {
                return Ok(updatedOrderItemDTO);
            }
            else
            {
                return BadRequest("Failed to update the order item.");
            }
        }

        [HttpDelete("Delete/{id}", Name = "DeleteOrderItem")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize] // Restrict deletion to admin role

        public async Task<ActionResult> DeleteOrderItem([FromRoute] int id)
        {
            if (id <= 0)
                return BadRequest($"Please enter a valid ID = {id}");
            if (!_orderItemsBL.IsOrderItemExistsByOrderItemID(id))
                return NotFound($"There is no order item with ID = {id}");
            if (await _orderItemsBL.DeleteOrderItemByOrderItemID(id))
                return Ok($"The order item was deleted successfully with ID = {id}");
            else
                return StatusCode(500, $"ERROR, the order item was NOT deleted. No rows were affected!");
        }
    }
}