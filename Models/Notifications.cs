namespace LapTrinhWeb.Models
{
    public class Notifications
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Type { get; set; } //Đơn hàng, Khuyến mãi, Tin tức
        public bool IsRead { get; set; } = false;
        public int? UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}