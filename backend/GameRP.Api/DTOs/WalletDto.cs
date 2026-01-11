namespace GameRP.Api.DTOs;

/// <summary>
/// Wallet data transfer object for API responses
/// </summary>
public class WalletDto
{
    /// <summary>
    /// Wallet unique ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Player's Steam ID
    /// </summary>
    public long SteamId { get; set; }

    /// <summary>
    /// Current balance
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// Total money earned all-time
    /// </summary>
    public decimal TotalEarned { get; set; }

    /// <summary>
    /// Total money spent all-time
    /// </summary>
    public decimal TotalSpent { get; set; }

    /// <summary>
    /// When the wallet was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
