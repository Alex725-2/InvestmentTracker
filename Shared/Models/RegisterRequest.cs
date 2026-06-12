using System.ComponentModel.DataAnnotations;

namespace InvestmentTracker.Shared.Models
{
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(4)]
        public string Password { get; set; } = string.Empty;

        public string? FullName { get; set; }
    }
}
