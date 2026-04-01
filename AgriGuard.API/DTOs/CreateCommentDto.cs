namespace AgriGuard.API.DTOs
{
    public class CreateCommentDto
    {
        public int UserId { get; set; }

        public string Content { get; set; } = string.Empty;
    }
}