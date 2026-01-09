namespace LapTrinhWeb.Models
{
    public class UserAddresses
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string ContactName { get; set; }
        public string ContactPhone { get; set; }
        public string AddressLine { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        public bool IsDefault { get; set; } = false;
    }
}