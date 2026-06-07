using static ECommerce_api_api.Controllers.UsersController;

namespace ECommerce_api.DTOs
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public List<OrderDto> Orders { get; set; }

    }
}
