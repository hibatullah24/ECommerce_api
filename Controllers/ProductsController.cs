using ECommerce_api;
using ECommerce_api.DTOs;
using ECommerce_api.Models;
using ECommerce_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce_api_api.Controllers
{
    [ApiController]
    [Route("api/Products")]
    public class ProductsController : ControllerBase
    {
        

        public ApplicationDbContext _context;
        public LoggingService<ProductsController> _logger;

        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = new LoggingService<ProductsController>(logger);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost("AddProduct")]
         public IActionResult AddProduct(int adminId, AddProductRequest request)
        {
            try
            {
                var requestingUser = _context.Users.Find(adminId);
                if (requestingUser == null || requestingUser.Role != "Admin")
                {
                    _logger.LogWarning("Unauthorized access attempt to AddProduct by user ID {AdminId}", adminId);
                    return Unauthorized("Access denied. Admins only.");
                }


                if (string.IsNullOrWhiteSpace(request.PName))
                    return BadRequest("Product name is required.");

                if (string.IsNullOrWhiteSpace(request.Description))
                    return BadRequest("Product description is required.");

                if (request.Price <= 0)
                    return BadRequest("Product price must be greater than zero.");

                if (request.Stock < 0)
                    return BadRequest("Product stock cannot be negative.");

                var product = new Product
                {
                    PName = request.PName,
                    Description = request.Description,
                    Price = request.Price,
                    Stock = request.Stock

                };

                _context.Products.Add(product);
                _context.SaveChanges();
                _logger.LogInfo("Product {ProductId} '{ProductName}' added by Admin {AdminId}.", product.PId, product.PName, adminId);

                return Ok("Product added successfully.");
            }
            catch(Exception ex)
            {
                _logger.LogError("AddProduct failed with an unexpected error: {Message}", ex.Message);
                return BadRequest("An unexpected error occurred while adding the product.");
            }

            [Authorize(Roles = "Admin")]
            [HttpPut("UpdateProduct")]
            public IActionResult UpdateProduct(int adminId, int id, UpdateProductRequest request)
            {
                try
                {
                    var requestingUser = _context.Users.Find(adminId);
                    if (requestingUser == null || requestingUser.Role != "Admin")
                    {
                        _logger.LogWarning("Unauthorized access attempt to UpdateProduct by user ID {AdminId}", adminId);
                        return Unauthorized("Access denied. Admins only.");

                    }
                    var product = _context.Products.Find(id);
                    if (product == null)
                    {
                        _logger.LogWarning("Product with ID {ProductId} not found for update by Admin {AdminId}.", id, adminId);
                        return NotFound("Product not found");
                    }

                    if (request.Price <= 0)
                        return BadRequest("Price must be greater than zero.");

                    if (request.Stock < 0)
                        return BadRequest("Stock cannot be negative.");

                    product.Price = request.Price;
                    product.Stock = request.Stock;
                    _context.Products.Update(product);
                    _context.SaveChanges();

                    _logger.LogInfo("Product {ProductId} updated by Admin {AdminId}. New Price: {Price}, New Stock: {Stock}.", product.PId, adminId, product.Price, product.Stock);
                    return Ok("Product updated successfully.");
                }
                catch(Exception ex)
                {
                    _logger.LogError("UpdateProduct failed with an unexpected error: {Message}", ex.Message);
                    return BadRequest("An unexpected error occurred while updating the product.");
                }
            }
            
        }

        [AllowAnonymous]
        [HttpGet("GetAllProducts")]
        public IActionResult GetAllProducts( int page =1)
        {

            try
            {
                const int pagesize = 10;

                int totalCount = _context.Products.Count();
                if (totalCount == 0)
                {
                    _logger.LogWarning("GetAllProducts: No products found.");
                    return NotFound("No products available.");

                }

                int totalPages = (int)Math.Ceiling(totalCount / (double)pagesize);

                if (page < 1 || page > totalPages)
                    return BadRequest($"Invalid page number.Valid range: 1 - {totalPages}.");

                var products = _context.Products.OrderBy(p => p.PName)
                                              .Skip((page - 1) * pagesize)
                                               .Take(pagesize)
                                               .ToList();

                _logger.LogInfo("GetAllProducts: Returned page {Page} of {TotalPages}.", page, totalPages);

                return Ok(products);
            }
            catch(Exception ex)
            {
                _logger.LogError("GetAllProducts failed with an unexpected error: {Message}", ex.Message);
                return BadRequest("\"An unexpected error occurred while retrieving products.\"");
            }
        }


        [AllowAnonymous]
        [HttpGet("GetProductById")]
        public IActionResult GetProductById ( int id)
        {
            try
            {
                var product = _context.Products.Find(id);
                if (product == null)
                {
                    _logger.LogWarning("GetProductById: Product {ProductId} not found.", id);
                    return NotFound("Product not found.");

                }

                _logger.LogInfo("GetProductById: Product {ProductId} retrieved.", id);
                return Ok(product);
            }
            catch(Exception ex)
            {
                _logger.LogError("GetProductById failed with an unexpected error: {Message}", ex.Message);
                return BadRequest("An unexpected error occurred while retrieving the product.");
            }



        }



       

       


    }
}