using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameRP.Api.Models;

/// <summary>
/// Represents a player's wallet/bank account
/// </summary>
public class Wallet : BaseEntity
{
    [Required]
    public Guid PlayerId { get; set; }

    [Required]
    public long SteamId { get; set; }

    /// <summary>
    /// Current balance in the wallet
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; } = 0;

    /// <summary>
    /// Total money earned (for statistics)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalEarned { get; set; } = 0;

    /// <summary>
    /// Total money spent (for statistics)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalSpent { get; set; } = 0;

    // Navigation property
    [ForeignKey(nameof(PlayerId))]
    public Player? Player { get; set; }

    public Wallet()
    {
        Id = NewGuidV7();
    }
}
