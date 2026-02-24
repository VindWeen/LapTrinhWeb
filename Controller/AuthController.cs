using Microsoft.AspNetCore.Mvc;
using LapTrinhWeb.Data;
using LapTrinhWeb.DTOs;
using LapTrinhWeb.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Google.Apis.Auth;

namespace LapTrinhWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // =============================
        // HASH PASSWORD SHA256
        // =============================
        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        // =============================
        // REGISTER
        // =============================
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDTO dto)
        {
            // Tự động validate DTO (nếu dùng [Required] ở DTO)
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Kiểm tra trùng thủ công (vẫn giữ để chắc chắn)
            if (await _db.Users.AnyAsync(u => u.Username == dto.Username))
                return BadRequest("Username đã tồn tại.");

            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email đã tồn tại.");

            // Transaction để đảm bảo cả User và Address lưu thành công hoặc rollback
            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var user = new Users
                {
                    Username = dto.Username,
                    Email = dto.Email,
                    Password = HashPassword(dto.Password),
                    DateOfBirth = dto.DateOfBirth,
                    Role = dto.Role ?? "Customer",  // Mặc định Customer nếu không gửi
                    GoogleId = null,
                    IsLocked = false
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();  // Lưu để lấy User.Id

                // Tạo địa chỉ mặc định
                var address = new UserAddresses
                {
                    UserId = user.Id,
                    ContactName = dto.ContactName,
                    ContactPhone = dto.ContactPhone,
                    AddressLine = dto.AddressLine,
                    Province = dto.Province,
                    District = dto.District,
                    Ward = dto.Ward,
                    IsDefault = true
                };

                _db.UserAddresses.Add(address);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    Message = "Đăng ký thành công",
                    UserId = user.Id
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // =============================
        // LOGIN
        // =============================
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDTO dto)
        {
            var hashedPassword = HashPassword(dto.Password);

            var user = await _db.Users
                .FirstOrDefaultAsync(u =>
                    u.Username == dto.Username &&
                    u.Password == hashedPassword);

            if (user == null)
                return Unauthorized("Sai username hoặc mật khẩu.");

            if (user.IsLocked)
                return Unauthorized("Tài khoản đã bị khóa.");

            var token = GenerateJwtToken(user);

            return Ok(new AuthResponseDTO
            {
                UserId = user.Id,
                Username = user.Username,
                Role = user.Role,
                Token = token
            });
        }
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginRequestDTO dto)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new List<string>
                        {
                    _config["GoogleAuth:ClientId"]
                        }
                    });

                // payload.Email
                // payload.Subject (GoogleId)
                // payload.Name

                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.GoogleId == payload.Subject);

                // Nếu chưa có tài khoản thì tạo mới
                if (user == null)
                {
                    user = new Users
                    {
                        Username = payload.Email,
                        Email = payload.Email,
                        GoogleId = payload.Subject,
                        Password = "", // không cần mật khẩu
                        DateOfBirth = DateTime.UtcNow,
                        Role = "User",
                        IsLocked = false
                    };

                    _db.Users.Add(user);
                    await _db.SaveChangesAsync();
                }

                if (user.IsLocked)
                    return Unauthorized("Tài khoản đã bị khóa.");

                var token = GenerateJwtToken(user);

                return Ok(new AuthResponseDTO
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Role = user.Role,
                    Token = token
                });
            }
            catch
            {
                return Unauthorized("Google token không hợp lệ.");
            }
        }
        // =============================
        // GENERATE JWT
        // =============================
        private string GenerateJwtToken(Users user)
        {
            var claims = new[]
            {
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}