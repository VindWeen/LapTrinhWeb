using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LapTrinhWeb.Data;
using LapTrinhWeb.Models;

namespace LapTrinhWeb.Controllers
{
    [ApiController]
    [Route("api/articles")]
    public class ArticleController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ArticleController(AppDbContext context)
        {
            _context = context;
        }

        // ======================================================
        // 1️⃣ LẤY VÀ VIẾT BÀI VIẾT
        // ======================================================

        // Lấy tất cả bài viết (Admin, Staff xem được tất cả, User chỉ xem được bài đã xuất bản)
        [HttpGet("admin")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAllArticlesAdmin()
        {
            var articles = await _context.Articles
                .OrderByDescending(a => a.PublishedAt)
                .ToListAsync();

            return Ok(articles);
        }

        // Lấy bài viết đã xuất bản cho người dùng
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublishedArticles()
        {
            var articles = await _context.Articles
                .Where(a => a.IsPublished)
                .OrderByDescending(a => a.PublishedAt)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Slug,
                    a.Summary,
                    a.Thumbnail,
                    a.PublishedAt
                })
                .ToListAsync();

            return Ok(articles);
        }

        // Lấy chi tiết bài viết (Admin, Staff xem được tất cả, User chỉ xem được bài đã xuất bản)
        [HttpGet("admin/{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetArticleByIdAdmin(int id)
        {
            var article = await _context.Articles
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
                return NotFound();

            return Ok(article);
        }

        // Lấy chi tiết bài viết cho người dùng (chỉ xem được bài đã xuất bản)
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetArticleById(int id)
        {
            var article = await _context.Articles
                .Where(a => a.IsPublished && a.Id == id)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Content,
                    a.PublishedAt
                })
                .FirstOrDefaultAsync();

            if (article == null)
                return NotFound();

            return Ok(article);
        }

        //Viết bài mới (Admin, Staff)
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> CreateArticle([FromBody] Articles model)
        {
            model.PublishedAt = DateTime.Now;

            _context.Articles.Add(model);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tạo bài viết thành công" });
        }

        // ======================================================
        // 2️⃣ SỬA BÀI (Admin, Staff)
        // ======================================================
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateArticle(int id, [FromBody] Articles model)
        {
            if (id != model.Id)
                return BadRequest("Id không khớp");

            var article = await _context.Articles.FindAsync(id);

            if (article == null)
                return NotFound("Không tìm thấy bài viết");

            article.Title = model.Title;
            article.Slug = model.Slug;
            article.Summary = model.Summary;
            article.Content = model.Content;
            article.Thumbnail = model.Thumbnail;
            article.CategoryId = model.CategoryId;
            article.IsPublished = model.IsPublished;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật bài viết thành công" });
        }

        // ======================================================
        // 3️⃣ XÓA BÀI (Admin only)
        // ======================================================
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteArticle(int id)
        {
            var article = await _context.Articles.FindAsync(id);

            if (article == null)
                return NotFound("Không tìm thấy bài viết");

            _context.Articles.Remove(article);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa bài viết thành công" });
        }

        // ======================================================
        // 4️⃣ QUẢN LÝ DANH MỤC (Admin only)
        // ======================================================
        // Lấy danh sách danh mục
        [HttpGet("categories")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _context.ArticleCategories
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Ok(categories);
        }

        // Lấy chi tiết danh mục
        [HttpGet("categories/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var category = await _context.ArticleCategories
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            return Ok(category);
        }

        // Thêm danh mục
        [HttpPost("categories")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategory([FromBody] ArticleCategories model)
        {
            _context.ArticleCategories.Add(model);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tạo danh mục thành công" });
        }

        // Sửa danh mục
        [HttpPut("categories/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] ArticleCategories model)
        {
            if (id != model.Id)
                return BadRequest();

            var category = await _context.ArticleCategories.FindAsync(id);

            if (category == null)
                return NotFound();

            category.Name = model.Name;
            category.Slug = model.Slug;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật danh mục thành công" });
        }

        // Xóa danh mục
        [HttpDelete("categories/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.ArticleCategories.FindAsync(id);

            if (category == null)
                return NotFound();

            _context.ArticleCategories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa danh mục thành công" });
        }

        // ======================================================
        // 5️⃣ KIỂM DUYỆT REVIEW (Admin, Staff)
        // ======================================================
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> ApproveArticle(int id)
        {
            var article = await _context.Articles.FindAsync(id);

            if (article == null)
                return NotFound();

            article.IsPublished = true;
            article.PublishedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Bài viết đã được duyệt" });
        }
    }
}