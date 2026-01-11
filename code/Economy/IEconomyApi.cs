using Sandbox;
using System;
using System.Threading.Tasks;

namespace GameRP.Economy;

/// <summary>
/// Wallet data from the API
/// </summary>
public class WalletData
{
	public string Id { get; set; }
	public long SteamId { get; set; }
	public decimal Balance { get; set; }
	public decimal TotalEarned { get; set; }
	public decimal TotalSpent { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request for deposit operation
/// </summary>
public class DepositRequest
{
	public decimal Amount { get; set; }
	public string Description { get; set; }
}

/// <summary>
/// Request for withdrawal operation
/// </summary>
public class WithdrawRequest
{
	public decimal Amount { get; set; }
	public string Description { get; set; }
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
	/// Deposit money into a wallet
	/// </summary>
	/// <param name="steamId">Player's Steam ID</param>
	/// <param name="request">Deposit details</param>
	/// <returns>Updated wallet data or null if failed</returns>
	Task<WalletData> DepositAsync( long steamId, DepositRequest request );

	/// <summary>
	/// Withdraw money from a wallet
	/// </summary>
	/// <param name="steamId">Player's Steam ID</param>
	/// <param name="request">Withdrawal details</param>
	/// <returns>Updated wallet data or null if failed</returns>
	Task<WalletData> WithdrawAsync( long steamId, WithdrawRequest request );

	/// <summary>
	/// Check if the API is healthy
	/// </summary>
	/// <returns>True if API is responding</returns>
	Task<bool> HealthCheckAsync();
}
