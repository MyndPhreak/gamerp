namespace GameRP.Api.DTOs;

/// <summary>
/// A single point in Fed history (for graphs)
/// </summary>
public class FederalReserveSnapshotDto
{
    public int TotalGoldReserves { get; set; }
    public decimal TotalCurrencyInCirculation { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal GoldBackingRatio { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Historical Fed data for rendering graphs
/// </summary>
public class FederalReserveHistoryDto
{
    public List<FederalReserveSnapshotDto> Snapshots { get; set; } = new();
    public FederalReserveStatsDto CurrentStats { get; set; } = null!;
}
