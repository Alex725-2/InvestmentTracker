namespace InvestmentTracker.Shared.Models
{
    public class BrokerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? SiteUrl { get; set; }
        public decimal DefaultCommissionRate { get; set; }
        public bool IsApproved { get; set; }
    }
}