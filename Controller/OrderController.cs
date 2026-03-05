using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LapTrinhWeb.Data;
using LapTrinhWeb.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LapTrinhWeb.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/orders - Danh sách đơn hàng (phân trang + lọc)
        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(new { success = false, message = "Không tìm thấy thông tin user." });

                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                var query = _context.Orders.AsQueryable();

                if (role != "Admin")
                {
                    query = query.Where(o => o.UserId == userId);
                }

                if (status.HasValue)
                {
                    query = query.Where(o => o.Status == status.Value);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(o => o.OrderDate >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(o => o.OrderDate <= toDate.Value);
                }

                var totalRecords = await query.CountAsync();

                var orders = await query
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Load tất cả OrderDetails liên quan một lần (tránh N+1 query)
                var orderIds = orders.Select(o => o.Id).ToList();
                var allDetails = await _context.OrderDetails
                    .Where(od => orderIds.Contains(od.OrderId.Value))
                    .ToListAsync();

                // Nhóm chi tiết theo OrderId
                var detailsByOrder = allDetails
                    .GroupBy(od => od.OrderId.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Tạo DTO trả về
                var result = orders.Select(o => new
                {
                    o.Id,
                    o.OrderCode,
                    o.OrderDate,
                    o.UserId,
                    o.ShippingName,
                    o.ShippingAddress,
                    o.ShippingPhone,
                    o.TotalAmount,
                    o.DiscountAmount,
                    o.ShippingFee,
                    o.FinalAmount,
                    o.CouponCode,
                    o.PaymentMethod,
                    o.PaymentStatus,
                    o.Status,
                    OrderDetails = detailsByOrder.TryGetValue(o.Id, out var details) ? details : new List<OrderDetails>()
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = result,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalRecords,
                        totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi lấy danh sách đơn hàng", error = ex.Message });
            }
        }

        // GET: api/orders/{id} - Chi tiết đơn hàng
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult> GetOrder(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(new { success = false, message = "Không tìm thấy thông tin user." });

                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                    return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });

                if (role != "Admin" && order.UserId != userId)
                    return Unauthorized(new { success = false, message = "Bạn không có quyền xem đơn hàng này" });

                // Load OrderDetails riêng
                var details = await _context.OrderDetails
                    .Where(od => od.OrderId == id)
                    .ToListAsync();

                var result = new
                {
                    order.Id,
                    order.OrderCode,
                    order.OrderDate,
                    order.UserId,
                    order.ShippingName,
                    order.ShippingAddress,
                    order.ShippingPhone,
                    order.TotalAmount,
                    order.DiscountAmount,
                    order.ShippingFee,
                    order.FinalAmount,
                    order.CouponCode,
                    order.PaymentMethod,
                    order.PaymentStatus,
                    order.Status,
                    OrderDetails = details
                };

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi lấy chi tiết đơn hàng", error = ex.Message });
            }
        }

        // POST: api/orders - Tạo đơn hàng mới (Customer)
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult> CreateOrder([FromBody] OrderCreateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(new { success = false, message = "Vui lòng đăng nhập lại." });

                if (dto == null || dto.OrderDetails == null || !dto.OrderDetails.Any())
                    return BadRequest(new { success = false, message = "Đơn hàng phải có ít nhất một sản phẩm" });

                decimal totalAmount = dto.OrderDetails.Sum(d => d.Quantity * d.UnitPrice);
                decimal shippingFee = 30000m; // Có thể lấy động sau
                decimal discountAmount = 0m;  // Chưa có coupon
                decimal finalAmount = totalAmount + shippingFee - discountAmount;

                var order = new Orders
                {
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    OrderCode = GenerateOrderCode(),
                    ShippingName = dto.ShippingName,
                    ShippingAddress = dto.ShippingAddress,
                    ShippingPhone = dto.ShippingPhone,
                    TotalAmount = totalAmount,
                    DiscountAmount = discountAmount,
                    ShippingFee = shippingFee,
                    FinalAmount = finalAmount,
                    CouponCode = null,
                    PaymentMethod = dto.PaymentMethod ?? "COD",
                    PaymentStatus = "Pending",
                    Status = 0 // Đang xử lý
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                var details = dto.OrderDetails.Select(d => new OrderDetails
                {
                    OrderId = order.Id,
                    SnapshotProductName = d.SnapshotProductName,
                    SnapshotSKU = d.SnapshotSKU,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Total = d.Quantity * d.UnitPrice
                }).ToList();

                _context.OrderDetails.AddRange(details);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, new
                {
                    success = true,
                    message = "Đặt hàng thành công",
                    data = new { order.Id, order.OrderCode }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi tạo đơn hàng", error = ex.Message });
            }
        }

        // PUT: api/orders/{id} - Cập nhật (chỉ Admin)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateOrder(int id, [FromBody] OrderUpdateDto dto)
        {
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                    return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });

                if (dto.Status.HasValue) order.Status = dto.Status.Value;
                if (!string.IsNullOrEmpty(dto.ShippingName)) order.ShippingName = dto.ShippingName;
                if (!string.IsNullOrEmpty(dto.ShippingAddress)) order.ShippingAddress = dto.ShippingAddress;
                if (!string.IsNullOrEmpty(dto.ShippingPhone)) order.ShippingPhone = dto.ShippingPhone;
                if (!string.IsNullOrEmpty(dto.PaymentStatus)) order.PaymentStatus = dto.PaymentStatus;
                if (!string.IsNullOrEmpty(dto.PaymentMethod)) order.PaymentMethod = dto.PaymentMethod;
                if (!string.IsNullOrEmpty(dto.CouponCode)) order.CouponCode = dto.CouponCode;

                if (dto.OrderDetails != null && dto.OrderDetails.Any())
                {
                    var oldDetails = await _context.OrderDetails.Where(od => od.OrderId == id).ToListAsync();
                    _context.OrderDetails.RemoveRange(oldDetails);

                    var newDetails = dto.OrderDetails.Select(d => new OrderDetails
                    {
                        OrderId = id,
                        SnapshotProductName = d.SnapshotProductName,
                        SnapshotSKU = d.SnapshotSKU,
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice,
                        Total = d.Quantity * d.UnitPrice
                    }).ToList();

                    _context.OrderDetails.AddRange(newDetails);

                    decimal totalAmount = newDetails.Sum(d => d.Total);
                    order.TotalAmount = totalAmount;
                    order.FinalAmount = totalAmount + order.ShippingFee - order.DiscountAmount;
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Cập nhật đơn hàng thành công", data = order });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi cập nhật đơn hàng", error = ex.Message });
            }
        }

        // DELETE: api/orders/{id} - Xóa (chỉ Admin)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteOrder(int id)
        {
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                    return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });

                var details = await _context.OrderDetails.Where(od => od.OrderId == id).ToListAsync();
                _context.OrderDetails.RemoveRange(details);
                _context.Orders.Remove(order);

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Xóa đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi xóa đơn hàng", error = ex.Message });
            }
        }

        // Sinh mã đơn hàng
        private string GenerateOrderCode()
        {
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var lastOrder = _context.Orders
                .Where(o => o.OrderCode.StartsWith($"ORD-{datePart}"))
                .OrderByDescending(o => o.OrderCode)
                .FirstOrDefault();

            int sequence = 1;
            if (lastOrder != null)
            {
                var lastSeqStr = lastOrder.OrderCode.Substring(lastOrder.OrderCode.Length - 5);
                if (int.TryParse(lastSeqStr, out int seq)) sequence = seq + 1;
            }

            return $"ORD-{datePart}-{sequence:00000}";
        }
    }

    // DTOs (giữ nguyên)
    public class OrderCreateDto
    {
        public string ShippingName { get; set; }
        public string ShippingAddress { get; set; }
        public string ShippingPhone { get; set; }
        public string PaymentMethod { get; set; } = "COD";
        public List<OrderDetailCreateDto> OrderDetails { get; set; }
    }

    public class OrderDetailCreateDto
    {
        public string SnapshotProductName { get; set; }
        public string SnapshotSKU { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class OrderUpdateDto
    {
        public int? Status { get; set; }
        public string ShippingName { get; set; }
        public string ShippingAddress { get; set; }
        public string ShippingPhone { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public string CouponCode { get; set; }
        public List<OrderDetailCreateDto> OrderDetails { get; set; }
    }
}