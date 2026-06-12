namespace InvestmentTracker.Shared.Models
{
    public class AccountDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int BrokerId { get; set; }
        public string? BrokerName { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public decimal CommissionRate { get; set; }
        public int CurrencyId { get; set; }
        public string? CurrencyCode { get; set; }
    }
}