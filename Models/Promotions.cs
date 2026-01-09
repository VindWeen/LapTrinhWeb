namespace LapTrinhWeb.Models
{
    public class Promotions
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool Priority { get; set; } = false;
    }
}