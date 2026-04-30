using System.ComponentModel.DataAnnotations;

namespace AgriGuard.API.DTOs
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}