namespace E_Commerce_system_api.Controllers
{
    public class PlaceOrderRequest
    {
        public object?[]? UserId { get; internal set; }
        public object Items { get; internal set; }
    }
}