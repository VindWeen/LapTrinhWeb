public class ProductDetailDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public List<ProductImageDto> Images { get; set; }
    public List<ProductVariantDto> Variants { get; set; }
}