namespace AgriGuard.API.Models
{
    public class User
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Country { get; set; } = string.Empty;

        public string Governorate { get; set; } = string.Empty;

        public string Role { get; set; } = "User";

        public bool IsBlocked { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? ProfileImageUrl { get; set; }

        public string? Bio { get; set; }

        public string? PasswordResetCode { get; set; }

        public DateTime? PasswordResetCodeExpiresAt { get; set; }

        public bool IsPasswordResetCodeUsed { get; set; } = false;
    }
}
