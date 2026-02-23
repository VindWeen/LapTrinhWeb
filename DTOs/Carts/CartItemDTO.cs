namespace LapTrinhWeb.DTOs
{
    public class CartItemDTO
    {
        public int ProductVariantId { get; set; }
        public string ProductName { get; set; }
        public string Variant { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
