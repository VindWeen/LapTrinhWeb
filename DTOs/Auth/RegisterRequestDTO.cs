using System.ComponentModel.DataAnnotations;

namespace LapTrinhWeb.DTOs
{
    public class RegisterRequestDTO
    {
        [Required(ErrorMessage = "Username là bắt buộc")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải ít nhất 6 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        public DateTime DateOfBirth { get; set; }

        // Role tùy chọn, mặc định sẽ là "Customer" ở backend
        public string? Role { get; set; }

        [Required(ErrorMessage = "Họ và tên (ContactName) là bắt buộc")]
        public string ContactName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        public string ContactPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ (AddressLine) là bắt buộc")]
        public string AddressLine { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tỉnh/Thành phố là bắt buộc")]
        public string Province { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quận/Huyện là bắt buộc")]
        public string District { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phường/Xã là bắt buộc")]
        public string Ward { get; set; } = string.Empty;
    }
}