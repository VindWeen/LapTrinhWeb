namespace LapTrinhWeb.Models
{
    public class PromotionCoditions
    {
        public int Id { get; set; }
        public int? PromotionId { get; set; }
        public string Field { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
    }
}