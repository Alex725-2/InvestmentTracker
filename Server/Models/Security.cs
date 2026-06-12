using System.ComponentModel.DataAnnotations;

namespace InvestmentTracker.Server.Models
{
    public class Security
    {
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string Ticker { get; set; } = string.Empty;

        [MaxLength(12)]
        public string? Isin { get; set; }

        [Required, MaxLength(500)]
        public string Name { get; set; } = string.Empty;

        public int AssetTypeId { get; set; }
        public AssetType AssetType { get; set; } = null!;

        // Навигационное свойство
        public ICollection<PortfolioItem> PortfolioItems { get; set; } = new List<PortfolioItem>();
    }
}