namespace LapTrinhWeb.Models
{
    public class CartItems
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? ProductId { get; set; }
        public int? ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public virtual Products Product { get; set; }
        public virtual ProductVariants ProductVariant { get; set; }
    }
}