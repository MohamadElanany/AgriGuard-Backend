using System.ComponentModel.DataAnnotations;

namespace AgriGuard.API.DTOs
{
    public class UpdateAccountSettingsDto
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Country { get; set; } = string.Empty;

        [Required]
        public string Governorate { get; set; } = string.Empty;

        public string? Bio { get; set; }

        public string? CurrentPassword { get; set; }

        [MinLength(6)]
        public string? NewPassword { get; set; }
    }
}