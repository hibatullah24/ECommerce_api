using E_Commerce_System;
using E_Commerce_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce_system_api.Controllers
{
    [ApiController]
    [Route("api/Orders")]
    public class OrdersController : ControllerBase
    {
        ApplicationDbContext context = new ApplicationDbContext();

        [HttpPost("PlaceOrder")]
        public IActionResult PlaceOrder(PlaceOrderRequest request)
        {
            var userExists = context.Users.Find(request.UserId);
            if (userExists == null)
                return NotFound("User not found.");

            if (request.Items == null || !request.Items.Any())
                return BadRequest("Order must contain at least one item.");

            var order = new Order
            {
                UId = request.UserId,
                OrderDate = DateTime.Now
            };
            context.Orders.Add(order);
            context.SaveChanges();

            foreach (var item in request.Items)
            {
                if (item.Quantity <= 0)
                    return BadRequest($"Invalid quantity for product ID {item.ProductId}.");

                var product = context.Products.Find(item.ProductId);
                if (product == null)
                    return NotFound($"Product with ID {item.ProductId} not found.");

                if (product.Stock < item.Quantity)
                    return BadRequest($"Not enough stock for '{product.PName}'. Available: {product.Stock}.");

                context.OrderProducts.Add(new OrderProduct
                {
                    OId = order.OId,
                    PId = product.PId,
                    Quantity = item.Quantity
                });

                product.Stock -= item.Quantity;
            }

            context.SaveChanges();

            var fullOrder = context.Orders
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.Product)
                .FirstOrDefault(o => o.OId == order.OId);

            decimal total = fullOrder.OrderProducts.Sum(op => op.Quantity * op.Product.Price);

            return Ok(new { message = "Order placed successfully.", orderId = order.OId, totalAmount = total });
        }

        [HttpGet("GetUserOrders")]
        public IActionResult GetUserOrders(int userId)
        {
            var userExists = context.Users.Find(userId);
            if (userExists == null)
                return NotFound("User not found.");

            var orders = context.Orders
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.Product)
                .Where(o => o.UId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            if (!orders.Any())
                return NotFound("No orders found.");

     
            var result = orders.Select(o => new
            {
                orderId = o.OId,
                orderDate = o.OrderDate,
                totalAmount = o.OrderProducts.Sum(op => op.Quantity * op.Product.Price),
                items = o.OrderProducts.Select(op => new
                {
                    productId = op.PId,
                    productName = op.Product.PName,
                    quantity = op.Quantity,
                    unitPrice = op.Product.Price,
                    subtotal = op.Quantity * op.Product.Price
                }).ToList()
            }).ToList();

            return Ok(result);
        }

        //DTOs
        public class PlaceOrderRequest
        {
            public int UserId { get; set; }
            public List <OrderItemRequest> Items { get; set; }
        }

        public class OrderItemRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }








    }
}
