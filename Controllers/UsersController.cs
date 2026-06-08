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

        [Authorize(Roles = "Admin")]
        [HttpGet("GetUserById")]
        public IActionResult GetUserById(int adminId, int id)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError("GetUserById failed with", ex.Message);
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllUsers")]
        public IActionResult GetAllUsers(int adminId)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError("GetAllUsers failed with an unexpected error: {Message}", ex.Message);
                return BadRequest("An unexpected error occurred while retrieving users.");
            }

       
    }
}