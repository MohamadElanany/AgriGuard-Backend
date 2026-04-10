using Microsoft.AspNetCore.Http;

namespace AgriGuard.API.DTOs
{
    public class CreatePostDto
    {

        public string Content { get; set; } = string.Empty;

        public IFormFile? Image { get; set; }
    }
}