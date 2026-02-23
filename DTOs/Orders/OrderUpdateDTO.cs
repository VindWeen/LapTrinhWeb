namespace LapTrinhWeb.DTOs
{
    public class OrderUpdateDTO
    {
        public string? OrderCode { get; set; }
        public string? ShippingAddress { get; set; }
        public int Status { get; set; }
        public string? Note { get; set; }
    }
}