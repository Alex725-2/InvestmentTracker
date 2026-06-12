namespace InvestmentTracker.Server.Models
{
    public class PortfolioItem
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public int SecurityId { get; set; }
        public Security Security { get; set; } = null!;

        public int AccountId { get; set; }
        public Account Account { get; set; } = null!;

        public decimal Quantity { get; set; }
        public decimal AveragePurchasePrice { get; set; }
    }
}