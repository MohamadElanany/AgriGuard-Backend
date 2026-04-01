namespace AgriGuard.API.Models
{
    public class Comment
    {
        public int Id { get; set; }

        public int PostId { get; set; }

        public int UserId { get; set; }

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Post Post { get; set; }

        public User User { get; set; }
    }
}