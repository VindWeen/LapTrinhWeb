using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LapTrinhWeb.Data;
using LapTrinhWeb.DTOs;
using LapTrinhWeb.Models;
using System.Security.Claims;

namespace LapTrinhWeb.Controllers
{
    [ApiController]
    [Route("api/admin/orders")]
    [Authorize(Roles = "Admin")]
    public class AdminOrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminOrdersController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // GET: /api/admin/orders
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(orders);
        }

        // ============================================================
        // GET: /api/admin/orders/{id}
        // ============================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderDetail(int id)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound(new { message = "Không tìm thấy đơn hàng" });

            var orderDetails = await _context.OrderDetails
                .Where(x => x.OrderId == id)
                .ToListAsync();

            var statusHistory = await _context.OrderStatusHistory
                .Where(x => x.OrderId == id)
                .OrderByDescending(x => x.ChangedAt)
                .ToListAsync();

            return Ok(new
            {
                Order = order,
                Details = orderDetails,
                StatusHistory = statusHistory
            });
        }

        // ============================================================
        // PUT: /api/admin/orders/{id}/status
        // ============================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(
            int id,
            [FromBody] OrderUpdateDTO model)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound(new { message = "Không tìm thấy đơn hàng" });

            // Cập nhật field cho phép sửa
            order.OrderCode = model.OrderCode;
            order.ShippingAddress = model.ShippingAddress;
            order.Status = model.Status;

            // Lấy adminId từ JWT
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var history = new OrderStatusHistory
            {
                OrderId = id,
                OldStatus = order.Status,
                NewStatus = model.Status,
                ChangedAt = DateTime.Now,
                UpdateByType = 1,
                UpdatedByUserId = adminId != null ? int.Parse(adminId) : null,
                Note = model.Note
            };

            _context.OrderStatusHistory.Add(history);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật đơn hàng thành công" });
        }
    }
}