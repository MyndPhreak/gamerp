using Sandbox;
using System;
using System.Threading.Tasks;

namespace GameRP.Economy;

/// <summary>
/// Wallet data from the API
/// </summary>
public class WalletData
{
	public long SteamId { get; set; }
	public long Balance { get; set; }
	public long TotalEarned { get; set; }
	public long TotalSpent { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Interface for Economy API communication
/// </summary>
public interface IEconomyApi
{
	/// <summary>
	/// Get wallet information for a player
	/// </summary>
	/// <param name="steamId">Player's Steam ID</param>
	/// <returns>Wallet data or null if failed</returns>
	Task<WalletData> GetWalletAsync( long steamId );

	/// <summary>
	/// Check if the API is healthy
	/// </summary>
	/// <returns>True if API is responding</returns>
	Task<bool> HealthCheckAsync();
}
