namespace InvestmentTracker.Server.Models
{
    public class Quote
    {
        public long Id { get; set; }
        public int SecurityId { get; set; }
        public Security Security { get; set; } = null!;
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
        public string Source { get; set; } = "MOEX_ISS";
    }
}