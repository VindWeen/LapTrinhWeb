using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LapTrinhWeb.Data;
using LapTrinhWeb.Models;
using Microsoft.AspNetCore.Authorization;

namespace LapTrinhWeb.Controllers.Admin
{
    [Route("api/admin/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/products - Danh sách
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string search = "",
    [FromQuery] int? categoryId = null,
    [FromQuery] bool? isActive = null)
        {
            try
            {
                var query = _context.Products.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => p.Name.Contains(search) || p.Slug.Contains(search));
                }

                if (categoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(p => p.IsActive == isActive.Value);
                }

                var totalRecords = await query.CountAsync();

                // Lấy danh sách sản phẩm cơ bản
                var products = await query
                    .OrderByDescending(p => p.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Slug = p.Slug,
                        CategoryId = p.CategoryId,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        Thumbnail = p.Thumbnail,
                        IsActive = p.IsActive,
                        ImageCount = _context.ProductImage.Count(pi => pi.ProductId == p.Id),
                        VariantCount = _context.ProductVariants.Count(pv => pv.ProductId == p.Id),
                        ReviewCount = _context.ProductReviews.Count(pr => pr.ProductId == p.Id),
                        AverageRating = _context.ProductReviews
                            .Where(pr => pr.ProductId == p.Id)
                            .Average(pr => (double?)pr.Rating) ?? 0,
                        Colors = new List<ColorInfo>(),
                        SizeRange = ""
                    })
                    .ToListAsync();

