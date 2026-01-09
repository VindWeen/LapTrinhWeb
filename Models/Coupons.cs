namespace LapTrinhWeb.Models
{
    public class Coupons
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public int? UserId { get; set; }
        public int? PromotionId { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime ExpiryDate { get; set; }
    }
}