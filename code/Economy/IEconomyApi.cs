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
/// Federal Reserve economic stats
/// </summary>
public class FederalReserveStats
{
	public int TotalGoldReserves { get; set; }
	public decimal TotalCurrencyInCirculation { get; set; }
	public decimal ExchangeRate { get; set; }
	public decimal GoldBackingRatio { get; set; }
	public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request to deposit gold bars at the Federal Reserve
/// </summary>
public class GoldDepositRequest
{
	public long SteamId { get; set; }
	public int GoldBars { get; set; }
}

/// <summary>
/// Request to withdraw gold bars from the Federal Reserve
/// </summary>
public class GoldWithdrawRequest
{
	public long SteamId { get; set; }
	public int GoldBars { get; set; }
}

/// <summary>
/// Result of a gold deposit or withdrawal operation
/// </summary>
public class GoldOperationResult
{
	public decimal CurrencyAmount { get; set; }
	public int GoldBars { get; set; }
	public decimal NewWalletBalance { get; set; }
	public FederalReserveStats FederalReserveStats { get; set; }
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

	/// <summary>
	/// Get Federal Reserve economic stats
	/// </summary>
	Task<FederalReserveStats> GetFederalReserveStatsAsync();

	/// <summary>
	/// Deposit gold bars at the Federal Reserve in exchange for currency
	/// </summary>
	Task<GoldOperationResult> DepositGoldAsync( long steamId, int goldBars );

	/// <summary>
	/// Withdraw gold bars from the Federal Reserve by paying currency
	/// </summary>
	Task<GoldOperationResult> WithdrawGoldAsync( long steamId, int goldBars );
}
