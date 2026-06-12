using System.ComponentModel.DataAnnotations;

namespace InvestmentTracker.Server.Models
{
    public class Account
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public int BrokerId { get; set; }
        public Broker Broker { get; set; } = null!;

        [Required, MaxLength(50)]
        public string AccountNumber { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Comment { get; set; }

        public decimal CommissionRate { get; set; }

        public int CurrencyId { get; set; }
        public Currency Currency { get; set; } = null!;

        public ICollection<PortfolioItem> PortfolioItems { get; set; } = new List<PortfolioItem>();
    }
}