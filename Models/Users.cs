namespace LapTrinhWeb.Models
{
    public class Users
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string GoogleId { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Role { get; set; }
        public Boolean IsLocked { get; set; } = false;
    }
}