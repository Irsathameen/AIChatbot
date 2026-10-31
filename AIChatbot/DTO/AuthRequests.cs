using System.ComponentModel.DataAnnotations;

namespace AIChatbot.DTO
{
    public class AuthRequest
    {
        [Required]
        [EmailAddress]
        [MaxLength(320)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(128)]
        public string Password { get; set; } = string.Empty;
    }
}
