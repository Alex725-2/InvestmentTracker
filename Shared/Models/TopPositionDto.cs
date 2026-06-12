public class TopPositionDto
{
    public string Ticker { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal? ChangePercent { get; set; }
    public decimal TotalValue { get; set; }
}