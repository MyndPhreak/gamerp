using Sandbox;
using GameRP.Economy;
using System.Threading.Tasks;

namespace GameRP.Systems;

/// <summary>
/// Main economy system that manages the API client
/// </summary>
public static class EconomySystem
{
	private static IEconomyApi _apiClient;

	/// <summary>
	/// Get the economy API client instance
	/// </summary>
	public static IEconomyApi Api
	{
		get
		{
			if ( _apiClient == null )
			{
				Initialize();
			}
			return _apiClient;
		}
	}

	/// <summary>
	/// Initialize the economy system
	/// </summary>
	public static void Initialize( string apiUrl = "http://localhost:8080/api" )
	{
		Log.Info( "[EconomySystem] Initializing..." );
		_apiClient = new EconomyApiClient( apiUrl );
		Log.Info( "[EconomySystem] Initialized successfully" );
	}

	/// <summary>
	/// Test the API connection
	/// </summary>
	public static async Task<bool> TestConnection()
	{
		Log.Info( "[EconomySystem] Testing API connection..." );
		var isHealthy = await Api.HealthCheckAsync();

		if ( isHealthy )
		{
			Log.Info( "[EconomySystem] API connection successful!" );
		}
		else
		{
			Log.Warning( "[EconomySystem] API connection failed!" );
		}

		return isHealthy;
	}

	/// <summary>
	/// Get balance for a Steam ID
	/// </summary>
	public static async Task<decimal> GetBalance( long steamId )
	{
		var wallet = await Api.GetWalletAsync( steamId );
		return wallet?.Balance ?? 0;
	}

	/// <summary>
	/// Deposit money into a wallet
	/// </summary>
	public static async Task<WalletData> Deposit( long steamId, decimal amount, string description = "Deposit" )
	{
		var request = new DepositRequest
		{
			Amount = amount,
			Description = description
		};
		return await Api.DepositAsync( steamId, request );
	}

	/// <summary>
	/// Withdraw money from a wallet
	/// </summary>
	public static async Task<WalletData> Withdraw( long steamId, decimal amount, string description = "Withdrawal" )
	{
		var request = new WithdrawRequest
		{
			Amount = amount,
			Description = description
		};
		return await Api.WithdrawAsync( steamId, request );
	}

	/// <summary>
	/// Get Federal Reserve economic stats
	/// </summary>
	public static async Task<FederalReserveStats> GetFederalReserveStats()
	{
		return await Api.GetFederalReserveStatsAsync();
	}

	/// <summary>
	/// Deposit gold bars at the Federal Reserve in exchange for currency
	/// </summary>
	public static async Task<GoldOperationResult> DepositGold( long steamId, int goldBars )
	{
		return await Api.DepositGoldAsync( steamId, goldBars );
	}

	/// <summary>
	/// Withdraw gold bars from the Federal Reserve by paying currency
	/// </summary>
	public static async Task<GoldOperationResult> WithdrawGold( long steamId, int goldBars )
	{
		return await Api.WithdrawGoldAsync( steamId, goldBars );
	}
}
