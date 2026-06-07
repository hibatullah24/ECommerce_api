namespace ECommerce_api.DTOs
{
    public class AddReviewRequest
    {
        public int UId { get; set; }
        public int PId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
    }
}
