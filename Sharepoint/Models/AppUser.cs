using System.ComponentModel.DataAnnotations;

namespace Sharepoint.Models
{
    public class AppUser
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string? DisplayName { get; set; }

        public string? Email { get; set; }

        public string Role { get; set; } = "User"; // "Admin" or "User"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Avatar stored as bytes in DB
        public byte[]? AvatarData { get; set; }
        public string? AvatarMimeType { get; set; } // "image/jpeg", "image/png", etc.

        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }
    }
}