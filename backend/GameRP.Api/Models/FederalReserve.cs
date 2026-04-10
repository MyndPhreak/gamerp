using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameRP.Api.Models;

/// <summary>
/// Singleton entity representing the Federal Reserve.
/// Controls currency issuance backed by gold reserves.
/// </summary>
public class FederalReserve : BaseEntity
{
    /// <summary>
    /// Total gold bars held in reserve
    /// </summary>
    [Required]
    public int TotalGoldReserves { get; set; } = 0;

    /// <summary>
    /// Total currency created by the Fed minus currency destroyed (net money supply)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCurrencyInCirculation { get; set; } = 0;

    /// <summary>
    /// Dollars issued per gold bar (default: $1,000)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ExchangeRate { get; set; } = 1000m;

    public FederalReserve()
    {
        Id = NewGuidV7();
    }
}
