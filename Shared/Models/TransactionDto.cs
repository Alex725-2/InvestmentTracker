namespace InvestmentTracker.Shared.Models
{
    public class TransactionDto
    {
        public int Id { get; set; }
        public int SecurityId { get; set; }
        public string? SecurityTicker { get; set; }
        public int AccountId { get; set; }
        public string? AccountNumber { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public TransactionType Type { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Commission { get; set; }
        public decimal TotalAmount => Quantity * Price; // без комиссии
        public string DateString { get; set; } = string.Empty;
    }
}