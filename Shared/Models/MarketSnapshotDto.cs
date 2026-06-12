namespace InvestmentTracker.Shared.Models
{
    public class MarketSnapshotDto
    {
        public decimal? IndexValue { get; set; }
        public decimal? IndexChangePct { get; set; }
        public string TradingStatus { get; set; } = "—";
        public string SessionCloseTime { get; set; } = "—";
        public List<MoverDto> TopGainers { get; set; } = new();
        public List<MoverDto> TopLosers { get; set; } = new();
    }

    public class MoverDto
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public decimal? ChangePct { get; set; }
    }
}