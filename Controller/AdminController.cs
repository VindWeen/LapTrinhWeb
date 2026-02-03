using Microsoft.AspNetCore.Mvc;
using LapTrinhWeb.DTOs;
using LapTrinhWeb.Models;
using LapTrinhWeb.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
namespace LapTrinhWeb.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }
        
        // GET /api/admin/users - Danh sách
        [HttpGet("admin/users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            return Ok(users);
        }

        // GET /api/admin/users/{id} - Chi tiết
        [HttpGet("admin/users/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Người dùng không tồn tại" });
            }
            return Ok(user);
        }

        // PUT /api/admin/users/{id}/lock - Khóa tài khoản
        [HttpPut("admin/users/{id}/lock")]
        public async Task<IActionResult> LockUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Người dùng không tồn tại" });
            }

            user.IsLocked = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tài khoản đã bị khóa" });
        }

        // PUT /api/admin/users/{id}/role - Đổi quyền
        [HttpPut("admin/users/{id}/role")]
        public async Task<IActionResult> ChangeUserRole(int id, string newRole)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Người dùng không tồn tại" });
            }

            user.Role = newRole;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Quyền người dùng đã được cập nhật" });
        }
    }
}