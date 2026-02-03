namespace LapTrinhWeb.DTOs
{
    public class ProductListItemDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal MinPrice { get; set; }
        public string ThumbnailUrl { get; set; }
    }
}