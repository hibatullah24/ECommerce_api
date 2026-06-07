namespace ECommerce_api.DTOs
{
    public class PlaceOrderRequest
    {
        public int UserId { get; set; }
        public List<OrderItemRequest> Items { get; set; }
    }
}