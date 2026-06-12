using Microsoft.AspNetCore.Identity;

namespace InvestmentTracker.Server.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Здесь можно добавить дополнительные поля, например, полное имя
        public string? FullName { get; set; }
    }
}
