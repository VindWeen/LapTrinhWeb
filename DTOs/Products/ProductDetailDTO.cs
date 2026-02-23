using LapTrinhWeb.DTOs;
namespace LapTrinhWeb.DTOs
{
    public class ProductDetailDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public List<ProductImageDTO> Images { get; set; }
        public List<ProductVariantDTO> Variants { get; set; }
    }
}