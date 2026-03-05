using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LapTrinhWeb.Data;
using LapTrinhWeb.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace LapTrinhWeb.Controllers
{
    [Route("api/cart")]
    [ApiController]
    [Authorize] // Bắt buộc phải đăng nhập
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/cart - Lấy toàn bộ giỏ hàng của user hiện tại
        [HttpGet]
        public async Task<ActionResult> GetCart()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập lại" });

            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Color)   // Load MasterColors
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Size)    // Load MasterSizes
                .Select(c => new
                {
                    c.Id,
                    ProductId = c.ProductId,
                    VariantId = c.ProductVariantId,
                    Quantity = c.Quantity,
                    Product = c.Product != null ? new
                    {
                        c.Product.Name,
                        c.Product.Price,
                        c.Product.Thumbnail
                    } : null,
                    Variant = c.ProductVariant != null ? new
                    {
                        SKU = c.ProductVariant.SKU ?? "N/A",
                        ColorName = c.ProductVariant.Color != null ? c.ProductVariant.Color.Name : null,
                        ColorHex = c.ProductVariant.Color != null ? c.ProductVariant.Color.HexCode : null,
                        SizeName = c.ProductVariant.Size != null ? c.ProductVariant.Size.Name : null
                    } : null
                })
                .ToListAsync();

            return Ok(new { success = true, data = cartItems });
        }

        // POST: api/cart - Thêm sản phẩm vào giỏ
        [HttpPost]
        public async Task<ActionResult> AddToCart([FromBody] CartItemDto dto)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập" });

            if (dto.ProductId == null && dto.ProductVariantId == null)
                return BadRequest(new { success = false, message = "Phải cung cấp ProductId hoặc ProductVariantId" });

            // Kiểm tra sản phẩm/variant tồn tại và còn hàng
            if (dto.ProductVariantId.HasValue)
            {
                var variant = await _context.ProductVariants.FindAsync(dto.ProductVariantId);
                if (variant == null || variant.Quantity < dto.Quantity)
                    return BadRequest(new { success = false, message = "Sản phẩm biến thể không tồn tại hoặc hết hàng" });
            }
            else if (dto.ProductId.HasValue)
            {
                var product = await _context.Products.FindAsync(dto.ProductId);
                if (product == null)
                    return BadRequest(new { success = false, message = "Sản phẩm không tồn tại" });
                // Nếu không dùng variant, có thể kiểm tra tồn kho tổng nếu có logic riêng
            }

            // Kiểm tra đã có trong giỏ chưa
            var existing = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId &&
                                         c.ProductId == dto.ProductId &&
                                         c.ProductVariantId == dto.ProductVariantId);

            if (existing != null)
            {
                existing.Quantity += dto.Quantity;
            }
            else
            {
                var newItem = new CartItems
                {
                    UserId = userId,
                    ProductId = dto.ProductId,
                    ProductVariantId = dto.ProductVariantId,
                    Quantity = dto.Quantity
                };
                _context.CartItems.Add(newItem);
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã thêm vào giỏ hàng" });
        }

        // PUT: api/cart/{id} - Cập nhật số lượng
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateCartItem(int id, [FromBody] UpdateCartQuantityDto dto)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            if (item == null)
                return NotFound(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ" });

            if (dto.Quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }
            else
            {
                // Kiểm tra tồn kho nếu cần
                item.Quantity = dto.Quantity;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Đã cập nhật giỏ hàng" });
        }

        // DELETE: api/cart/{id} - Xóa 1 item
        [HttpDelete("{id}")]
        public async Task<ActionResult> RemoveCartItem(int id)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            if (item == null)
                return NotFound();

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã xóa sản phẩm khỏi giỏ" });
        }

        // DELETE: api/cart/clear - Xóa toàn bộ giỏ
        [HttpDelete("clear")]
        public async Task<ActionResult> ClearCart()
        {
            var userId = int.Parse(User.FindFirst("userId")!.Value);
            var items = await _context.CartItems.Where(c => c.UserId == userId).ToListAsync();
            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Đã xóa toàn bộ giỏ hàng" });
        }
    }

    // DTOs
    public class CartItemDto
    {
        public int? ProductId { get; set; }
        public int? ProductVariantId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartQuantityDto
    {
        public int Quantity { get; set; }
    }
}