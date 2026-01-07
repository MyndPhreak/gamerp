using Sandbox;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Local file-based database provider (in-memory for S&Box)
/// </summary>
public class LocalDatabase : IPlayerDatabase
{
	private readonly string _databaseName;
	private readonly Dictionary<long, PlayerData> _players = new();

	public LocalDatabase( string databaseName )
	{
		_databaseName = databaseName;
		Log.Info( $"[LocalDatabase] Initialized: {_databaseName}" );
	}

	public Task SavePlayer( PlayerData data )
	{
		if ( data == null )
		{
			Log.Warning( "[LocalDatabase] Attempted to save null player data" );
			return Task.CompletedTask;
		}

		_players[data.SteamId] = data;
		Log.Info( $"[LocalDatabase] Saved player {data.Name} (SteamID: {data.SteamId})" );

		// TODO: In a real implementation, persist to FileSystem when available
		// For now, this is in-memory only

		return Task.CompletedTask;
	}

	public Task<PlayerData> GetPlayer( long steamId )
	{
		if ( _players.TryGetValue( steamId, out var data ) )
		{
			Log.Info( $"[LocalDatabase] Retrieved player data for SteamID: {steamId}" );
			return Task.FromResult( data );
		}

		Log.Info( $"[LocalDatabase] No data found for SteamID: {steamId}" );
		return Task.FromResult<PlayerData>( null );
	}
}
