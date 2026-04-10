using Sandbox;
using System;
using System.Threading.Tasks;

namespace GameRP.Economy;

/// <summary>
/// Client for communicating with the GameRP Economy API
/// </summary>
public class EconomyApiClient : IEconomyApi
{
	private readonly string _baseUrl;

	public EconomyApiClient( string baseUrl = "http://localhost:8080/api" )
	{
		_baseUrl = baseUrl;
		Log.Info( $"[EconomyAPI] Initialized with base URL: {_baseUrl}" );
	}

	/// <summary>
	/// Get wallet information for a player
	/// </summary>
	public async Task<WalletData> GetWalletAsync( long steamId )
	{
		try
		{
			var url = $"{_baseUrl}/wallet/{steamId}";
			Log.Info( $"[EconomyAPI] Fetching wallet for SteamID: {steamId}" );
			Log.Info( $"[EconomyAPI] Request URL: {url}" );

			var response = await Http.RequestAsync( url );

			if ( !response.IsSuccessStatusCode )
			{
				Log.Warning( $"[EconomyAPI] Failed to get wallet. Status: {response.StatusCode}" );
				return null;
			}

			// Get the response body as string
			var json = await response.Content.ReadAsStringAsync();
			Log.Info( $"[EconomyAPI] Response: {json}" );

			// Use S&Box's Json.Deserialize
			var wallet = Json.Deserialize<WalletData>( json );

			if ( wallet != null )
			{
				Log.Info( $"[EconomyAPI] Wallet retrieved successfully. Balance: ${wallet.Balance}" );
			}

			return wallet;
		}
		catch ( Exception ex )
		{
			Log.Error( $"[EconomyAPI] Error getting wallet: {ex.Message}" );
			Log.Error( $"[EconomyAPI] Stack trace: {ex.StackTrace}" );
			return null;
		}
	}

	/// <summary>
	/// Deposit money into a wallet
	/// </summary>
	public async Task<WalletData> DepositAsync( long steamId, DepositRequest request )
	{
		try
		{
			var url = $"{_baseUrl}/wallet/{steamId}/deposit";
			Log.Info( $"[EconomyAPI] Depositing ${request.Amount} for SteamID: {steamId}" );

			var json = Json.Serialize( request );
			var content = new System.Net.Http.StringContent( json, System.Text.Encoding.UTF8, "application/json" );
			var response = await Http.RequestAsync( url, "POST", content );

			if ( !response.IsSuccessStatusCode )
			{
				Log.Warning( $"[EconomyAPI] Deposit failed. Status: {response.StatusCode}" );
				return null;
			}

			var responseJson = await response.Content.ReadAsStringAsync();
			var wallet = Json.Deserialize<WalletData>( responseJson );

			if ( wallet != null )
			{
				Log.Info( $"[EconomyAPI] Deposit successful. New balance: ${wallet.Balance}" );
			}

			return wallet;
		}
		catch ( Exception ex )
		{
			Log.Error( $"[EconomyAPI] Error depositing: {ex.Message}" );
			return null;
		}
	}

	/// <summary>
	/// Withdraw money from a wallet
	/// </summary>
	public async Task<WalletData> WithdrawAsync( long steamId, WithdrawRequest request )
	{
		try
		{
			var url = $"{_baseUrl}/wallet/{steamId}/withdraw";
			Log.Info( $"[EconomyAPI] Withdrawing ${request.Amount} for SteamID: {steamId}" );

			var json = Json.Serialize( request );
			var content = new System.Net.Http.StringContent( json, System.Text.Encoding.UTF8, "application/json" );
			var response = await Http.RequestAsync( url, "POST", content );

			if ( !response.IsSuccessStatusCode )
			{
				Log.Warning( $"[EconomyAPI] Withdrawal failed. Status: {response.StatusCode}" );
				return null;
			}

			var responseJson = await response.Content.ReadAsStringAsync();
			var wallet = Json.Deserialize<WalletData>( responseJson );

			if ( wallet != null )
			{
				Log.Info( $"[EconomyAPI] Withdrawal successful. New balance: ${wallet.Balance}" );
			}

			return wallet;
		}
		catch ( Exception ex )
		{
			Log.Error( $"[EconomyAPI] Error withdrawing: {ex.Message}" );
			return null;
		}
	}

