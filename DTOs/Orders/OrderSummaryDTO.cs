namespace LapTrinhWeb.DTOs
{
    public class OrderSummaryDTO
    {
        public int OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
    }
}
