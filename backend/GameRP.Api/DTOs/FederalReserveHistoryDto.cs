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

/// <summary>
/// A gold transaction for display in the Fed dashboard
/// </summary>
public class FederalReserveTransactionDto
{
    public string Type { get; set; } = null!;   // "deposit" or "withdrawal"
    public int GoldBars { get; set; }
    public decimal CurrencyAmount { get; set; }
    public DateTime Timestamp { get; set; }

    // Only populated for admin requests
    public long? SteamId { get; set; }
    public string? PlayerName { get; set; }
}

/// <summary>
/// Request to update the Federal Reserve exchange rate
/// </summary>
public class UpdateExchangeRateDto
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.Range(1, 1000000)]
    public decimal NewRate { get; set; }
}
