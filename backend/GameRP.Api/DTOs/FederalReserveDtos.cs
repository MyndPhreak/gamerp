using System.ComponentModel.DataAnnotations;

namespace GameRP.Api.DTOs;

/// <summary>
/// Public economic stats from the Federal Reserve
/// </summary>
public class FederalReserveStatsDto
{
    public int TotalGoldReserves { get; set; }
    public decimal TotalCurrencyInCirculation { get; set; }
    public decimal ExchangeRate { get; set; }

    /// <summary>
    /// Ratio of gold value to currency in circulation (should be 1.0 for 100% backing)
    /// </summary>
    public decimal GoldBackingRatio { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request to deposit gold bars at the Federal Reserve in exchange for currency
/// </summary>
public class GoldDepositRequestDto
{
    [Required]
    public long SteamId { get; set; }

    [Required]
    [Range(1, 1000)]
    public int GoldBars { get; set; }
}

/// <summary>
/// Request to withdraw gold bars from the Federal Reserve by paying currency
/// </summary>
public class GoldWithdrawRequestDto
{
    [Required]
    public long SteamId { get; set; }

    [Required]
    [Range(1, 1000)]
    public int GoldBars { get; set; }
}

/// <summary>
/// Result of a gold deposit or withdrawal operation
/// </summary>
public class GoldOperationResultDto
{
    public decimal CurrencyAmount { get; set; }
    public int GoldBars { get; set; }
    public decimal NewWalletBalance { get; set; }
    public FederalReserveStatsDto FederalReserveStats { get; set; } = null!;
}
