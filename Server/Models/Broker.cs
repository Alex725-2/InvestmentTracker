using System.ComponentModel.DataAnnotations;

namespace InvestmentTracker.Server.Models
{
    public class Broker
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? SiteUrl { get; set; }

        public decimal DefaultCommissionRate { get; set; }

        public bool IsApproved { get; set; }

        public ICollection<Account> Accounts { get; set; } = new List<Account>();
    }
}