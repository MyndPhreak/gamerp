using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameRP.Api.Models;

/// <summary>
/// Point-in-time snapshot of Federal Reserve state, created after each gold operation.
/// Used to render historical graphs in the dashboard.
/// </summary>
public class FederalReserveSnapshot : BaseEntity
{
    [Required]
    public int TotalGoldReserves { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCurrencyInCirculation { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ExchangeRate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal GoldBackingRatio { get; set; }

    public FederalReserveSnapshot()
    {
        Id = NewGuidV7();
    }
}
