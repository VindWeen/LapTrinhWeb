namespace LapTrinhWeb.Models
{
    public class OrderDetails
    {
        public int Id { get; set; }
        public int? OrderId { get; set; }
        public string SnapshotProductName { get; set; }
        public string SnapshotSKU { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; } = Quantity * UnitPrice;
    }
}