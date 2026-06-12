using System.ComponentModel.DataAnnotations;

namespace InvestmentTracker.Server.Models
{
    public class AssetType
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty; // Акция, Облигация, ПИФ

        public ICollection<Security> Securities { get; set; } = new List<Security>();
    }
}