	/// <summary>
	/// Get Federal Reserve economic stats
	/// </summary>
	public async Task<FederalReserveStats> GetFederalReserveStatsAsync()
	{
		try
		{
			var url = $"{_baseUrl}/federal-reserve/stats";
			Log.Info( $"[EconomyAPI] Fetching Federal Reserve stats" );

			var response = await Http.RequestAsync( url );

			if ( !response.IsSuccessStatusCode )
			{
				Log.Warning( $"[EconomyAPI] Failed to get Fed stats. Status: {response.StatusCode}" );
				return null;
			}

			var json = await response.Content.ReadAsStringAsync();
			var stats = Json.Deserialize<FederalReserveStats>( json );

			if ( stats != null )
			{
				Log.Info( $"[EconomyAPI] Fed stats: {stats.TotalGoldReserves} gold, ${stats.TotalCurrencyInCirculation} in circulation" );
			}

			return stats;
		}
		catch ( Exception ex )
		{
			Log.Error( $"[EconomyAPI] Error getting Fed stats: {ex.Message}" );
			return null;
		}
	}

	/// <summary>
	/// Deposit gold bars at the Federal Reserve in exchange for currency
	/// </summary>
	public async Task<GoldOperationResult> DepositGoldAsync( long steamId, int goldBars )
	{
		try
		{
			var url = $"{_baseUrl}/federal-reserve/deposit-gold";
			Log.Info( $"[EconomyAPI] Depositing {goldBars} gold bars for SteamID: {steamId}" );

			var request = new GoldDepositRequest { SteamId = steamId, GoldBars = goldBars };
			var json = Json.Serialize( request );
			var content = new System.Net.Http.StringContent( json, System.Text.Encoding.UTF8, "application/json" );
			var response = await Http.RequestAsync( url, "POST", content );

			if ( !response.IsSuccessStatusCode )
			{
				Log.Warning( $"[EconomyAPI] Gold deposit failed. Status: {response.StatusCode}" );
				return null;
			}

			var responseJson = await response.Content.ReadAsStringAsync();
			var result = Json.Deserialize<GoldOperationResult>( responseJson );

			if ( result != null )
			{
				Log.Info( $"[EconomyAPI] Gold deposit successful. Received ${result.CurrencyAmount}. New balance: ${result.NewWalletBalance}" );
			}

			return result;
		}
		catch ( Exception ex )
		{
			Log.Error( $"[EconomyAPI] Error depositing gold: {ex.Message}" );
			return null;
		}
	}

	/// <summary>
	/// Withdraw gold bars from the Federal Reserve by paying currency
	/// </summary>
	public async Task<GoldOperationResult> WithdrawGoldAsync( long steamId, int goldBars )
	{
		try
		{
			var url = $"{_baseUrl}/federal-reserve/withdraw-gold";
			Log.Info( $"[EconomyAPI] Withdrawing {goldBars} gold bars for SteamID: {steamId}" );

			var request = new GoldWithdrawRequest { SteamId = steamId, GoldBars = goldBars };
			var json = Json.Serialize( request );
			var content = new System.Net.Http.StringContent( json, System.Text.Encoding.UTF8, "application/json" );
			var response = await Http.RequestAsync( url, "POST", content );

			if ( !response.IsSuccessStatusCode )
			{
				Log.Warning( $"[EconomyAPI] Gold withdrawal failed. Status: {response.StatusCode}" );
				return null;
			}

			var responseJson = await response.Content.ReadAsStringAsync();
			var result = Json.Deserialize<GoldOperationResult>( responseJson );

			if ( result != null )
			{
				Log.Info( $"[EconomyAPI] Gold withdrawal successful. Paid ${result.CurrencyAmount}. New balance: ${result.NewWalletBalance}" );
			}

			return result;
		}
		catch ( Exception ex )
		{
			Log.Error( $"[EconomyAPI] Error withdrawing gold: {ex.Message}" );
			return null;
		}
	}

	/// <summary>
	/// Check if the API is healthy
	/// </summary>
	public async Task<bool> HealthCheckAsync()
	{
		try
		{
			var url = $"{_baseUrl}/wallet/health";
			Log.Info( $"[EconomyAPI] Health check: {url}" );

			var response = await Http.RequestAsync( url );

			if ( response.IsSuccessStatusCode )
			{
				var content = await response.Content.ReadAsStringAsync();
				Log.Info( $"[EconomyAPI] Health check passed: {content}" );
				return true;
			}

			Log.Warning( $"[EconomyAPI] Health check failed. Status: {response.StatusCode}" );
			return false;
		}
		catch ( Exception ex )
		{
			Log.Error( $"[EconomyAPI] Health check error: {ex.Message}" );
			return false;
		}
	}
}
