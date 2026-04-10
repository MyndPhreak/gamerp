using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameRP.Api.Models;

/// <summary>
/// Types of wallet transactions
/// </summary>
public enum TransactionType
{
    Deposit = 0,
    Withdrawal = 1,
    Transfer = 2,
    Purchase = 3,
    Salary = 4,
    Reward = 5,
    GoldDeposit = 6,
    GoldWithdrawal = 7
}

/// <summary>
/// Represents a wallet transaction
/// </summary>
public class Transaction : BaseEntity
{
    [Required]
    public Guid PlayerId { get; set; }

    [Required]
    public long SteamId { get; set; }

    /// <summary>
    /// Amount of the transaction (positive for deposits, negative for withdrawals)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Balance after this transaction
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal BalanceAfter { get; set; }

    [Required]
    public TransactionType Type { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// For transfers: the other player's SteamID
    /// </summary>
    public long? RelatedSteamId { get; set; }

    // Navigation property
    [ForeignKey(nameof(PlayerId))]
    public Player? Player { get; set; }

    public Transaction()
    {
        Id = NewGuidV7();
    }
}
