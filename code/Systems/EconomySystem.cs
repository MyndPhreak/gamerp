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
	public static async Task<long> GetBalance( long steamId )
	{
		var wallet = await Api.GetWalletAsync( steamId );
		return wallet?.Balance ?? 0;
	}
}
