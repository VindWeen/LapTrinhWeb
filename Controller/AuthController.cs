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
        // [HttpPost("google-login")]
        // public async Task<IActionResult> GoogleLogin(GoogleLoginRequestDTO dto)
        // {
        //     try
        //     {
        //         var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken,
        //             new GoogleJsonWebSignature.ValidationSettings
        //             {
        //                 Audience = new List<string> { _config["GoogleAuth:ClientId"] }
        //             });

        //         // payload.Email
        //         // payload.Subject (GoogleId)
        //         // payload.Name

        //         var user = await _db.Users
        //             .FirstOrDefaultAsync(u => u.GoogleId == payload.Subject);

        //         // Nếu chưa có tài khoản thì tạo mới
        //         if (user == null)
        //         {
        //             user = new Users
        //             {
        //                 Username = payload.Email,
        //                 Email = payload.Email,
        //                 GoogleId = payload.Subject,
        //                 Password = "", // không cần mật khẩu
        //                 DateOfBirth = DateTime.UtcNow,
        //                 Role = "User",
        //                 IsLocked = false
        //             };

        //             _db.Users.Add(user);
        //             await _db.SaveChangesAsync();
        //         }

        //         if (user.IsLocked)
        //             return Unauthorized("Tài khoản đã bị khóa.");

        //         var token = GenerateJwtToken(user);

        //         return Ok(new AuthResponseDTO
        //         {
        //             UserId = user.Id,
        //             Username = user.Username,
        //             Role = user.Role,
        //             Token = token
        //         });
        //     }
        //     catch
        //     {
        //         return Unauthorized("Google token không hợp lệ.");
        //     }
        // }


        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginRequestDTO dto)
        {
            Console.WriteLine($"[GoogleLogin] Received idToken (first 50 chars): {(dto?.IdToken?.Length > 50 ? dto.IdToken.Substring(0, 50) : dto.IdToken)}");

            var clientId = _config["GoogleAuth:ClientId"];
            Console.WriteLine($"[GoogleLogin] Using ClientId from config: {clientId}");

            if (string.IsNullOrEmpty(clientId))
            {
                Console.WriteLine("[GoogleLogin] ERROR: GoogleAuth:ClientId is empty or missing!");
                return Unauthorized("Server config error: Missing Google Client ID");
            }

            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new List<string> { clientId }
                    });

                Console.WriteLine($"[GoogleLogin] Success - GoogleId: {payload.Subject}, Email: {payload.Email}");

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
                        Role = "Customer",
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
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleLogin] Verify FAILED: {ex.Message}");
                Console.WriteLine($"[GoogleLogin] StackTrace: {ex.StackTrace}");
                return Unauthorized($"Google token không hợp lệ: {ex.Message}");
            }
        }
        //Refresh token
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            // Lấy token cũ từ header Authorization (Bearer token)
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized("Token không hợp lệ hoặc thiếu.");
            }

            var oldToken = authHeader.Substring("Bearer ".Length).Trim();

            try
            {
                // Validate token cũ (không cần check expire, chỉ cần signature và claims)
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = false,  // Không check expire để cho phép refresh token cũ
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _config["Jwt:Issuer"],
                    ValidAudience = _config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };

                // Validate token
                tokenHandler.ValidateToken(oldToken, validationParameters, out SecurityToken validatedToken);
                var jwtToken = (JwtSecurityToken)validatedToken;

                // Lấy userId từ claim
                var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "userId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("Token không chứa userId hợp lệ.");
                }

                // Tìm user trong DB để lấy thông tin mới nhất (role, locked...)
                var user = await _db.Users.FindAsync(userId);
                if (user == null)
                    return Unauthorized("User không tồn tại.");

                if (user.IsLocked)
                    return Unauthorized("Tài khoản đã bị khóa.");

                // Tạo token mới (giống GenerateJwtToken)
                var newToken = GenerateJwtToken(user);

                return Ok(new
                {
                    Token = newToken,
                    Message = "Refresh token thành công"
                });
            }
            catch (SecurityTokenException)
            {
                return Unauthorized("Token không hợp lệ hoặc đã bị thay đổi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Refresh token error: {ex.Message}");
                return StatusCode(500, "Lỗi server khi refresh token.");
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