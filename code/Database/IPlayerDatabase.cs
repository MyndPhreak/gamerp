using System.Threading.Tasks;

/// <summary>
/// Interface for player database providers
/// </summary>
public interface IPlayerDatabase
{
	/// <summary>
	/// Save player data
	/// </summary>
	Task SavePlayer( PlayerData data );

	/// <summary>
	/// Get player data by Steam ID
	/// </summary>
	Task<PlayerData> GetPlayer( long steamId );
}
