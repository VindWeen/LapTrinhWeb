using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LapTrinhWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using LapTrinhWeb.Data;

namespace LapTrinhWeb.Controllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class PromotionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PromotionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/promotions - Danh sách khuyến mãi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PromotionResponse>>> GetPromotions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string search = "",
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var query = _context.Promotions.AsQueryable();

                // Tìm kiếm theo tên
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => p.Name.Contains(search));
                }

                // Lọc theo trạng thái
                if (isActive.HasValue)
                {
                    query = query.Where(p => p.IsActive == isActive.Value);
                }

                // Sắp xếp theo Priority và StartDate
                query = query.OrderByDescending(p => p.Priority)
                            .ThenByDescending(p => p.StartDate);

                var totalRecords = await query.CountAsync();

                var promotions = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PromotionResponse
                    {
                        Id = p.Id,
                        Name = p.Name,
                        DiscountType = p.DiscountType,
                        DiscountValue = p.DiscountValue,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        IsActive = p.IsActive,
                        Priority = p.Priority,
                        ConditionsCount = _context.PromotionConditions
                            .Count(pc => pc.PromotionId == p.Id),
                        CouponsCount = _context.Coupons
                            .Count(c => c.PromotionId == p.Id)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = promotions,
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
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi lấy danh sách khuyến mãi",
                    error = ex.Message
                });
            }
        }

        // POST: api/admin/promotions - Tạo chương trình khuyến mãi
        [HttpPost]
        public async Task<ActionResult<Promotions>> CreatePromotion([FromBody] CreatePromotionRequest request)
        {
            try
            {
                // Validate dữ liệu
                if (string.IsNullOrEmpty(request.Name))
                {
                    return BadRequest(new { success = false, message = "Tên chương trình không được để trống" });
                }

                if (request.StartDate >= request.EndDate)
                {
                    return BadRequest(new { success = false, message = "Ngày bắt đầu phải nhỏ hơn ngày kết thúc" });
                }

                if (request.DiscountValue <= 0)
                {
                    return BadRequest(new { success = false, message = "Giá trị giảm giá phải lớn hơn 0" });
                }

                // Validate DiscountType
                if (request.DiscountType != "percentage" && request.DiscountType != "fixed")
                {
                    return BadRequest(new { success = false, message = "Loại giảm giá phải là 'percentage' hoặc 'fixed'" });
                }

                // Nếu là percentage thì giá trị phải <= 100
                if (request.DiscountType == "percentage" && request.DiscountValue > 100)
                {
                    return BadRequest(new { success = false, message = "Giảm giá theo phần trăm không được vượt quá 100%" });
                }

                // Tạo promotion mới
                var promotion = new Promotions
                {
                    Name = request.Name,
                    DiscountType = request.DiscountType,
                    DiscountValue = request.DiscountValue,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    IsActive = request.IsActive ?? true,
                    Priority = request.Priority ?? 0 
                };

                _context.Promotions.Add(promotion);
                await _context.SaveChangesAsync();

                // Thêm các điều kiện nếu có
                if (request.Conditions != null && request.Conditions.Any())
                {
                    foreach (var condition in request.Conditions)
                    {
                        var promotionCondition = new PromotionConditions
                        {
                            PromotionId = promotion.Id,
                            Field = condition.Field,
                            Operator = condition.Operator,
                            Value = condition.Value
                        };
                        _context.PromotionConditions.Add(promotionCondition);
                    }
                    await _context.SaveChangesAsync();
                }

                return CreatedAtAction(nameof(GetPromotion), new { id = promotion.Id }, new
                {
                    success = true,
                    message = "Tạo chương trình khuyến mãi thành công",
                    data = promotion
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi tạo chương trình khuyến mãi",
                    error = ex.Message
                });
            }
        }

        // GET: api/admin/promotions/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Promotions>> GetPromotion(int id)
        {
            try
            {
                var promotion = await _context.Promotions.FindAsync(id);

                if (promotion == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy chương trình khuyến mãi" });
                }

                var conditions = await _context.PromotionConditions
                    .Where(pc => pc.PromotionId == id)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        promotion,
                        conditions
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi lấy thông tin khuyến mãi",
                    error = ex.Message
                });
            }
        }

        // POST: api/admin/coupons/generate - Tạo voucher
        [HttpPost("coupons/generate")]
        public async Task<ActionResult> GenerateCoupons([FromBody] GenerateCouponsRequest request)
        {
            try
            {
                // Validate
                if (!request.PromotionId.HasValue || request.PromotionId <= 0)
                {
                    return BadRequest(new { success = false, message = "PromotionId không hợp lệ" });
                }

                if (request.Quantity <= 0)
                {
                    return BadRequest(new { success = false, message = "Số lượng voucher phải lớn hơn 0" });
                }

                if (request.Quantity > 10000)
                {
                    return BadRequest(new { success = false, message = "Số lượng voucher không được vượt quá 10,000" });
                }

                // Kiểm tra promotion có tồn tại không
                var promotion = await _context.Promotions.FindAsync(request.PromotionId.Value);
                if (promotion == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy chương trình khuyến mãi" });
                }

                // Tính ngày hết hạn
                DateTime expiryDate = request.ExpiryDate ?? promotion.EndDate;

                // Tạo danh sách coupon
                var coupons = new List<Coupons>();
                var generatedCodes = new HashSet<string>();

                for (int i = 0; i < request.Quantity; i++)
                {
                    string code;
                    int attempts = 0;
                    
                    // Tạo mã code duy nhất
                    do
                    {
                        code = GenerateCouponCode(request.Prefix);
                        attempts++;
                        
                        if (attempts > 100)
                        {
                            return StatusCode(500, new
                            {
                                success = false,
                                message = "Không thể tạo mã voucher duy nhất, vui lòng thử lại"
                            });
                        }
                    }
                    while (generatedCodes.Contains(code) || 
                           await _context.Coupons.AnyAsync(c => c.Code == code));

                    generatedCodes.Add(code);

                    var coupon = new Coupons
                    {
                        Code = code,
                        PromotionId = request.PromotionId,
                        UserId = request.UserId,
                        IsUsed = false,
                        ExpiryDate = expiryDate
                    };

                    coupons.Add(coupon);
                }

                // Lưu tất cả coupon vào database
                _context.Coupons.AddRange(coupons);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Đã tạo thành công {request.Quantity} voucher",
                    data = new
                    {
                        promotionId = request.PromotionId,
                        quantity = request.Quantity,
                        expiryDate = expiryDate,
                        coupons = coupons.Select(c => new
                        {
                            c.Id,
                            c.Code,
                            c.ExpiryDate
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi tạo voucher",
                    error = ex.Message
                });
            }
        }

        // Hàm tạo mã coupon ngẫu nhiên
        private string GenerateCouponCode(string prefix = "")
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var code = new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            return string.IsNullOrEmpty(prefix) ? code : $"{prefix}{code}";
        }
    }

    // DTOs - Data Transfer Objects
    public class CreatePromotionRequest
    {
        public string Name { get; set; }
        public string DiscountType { get; set; } // "percentage" hoặc "fixed"
        public decimal DiscountValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool? IsActive { get; set; }
        public int? Priority { get; set; }
        public List<ConditionDto> Conditions { get; set; }
    }

    public class ConditionDto
    {
        public string Field { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
    }

    public class GenerateCouponsRequest
    {
        public int? PromotionId { get; set; }
        public int Quantity { get; set; }
        public int? UserId { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Prefix { get; set; }
    }

    public class PromotionResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        public int ConditionsCount { get; set; }
        public int CouponsCount { get; set; }
    }
}