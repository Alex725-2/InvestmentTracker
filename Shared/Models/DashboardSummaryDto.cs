namespace InvestmentTracker.Shared.Models
{
    public class DashboardSummaryDto
    {
        public decimal TotalMarketValue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalPnL { get; set; }
        public decimal TodayPnL { get; set; } // изменение с предыдущего дня (упростим)
        public List<AssetTypeAllocationDto> Allocation { get; set; } = new();
    }

    public class AssetTypeAllocationDto
    {
        public string AssetTypeName { get; set; } = string.Empty;
        public decimal TotalValue { get; set; }
        public decimal Percentage { get; set; }
    }
}