using LapTrinhWeb.Models;
namespace LapTrinhWeb.Models
{
    public class Articles
    {
        public int Id { get; set; }
        public int? CategoryId { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Summary { get; set; }
        public string Content { get; set; }
        public string Thumbnail { get; set; }
        public Boolean IsPublished { get; set; }
        public DateTime PublishedAt { get; set; } = DateTime.Now;
    }
}