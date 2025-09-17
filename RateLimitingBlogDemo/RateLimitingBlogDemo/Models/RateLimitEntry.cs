namespace RateLimitingBlogDemo.Models
{
    public class RateLimitEntry
    {
        public int Count { get; set; }
        public DateTime StartTime { get; set; }
    }
}
