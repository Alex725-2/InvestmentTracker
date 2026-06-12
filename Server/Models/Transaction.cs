using System.ComponentModel.DataAnnotations;
using InvestmentTracker.Shared.Models;

namespace InvestmentTracker.Server.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public int SecurityId { get; set; }
        public Security Security { get; set; } = null!;

        public int AccountId { get; set; }
        public Account Account { get; set; } = null!;

        public DateTime Date { get; set; }

        public TransactionType Type { get; set; }

        public int Quantity { get; set; }

        [DataType("decimal(18,4)")]
        public decimal Price { get; set; }

        [DataType("decimal(18,4)")]
        public decimal Commission { get; set; }
    }
}