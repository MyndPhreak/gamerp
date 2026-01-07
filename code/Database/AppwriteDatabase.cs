using Sandbox;
using System.Threading.Tasks;

/// <summary>
/// Appwrite database provider (not yet implemented)
/// </summary>
public class AppwriteDatabase : IPlayerDatabase
{
	private readonly string _endpoint;
	private readonly string _projectId;
	private readonly string _databaseId;
	private readonly string _collectionId;
	private readonly string _apiKey;

	public AppwriteDatabase( string endpoint, string projectId, string databaseId, string collectionId, string apiKey )
	{
		_endpoint = endpoint;
		_projectId = projectId;
		_databaseId = databaseId;
		_collectionId = collectionId;
		_apiKey = apiKey;

		Log.Info( "[AppwriteDatabase] Initialized (not yet implemented)" );
	}

	public Task SavePlayer( PlayerData data )
	{
		Log.Warning( "[AppwriteDatabase] SavePlayer not yet implemented" );
		// TODO: Implement Appwrite API calls when HTTP whitelisting is configured
		return Task.CompletedTask;
	}

	public Task<PlayerData> GetPlayer( long steamId )
	{
		Log.Warning( "[AppwriteDatabase] GetPlayer not yet implemented" );
		// TODO: Implement Appwrite API calls when HTTP whitelisting is configured
		return Task.FromResult<PlayerData>( null );
	}
}
