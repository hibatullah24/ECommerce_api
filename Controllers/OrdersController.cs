using ECommerce_api;
using ECommerce_api.DTOs;
using ECommerce_api.Models;
using ECommerce_api.Services;
using ECommerce_api_api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce_api.Controllers
{
    [ApiController]
    [Route("api/Orders")]
    public class OrdersController : ControllerBase
    {
        
        

        public ApplicationDbContext _context;
        public LoggingService<OrdersController> _logger;
        public JwtService _jwtService;
        public EmailService _emailService;

        public OrdersController(ApplicationDbContext context, ILogger<OrdersController> logger, JwtService jwtService, EmailService emailService)
        {
            _context = context;
            _logger = new LoggingService<OrdersController>(logger);
            _jwtService = jwtService;
            _emailService = emailService;
        }
        [Authorize]
        [HttpPost("PlaceOrder")]
        public IActionResult PlaceOrder(DTOs.PlaceOrderRequest request)
        {
            var userExists = _context.Users.Find(request.UserId);
            if (userExists == null)
            {
                _logger.LogWarning("User with ID {UserId} not found.", request.UserId);
                return NotFound("User not found.");
            }

            if (request.Items == null || !request.Items.Any())
            {
                _logger.LogWarning("Order placement failed for User {UserId}: No items provided.", request.UserId);
                return BadRequest("Order must contain at least one item.");
            }

            var order = new Order
            {
                UId = request.UserId,
                OrderDate = DateTime.Now
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            foreach (var item in request.Items)
            {
                if (item.Quantity <= 0)
                {
                    _logger.LogWarning("PlaceOrder failed: Invalid quantity for Product {ProductId}.", item.ProductId);
                    return BadRequest($"Invalid quantity for product ID {item.ProductId}.");
                }

                var product = _context.Products.Find(item.ProductId);
                if (product == null)
                {
                    _logger.LogWarning("PlaceOrder failed: Product {ProductId} not found.", item.ProductId);
                    return NotFound($"Product with ID {item.ProductId} not found.");
                }

                if (product.Stock < item.Quantity)
                {
                    _logger.LogWarning("PlaceOrder failed: Not enough stock for Product {ProductId}. Requested: {Requested}, Available: {Available}.", item.ProductId, item.Quantity, product.Stock);
                    return BadRequest($"Not enough stock for '{product.PName}'. Available: {product.Stock}.");
                }

                _context.OrderProducts.Add(new OrderProduct
                {
                    OId = order.OId,
                    PId = product.PId,
                    Quantity = item.Quantity
                });

                product.Stock -= item.Quantity;
            }

            _context.SaveChanges();

            var fullOrder = _context.Orders
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.Product)
                .FirstOrDefault(o => o.OId == order.OId);

            decimal total = fullOrder.OrderProducts.Sum(op => op.Quantity * op.Product.Price);

            _logger.LogInfo("Order {OrderId} placed successfully for User {UserId}. Total: {Total}.", order.OId, request.UserId, total);
            return Ok(new { message = "Order placed successfully.", orderId = order.OId, totalAmount = total });
        }

        [Authorize]
        [HttpGet("GetUserOrders")]
        public IActionResult GetUserOrders(int userId)
        {
            var userExists = _context.Users.Find(userId);
            if (userExists == null)
            {
                _logger.LogWarning("GetUserOrders failed: User {UserId} not found.", userId);
                return NotFound("User not found.");
            }

            var orders = _context.Orders
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.Product)
                .Where(o => o.UId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            if (!orders.Any())
            {
                _logger.LogWarning("GetUserOrders: No orders found for User {UserId}.", userId);
                return NotFound("No orders found.");
            }

            _logger.LogInfo("GetUserOrders: Retrieved {Count} orders for User {UserId}.", orders.Count, userId);

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
    }
}