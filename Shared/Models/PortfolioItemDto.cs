namespace InvestmentTracker.Shared.Models
{
    public class PortfolioItemDto
    {
        public int Id { get; set; }
        public int SecurityId { get; set; }
        public string SecurityTicker { get; set; } = string.Empty;
        public int AccountId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AveragePurchasePrice { get; set; }
        public decimal? CurrentPrice { get; set; } // null, пока нет котировок
    }
}