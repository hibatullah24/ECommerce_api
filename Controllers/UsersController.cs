using E_Commerce_System;
using E_Commerce_System.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;



 namespace E_Commerce_system_api.Controllers
{
    [ApiController]
    [Route("api/Users")]
    public class UsersController : ControllerBase
    {
        ApplicationDbContext context = new ApplicationDbContext();


        [HttpPost("Register")]
        public IActionResult Register(Models.RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Name and email are required.");

            if (!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return BadRequest("Invalid email format.");

            var existingUser = context.Users.FirstOrDefault(u => u.Email == request.Email);

            if (existingUser != null)
                return Conflict("Email is already registered.");

            if (string.IsNullOrWhiteSpace((string?)request.Password) || request.Password.Length < 6)
                return BadRequest("Password must be at least 6 char.");

            var user = new User
            {
                UName = request.Name,
                Email = request.Email,
                Password = HashPassword(request.Password),
                Phone = request.Phone,
                Role = "User",
                CreatedAt = DateTime.Now

            };

            context.Users.Add(user);
            context.SaveChanges();
            return Ok("Registration successful");
        }


        [HttpPost("Login")]
        public IActionResult Login(Models.LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Email and Password are required.");

            var user = context.Users.FirstOrDefault(u => u.Email == request.Email);
            if (user == null)
                return NotFound("Email not found");

            bool isPasswordValid = user.Password == HashPassword(request.Password);

            if (!isPasswordValid)
                return Unauthorized("Invalid password.");

            return Ok(new { message = $"Welcome, {user.UName}", userId = user.UId, role = user.Role });
        }

        private string HashPassword(string password)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes)
                builder.Append(b.ToString("x2"));
            return builder.ToString();
        }

        [HttpGet("GetUserById")]
        public IActionResult GetUserById (int id)
        {
            var user = context.Users.Find(id);
            if (user == null)
            {
               
                return NotFound("User not found");
            }
            return Ok(user);

        }

        [HttpGet("GetAllUsers")]
        public IActionResult GetAllUsers()
        {
            var users = context.Users.Select( u => new UserDto
            {
                UserId = u.UId,
                Name = u.UName,
                Email = u.Email,
                Phone = u.Phone,

                Orders = context.Orders.Where( o => o.UId == u.UId)
                                       .Select( o => new OrderDto
                                       {
                                           OrderId = o.OId,
                                           OrderDate = (DateTime)o.OrderDate,
                                           TotalAmount = o.TotalAmount,
                                          
                                       })
                                       .ToList()

            })
                .ToList();

            if (users == null || users.Count == 0)
                return NotFound("No users found.");

            return Ok(users);

        }


        public class UserDto
        {
            public int UserId { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public List<OrderDto> Orders { get; set; }

        }

        public class OrderDto
            {
                public int OrderId { get; set; }
                public DateTime OrderDate { get; set; }
                public decimal TotalAmount { get; set; }
        }

    }
}
