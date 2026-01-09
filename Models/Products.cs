namespace LapTrinhWeb.Models
{
    public class Products
    {
        public int Id { get; set; }
        public string Slug { get; set; }
        public int? CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Thumbnail { get; set; }
        public bool IsActive { get; set; }
    }
}