                if (!products.Any())
                    return Ok(new
                    {
                        success = true,
                        data = products,
                        pagination = new
                        {
                            page,
                            pageSize,
                            totalRecords,
                            totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize)
                        }
                    });

                // Lấy tất cả ProductId cần load
                var productIds = products.Select(p => p.Id).ToList();

                // Load Colors (distinct theo ColorId)
                var colorData = await _context.ProductVariants
                    .Where(v => productIds.Contains(v.ProductId.Value) && v.ColorId.HasValue)
                    .GroupBy(v => new { v.ProductId, v.ColorId })
                    .Select(g => new
                    {
                        ProductId = g.Key.ProductId.Value,
                        Color = _context.MasterColors
                            .Where(c => c.Id == g.Key.ColorId.Value)
                            .Select(c => new ColorInfo
                            {
                                HexCode = c.HexCode ?? "#ccc",
                                Name = c.Name ?? ""
                            })
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                // Load SizeNames để tính range
                var sizeData = await _context.ProductVariants
                    .Where(v => productIds.Contains(v.ProductId.Value) && v.SizeId.HasValue)
                    .GroupBy(v => new { v.ProductId, v.SizeId })
                    .Select(g => new
                    {
                        ProductId = g.Key.ProductId.Value,
                        SizeName = _context.MasterSizes
                            .Where(s => s.Id == g.Key.SizeId.Value)
                            .Select(s => s.Name)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                // Gán vào products
                foreach (var prod in products)
                {
                    // Colors
                    prod.Colors = colorData
                        .Where(c => c.ProductId == prod.Id && c.Color != null)
                        .Select(c => c.Color)
                        .DistinctBy(c => c.HexCode)
                        .ToList();

                    // SizeRange
                    var sizes = sizeData
                        .Where(s => s.ProductId == prod.Id && !string.IsNullOrEmpty(s.SizeName))
                        .Select(s => s.SizeName)
                        .Distinct()
                        .OrderBy(s => s)
                        .ToList();

                    if (sizes.Any())
                    {
                        prod.SizeRange = sizes.First() + " - " + sizes.Last();
                    }
                }

                return Ok(new
                {
                    success = true,
                    data = products,
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
                    message = "Lỗi khi lấy danh sách sản phẩm",
                    error = ex.Message
                });
            }
        }

        // GET: api/admin/products/{id} - Chi tiết sản phẩm
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductDetailDto>> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                var productDetail = new ProductDetailDto
                {
                    Id = product.Id,
                    Slug = product.Slug,
                    CategoryId = product.CategoryId,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Thumbnail = product.Thumbnail,
                    IsActive = product.IsActive,
                    Images = await _context.ProductImage
                        .Where(pi => pi.ProductId == id)
                        .OrderBy(pi => pi.SortOrder)
                        .ToListAsync(),
                    Variants = await _context.ProductVariants
                        .Where(pv => pv.ProductId == id)
                        .ToListAsync(),
                    Promotions = await _context.ProductPromotions
                        .Where(pp => pp.ProductId == id)
                        .ToListAsync(),

                    // Tính AverageRating
                    AverageRating = await _context.ProductReviews
                        .Where(pr => pr.ProductId == id)
                        .AverageAsync(pr => (double?)pr.Rating) ?? 0
                };

                // Load Colors (distinct theo ColorId)
                productDetail.Colors = await _context.ProductVariants
                    .Where(v => v.ProductId == id && v.ColorId.HasValue)
                    .GroupBy(v => v.ColorId.Value)
                    .Select(g => new ColorInfo
                    {
                        HexCode = _context.MasterColors
                            .Where(c => c.Id == g.Key)
                            .Select(c => c.HexCode ?? "#ccc")
                            .FirstOrDefault(),
                        Name = _context.MasterColors
                            .Where(c => c.Id == g.Key)
                            .Select(c => c.Name ?? "")
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                // Load Sizes (danh sách tên size unique)
                productDetail.Sizes = await _context.ProductVariants
                    .Where(v => v.ProductId == id && v.SizeId.HasValue)
                    .Select(v => _context.MasterSizes
                        .Where(s => s.Id == v.SizeId.Value)
                        .Select(s => s.Name)
                        .FirstOrDefault())
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct()
                    .OrderBy(name => name)
                    .ToListAsync();

                return Ok(new { success = true, data = productDetail });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi lấy chi tiết sản phẩm", error = ex.Message });
            }
        }

        // POST: api/admin/products - Tạo mới
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Products>> CreateProduct([FromBody] ProductCreateDto productDto)
        {
            try
            {
                // Kiểm tra slug trùng
                if (await _context.Products.AnyAsync(p => p.Slug == productDto.Slug))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Slug đã tồn tại"
                    });
                }

                var product = new Products
                {
                    Slug = productDto.Slug,
                    CategoryId = productDto.CategoryId,
                    Name = productDto.Name,
                    Description = productDto.Description,
                    Price = productDto.Price,
                    Thumbnail = productDto.Thumbnail,
                    IsActive = productDto.IsActive
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Thêm hình ảnh nếu có
                if (productDto.Images != null && productDto.Images.Any())
                {
                    var images = productDto.Images.Select((img, index) => new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = img.ImageUrl,
                        SortOrder = img.SortOrder > 0 ? img.SortOrder : index + 1
                    }).ToList();

                    _context.ProductImage.AddRange(images);
                }

                // Thêm variants nếu có
                if (productDto.Variants != null && productDto.Variants.Any())
                {
                    var variants = productDto.Variants.Select(v => new ProductVariants
                    {
                        ProductId = product.Id,
                        ColorId = v.ColorId,
                        SizeId = v.SizeId,
                        SKU = v.SKU,
                        Quantity = v.Quantity,
                        PriceModifier = v.PriceModifier
                    }).ToList();

                    _context.ProductVariants.AddRange(variants);
                }

                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, new
                {
                    success = true,
                    message = "Tạo sản phẩm thành công",
                    data = product
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi tạo sản phẩm",
                    error = ex.Message
                });
            }
        }

        // PUT: api/admin/products/{id} - Cập nhật
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpdateDto productDto)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm thấy sản phẩm"
                    });
                }

                // Kiểm tra slug trùng (trừ chính nó)
                if (await _context.Products.AnyAsync(p => p.Slug == productDto.Slug && p.Id != id))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Slug đã tồn tại"
                    });
                }

                // Cập nhật thông tin sản phẩm
                product.Slug = productDto.Slug;
                product.CategoryId = productDto.CategoryId;
                product.Name = productDto.Name;
                product.Description = productDto.Description;
                product.Price = productDto.Price;
                product.Thumbnail = productDto.Thumbnail;
                product.IsActive = productDto.IsActive;

                _context.Entry(product).State = EntityState.Modified;

                // Cập nhật hình ảnh nếu có
                if (productDto.Images != null)
                {
                    // Xóa hình ảnh cũ
                    var oldImages = _context.ProductImage.Where(pi => pi.ProductId == id);
                    _context.ProductImage.RemoveRange(oldImages);

                    // Thêm hình ảnh mới
                    var newImages = productDto.Images.Select((img, index) => new ProductImage
                    {
                        ProductId = id,
                        ImageUrl = img.ImageUrl,
                        SortOrder = img.SortOrder > 0 ? img.SortOrder : index + 1
                    }).ToList();

                    _context.ProductImage.AddRange(newImages);
                }

                // Cập nhật variants nếu có
                if (productDto.Variants != null)
                {
                    // Xóa variants cũ
                    var oldVariants = _context.ProductVariants.Where(pv => pv.ProductId == id);
                    _context.ProductVariants.RemoveRange(oldVariants);

                    // Thêm variants mới
                    var newVariants = productDto.Variants.Select(v => new ProductVariants
                    {
                        ProductId = id,
                        ColorId = v.ColorId,
                        SizeId = v.SizeId,
                        SKU = v.SKU,
                        Quantity = v.Quantity,
                        PriceModifier = v.PriceModifier
                    }).ToList();

                    _context.ProductVariants.AddRange(newVariants);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật sản phẩm thành công",
                    data = product
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ProductExists(id))
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm thấy sản phẩm"
                    });
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi cập nhật sản phẩm",
                    error = ex.Message
                });
            }
        }

        // DELETE: api/admin/products/{id} - Xóa
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm thấy sản phẩm"
                    });
                }

                // Kiểm tra xem sản phẩm có trong đơn hàng không
                var hasOrders = await _context.OrderDetails.AnyAsync(od => od.Id == id);
                if (hasOrders)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Không thể xóa sản phẩm đã có trong đơn hàng. Bạn có thể đặt IsActive = false để ẩn sản phẩm."
                    });
                }

                // Xóa các bản ghi liên quan
                var images = _context.ProductImage.Where(pi => pi.ProductId == id);
                _context.ProductImage.RemoveRange(images);

                var variants = _context.ProductVariants.Where(pv => pv.ProductId == id);
                _context.ProductVariants.RemoveRange(variants);

                var promotions = _context.ProductPromotions.Where(pp => pp.ProductId == id);
                _context.ProductPromotions.RemoveRange(promotions);

                var reviews = _context.ProductReviews.Where(pr => pr.ProductId == id);
                _context.ProductReviews.RemoveRange(reviews);

                // Xóa sản phẩm
                _context.Products.Remove(product);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Xóa sản phẩm thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi xóa sản phẩm",
                    error = ex.Message
                });
            }
        }

        private async Task<bool> ProductExists(int id)
        {
            return await _context.Products.AnyAsync(e => e.Id == id);
        }
    }

    // DTO Classes
    public class ProductDto
    {
        public int Id { get; set; }
        public string Slug { get; set; }
        public int? CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Thumbnail { get; set; }
        public bool IsActive { get; set; }
        public int ImageCount { get; set; }
        public int VariantCount { get; set; }
        public int ReviewCount { get; set; }
        public double AverageRating { get; set; }
        public List<ColorInfo> Colors { get; set; } = new();
        public string SizeRange { get; set; } = "";
    }

    public class ProductDetailDto
    {
        public int Id { get; set; }
        public string Slug { get; set; }
        public int? CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Thumbnail { get; set; }
        public bool IsActive { get; set; }
        public List<ProductImage> Images { get; set; }
        public List<ProductVariants> Variants { get; set; }
        public List<ProductPromotions> Promotions { get; set; }
        public double AverageRating { get; set; }
        public List<ColorInfo> Colors { get; set; } = new();
        public List<string> Sizes { get; set; } = new();
    }

    public class ProductCreateDto
    {
        public string Slug { get; set; }
        public int? CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Thumbnail { get; set; }
        public bool IsActive { get; set; }
        public List<ProductImageDto> Images { get; set; }
        public List<ProductVariantDto> Variants { get; set; }
    }

    public class ProductUpdateDto
    {
        public string Slug { get; set; }
        public int? CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Thumbnail { get; set; }
        public bool IsActive { get; set; }
        public List<ProductImageDto> Images { get; set; }
        public List<ProductVariantDto> Variants { get; set; }
    }

    public class ProductImageDto
    {
        public string ImageUrl { get; set; }
        public int SortOrder { get; set; }
    }

    public class ProductVariantDto
    {
        public int? ColorId { get; set; }
        public int? SizeId { get; set; }
        public string SKU { get; set; }
        public int Quantity { get; set; }
        public decimal PriceModifier { get; set; }
    }
}

public class ColorInfo
{
    public string HexCode { get; set; } = "#ccc";
    public string Name { get; set; } = "";
}