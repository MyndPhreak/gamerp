namespace GameRP.Api.Models;

/// <summary>
/// Wallet data response
/// </summary>
public class WalletResponse
{
    /// <summary>
    /// Player's Steam ID
    /// </summary>
    public long SteamId { get; set; }

    /// <summary>
    /// Current balance in dollars
    /// </summary>
    public long Balance { get; set; }

    /// <summary>
    /// Total money earned all-time
    /// </summary>
    public long TotalEarned { get; set; }

    /// <summary>
    /// Total money spent all-time
    /// </summary>
    public long TotalSpent { get; set; }

    /// <summary>
    /// When the wallet was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last transaction timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; }
}
