using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public sealed class DatabaseService : Component
{
	public static DatabaseService Instance { get; private set; }

	public enum ProviderType { Local, Appwrite, External }
	[Property] public ProviderType Provider { get; set; } = ProviderType.Local;

	[Property, Group("Local"), ShowIf("Provider", ProviderType.Local)] public string LocalDatabaseName { get; set; } = "gamerp.json";

	[Property, Group("Appwrite"), ShowIf("Provider", ProviderType.Appwrite)] public string Endpoint { get; set; } = "https://cloud.appwrite.io/v1";
	[Property, Group("Appwrite"), ShowIf("Provider", ProviderType.Appwrite)] public string ProjectId { get; set; }
	[Property, Group("Appwrite"), ShowIf("Provider", ProviderType.Appwrite)] public string DatabaseId { get; set; }
	[Property, Group("Appwrite"), ShowIf("Provider", ProviderType.Appwrite)] public string CollectionId { get; set; }
	[Property, Group("Appwrite"), ShowIf("Provider", ProviderType.Appwrite)] public string ApiKey { get; set; }

	[Property, Group("External"), ShowIf("Provider", ProviderType.External)] public string ExternalConnectionString { get; set; }

	private IPlayerDatabase _activeProvider;

	protected override void OnAwake()
	{
		Instance = this;
		InitializeProvider();
	}

	private void InitializeProvider()
	{
		if ( Provider == ProviderType.Local )
		{
			_activeProvider = new LocalDatabase( LocalDatabaseName );
			Log.Info( "Database initialized with Local Provider" );
		}
		else if ( Provider == ProviderType.Appwrite )
		{
			_activeProvider = new AppwriteDatabase( Endpoint, ProjectId, DatabaseId, CollectionId, ApiKey );
			Log.Info( "Database initialized with Appwrite Provider" );
		}
		else if ( Provider == ProviderType.External )
		{
			// TODO: Implement ExternalDatabase provider
			Log.Warning( "External Database Provider selected but not yet implemented" );
		}
	}

	public async void SavePlayer( PlayerData data )
	{
		if ( _activeProvider == null ) return;
		await _activeProvider.SavePlayer( data );
	}

	public async Task<PlayerData> GetPlayer( long steamId )
	{
		if ( _activeProvider == null ) return null;
		return await _activeProvider.GetPlayer( steamId );
	}
}
