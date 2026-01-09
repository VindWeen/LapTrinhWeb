namespace LapTrinhWeb.Models
{
    public class Orders
    {
        public int Id { get; set; }
        public string OrderCode { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public int? UserId { get; set; }
        public string ShippingName { get; set; }
        public string ShippingAddress { get; set; }
        public string ShippingPhone { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal FinalAmount { get; set; }
        public string CouponCode { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public int Status { get; set; } //0: Đang xử lý, 1: Đã xử lý, 2: Đang giao hàng, 3: Giao thành công/ thất bại
    }
}