using ECommerce_api;
using ECommerce_api.DTOs;
using ECommerce_api.Models;
using ECommerce_api.Services;
using MailKit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ECommerce_api_api.Controllers
{
    [ApiController]
    [Route("api/Users")]
    public class UsersController : ControllerBase
    {
        public ApplicationDbContext _context;
        public LoggingService<UsersController> _logger;
        public JwtService _jwtService;
        public EmailService _emailService;

        public UsersController(ApplicationDbContext context, ILogger<UsersController> logger, JwtService jwtService,  EmailService emailService)
        {
            _context = context;
            _logger = new LoggingService<UsersController>(logger);
            _jwtService = jwtService;
            _emailService = emailService;
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        public IActionResult Register(Models.RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Name and email are required.");

            if (!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return BadRequest("Invalid email format.");

            var existingUser = _context.Users.FirstOrDefault(u => u.Email == request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Register failed: Email {Email} already registered.", request.Email);
                return Conflict("Email is already registered.");
            }

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
                return BadRequest("Password must be at least 6 characters.");

            var user = new User
            {
                UName = request.Name,
                Email = request.Email,
                Password = HashPassword(request.Password),
                Phone = request.Phone,
                Role = "User",
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            _context.SaveChanges();


            _emailService.SendEmail(
                    user.Email,
                    "Welcome to E-Commerce API",
                    $"<h2>Welcome, {user.UName}!</h2><p>Your account has been created successfully.</p>"
             );

            _logger.LogInfo("User {UserId} registered successfully.", user.UId);
            return Ok(new { message = "Registration successful.", userId = user.UId, role = user.Role });
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public IActionResult Login(Models.LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Email and Password are required.");

            var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: Email {Email} not found.", request.Email);
                return NotFound("Email not found.");
            }

            bool isPasswordValid = user.Password == HashPassword(request.Password);
            if (!isPasswordValid)
            {
                _logger.LogWarning("Login failed: Invalid password for Email {Email}.", request.Email);
                return Unauthorized("Invalid password.");
            }

            var token = _jwtService.GenerateToken(user);

            _emailService.SendEmail(
                    user.Email,
                    "New Login Detected",
                    $"<h2>Hello, {user.UName}!</h2><p>A new login was detected on your account at {DateTime.Now}.</p>"
                );

            _logger.LogInfo("User {UserId} logged in successfully.", user.UId);

            return Ok (new
            {
                message = $"Welcome, {user.UName}!",
                userId = user.UId,
                role = user.Role,
                token = token
            });
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("GetUserById")]
        public IActionResult GetUserById(int adminId, int id)
        {
            var requestingUser = _context.Users.Find(adminId);
            if (requestingUser == null || requestingUser.Role != "Admin")
            {
                _logger.LogWarning("GetUserById failed: User {AdminId} is not an admin.", adminId);
                return StatusCode(403, "Access denied. Admins only.");
            }

            var user = _context.Users.Find(id);
            if (user == null)
            {
                _logger.LogWarning("GetUserById: User {UserId} not found.", id);
                return NotFound("User not found.");
            }

            _logger.LogInfo("Admin {AdminId} retrieved User {UserId}.", adminId, id);
            return Ok(user);
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllUsers")]
        public IActionResult GetAllUsers(int adminId)
        {
            var requestingUser = _context.Users.Find(adminId);
            if (requestingUser == null || requestingUser.Role != "Admin")
            {
                _logger.LogWarning("GetAllUsers failed: User {AdminId} is not an admin.", adminId);
                return StatusCode(403, "Access denied. Admins only.");
            }

            var users = _context.Users.Select(u => new UserDto
            {
                UserId = u.UId,
                Name = u.UName,
                Email = u.Email,
                Phone = u.Phone,
                Orders = _context.Orders.Where(o => o.UId == u.UId)
                                        .Select(o => new OrderDto
                                        {
                                            OrderId = o.OId,
                                            OrderDate = (DateTime)o.OrderDate,
                                            TotalAmount = o.TotalAmount,
                                        })
                                        .ToList()
            }).ToList();

            if (!users.Any())
            {
                _logger.LogWarning("GetAllUsers: No users found.");
                return NotFound("No users found.");
            }

            _logger.LogInfo("Admin {AdminId} retrieved all users. Count: {Count}.", adminId, users.Count);
            return Ok(users);
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
    }
}