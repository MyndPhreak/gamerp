using Sandbox;
using GameRP.Systems;
using System.Threading.Tasks;

/// <summary>
/// Console commands for testing the economy system
/// </summary>
public static class EconomyCommands
{
	/// <summary>
	/// Test API health check
	/// </summary>
	[ConCmd( "economy_test" )]
	public static async Task TestEconomyApi()
	{
		Log.Info( "========================================" );
		Log.Info( "Testing Economy API Connection..." );
		Log.Info( "========================================" );

		var success = await EconomySystem.TestConnection();

		if ( success )
		{
			Log.Info( "✓ API is online and responding!" );
		}
		else
		{
			Log.Error( "✗ API is not responding!" );
			Log.Info( "Make sure the API server is running on http://localhost:5000" );
		}

		Log.Info( "========================================" );
	}

	/// <summary>
	/// Get wallet balance for current player
	/// </summary>
	[ConCmd( "economy_balance" )]
	public static async Task GetMyBalance()
	{
		// For testing, just use a default Steam ID
		long steamId = 76561198012345678;

		Log.Info( "========================================" );
		Log.Info( $"Fetching wallet for SteamID: {steamId}" );
		Log.Info( "========================================" );

		var wallet = await EconomySystem.Api.GetWalletAsync( steamId );

		if ( wallet != null )
		{
			Log.Info( $"Steam ID:      {wallet.SteamId}" );
			Log.Info( $"Balance:       ${wallet.Balance:N0}" );
			Log.Info( $"Total Earned:  ${wallet.TotalEarned:N0}" );
			Log.Info( $"Total Spent:   ${wallet.TotalSpent:N0}" );
			Log.Info( $"Created:       {wallet.CreatedAt}" );
			Log.Info( $"Last Updated:  {wallet.LastUpdated}" );
		}
		else
		{
			Log.Error( "Failed to retrieve wallet data" );
		}

		Log.Info( "========================================" );
	}

	/// <summary>
	/// Get wallet balance for any Steam ID
	/// </summary>
	[ConCmd( "economy_getbalance" )]
	public static async Task GetBalance( long steamId )
	{
		Log.Info( "========================================" );
		Log.Info( $"Fetching wallet for SteamID: {steamId}" );
		Log.Info( "========================================" );

		var balance = await EconomySystem.GetBalance( steamId );

		Log.Info( $"Balance: ${balance:N0}" );
		Log.Info( "========================================" );
	}
}
