using System.ComponentModel.DataAnnotations;

namespace Sharepoint.Models
{
    public class ProfileViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string Role { get; set; } = "User";
        public DateTime CreatedAt { get; set; }
        public bool HasAvatar { get; set; }
        public int UploadCount { get; set; }
    }

    public class EditProfileViewModel
    {
        [StringLength(100)]
        public string? DisplayName { get; set; }

        [EmailAddress]
        [StringLength(200)]
        public string? Email { get; set; }

        public IFormFile? AvatarFile { get; set; }
        public bool RemoveAvatar { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new password.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}