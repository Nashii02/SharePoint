using System.ComponentModel.DataAnnotations;

namespace Sharepoint.Models
{
    public class RegisterViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";
        public string? Nickname { get; set; }

        [Required, MinLength(6)]
        public string Password { get; set; } = "";

        [Required, Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}