namespace LapTrinhWeb.Models
{
    public class OrderStatusHistory
    {
        public int Id { get; set; }
        public int? OrderId { get; set; }
        public int? OldStatus { get; set; }
        public int NewStatus { get; set; }
        public DateTime ChangedAt { get; set; }
        public int UpdateByType { get; set; } // 0: System, 1: Admin, 2: Staff, 3: Shipper
        public int? UpdatedByUserId { get; set; }
        public string Note { get; set; }
    }
}