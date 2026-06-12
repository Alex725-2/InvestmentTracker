using System.ComponentModel.DataAnnotations;

namespace InvestmentTracker.Server.Models
{
    public class Currency
    {
        public int Id { get; set; }

        [Required, MaxLength(3)]
        public string Code { get; set; } = string.Empty; // RUR, USD

        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public ICollection<Account> Accounts { get; set; } = new List<Account>();
    }
}