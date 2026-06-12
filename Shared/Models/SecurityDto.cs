namespace InvestmentTracker.Shared.Models
{
    public class SecurityDto
    {
        public int Id { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string? Isin { get; set; }
        public string Name { get; set; } = string.Empty;
        public int AssetTypeId { get; set; }
        public string? AssetTypeName { get; set; } // для отображения
    }
}