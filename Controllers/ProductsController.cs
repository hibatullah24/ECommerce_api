using E_Commerce_System;
using E_Commerce_System.Models;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce_system_api.Controllers
{
    [ApiController]
    [Route("api/Products")]
    public class ProductsController : ControllerBase
    {
        ApplicationDbContext context = new ApplicationDbContext();

         [HttpPost("AddProduct")]
         public IActionResult AddProduct(AddProductRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PName))
                return BadRequest("Product name is required.");

            if (string.IsNullOrWhiteSpace(request.Description))
                return BadRequest("Product description is required.");

            if (request.Price <= 0)
                return BadRequest("Product price must be greater than zero.");

            if (request.Stock <0)
                return BadRequest ("Product stock cannot be negative.");

            var product = new Product
            {
                PName = request.PName,
                Description = request.Description,
                Price = request.Price,
                Stock = request.Stock

            };

            context.Products.Add(product);
            context.SaveChanges();
            return Ok("Product added successfully.");
        }

        [HttpPut("UpdateProduct")]
        public IActionResult UpdateProduct (int id, UpdateProductRequest request)
        {
            var product = context.Products.Find(id);
            if (product == null)
                return NotFound("Product not found");

            if (request.Price <= 0)
                return BadRequest("Price must be greater than zero.");

            if (request.Stock < 0)
                return BadRequest("Stock cannot be negative.");

            product.Price = request.Price;
            product.Stock = request.Stock;
            context.Products.Update(product);
            context.SaveChanges();
            return Ok("Product updated successfully.");
        }

        [HttpGet("GetAllProducts")]
        public IActionResult GetAllProducts(int page =1)
        {
            const int pagesize = 10;

            int totalCount = context.Products.Count();
            if (totalCount == 0)
                return NotFound("No products available.");

            int totalPages = (int)Math.Ceiling(totalCount / (double)pagesize);

            if (page < 1 || page > totalPages)
                return BadRequest($"Invalid page number.Valid range: 1 - {totalPages}.");

            var products = context.Products.OrderBy(p => p.PName)
                                           .Skip((page - 1) * pagesize)
                                           .Take(pagesize)
                                           .ToList();
            return Ok(products);
        }

        [HttpGet("GetProductById")]
        public IActionResult GetProductById (int id)
        {
            var product = context.Products.Find(id);
            if (product == null)
                return NotFound("Product not found.");

            return Ok(product);
        }



        public class AddProductRequest
        {
            public string PName { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public int Stock { get; set; }
        }

        public class UpdateProductRequest
        {
            public decimal Price { get; set; }
            public int Stock { get; set; }
        }


    }
}