using Sandbox;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// HTTP-based database provider. Persists player data to the GameRP backend API.
/// Set DatabaseService.ExternalConnectionString to your API base URL (e.g. http://localhost:8080/api).
/// </summary>
public class ExternalDatabase : IPlayerDatabase
{
	private readonly string _baseUrl;

	public ExternalDatabase( string baseUrl )
	{
		_baseUrl = baseUrl.TrimEnd( '/' );
		Log.Info( $"[ExternalDatabase] Initialized with URL: {_baseUrl}" );
	}

	public async Task<PlayerData> GetPlayer( long steamId )
	{
		try
		{
			var url = $"{_baseUrl}/wallet/{steamId}/profile";
			var response = await Http.RequestAsync( url );

			if ( response.StatusCode == System.Net.HttpStatusCode.NotFound )
			{
				Log.Info( $"[ExternalDatabase] No player found for SteamID: {steamId}" );
				return null;
			}

			if ( !response.IsSuccessStatusCode )
			{
				Log.Warning( $"[ExternalDatabase] GetPlayer failed. Status: {response.StatusCode}" );
				return null;
			}

			var json = await response.Content.ReadAsStringAsync();
			var profile = Json.Deserialize<ExternalPlayerProfile>( json );

			if ( profile == null )
			{
				Log.Warning( $"[ExternalDatabase] Failed to deserialize profile for {steamId}" );
				return null;
			}

			Log.Info( $"[ExternalDatabase] Loaded {profile.DisplayName} (SteamID: {steamId})" );

			return new PlayerData
			{
				SteamId = profile.SteamId,
				DisplayName = profile.DisplayName,
				Gender = profile.Gender,
				DateOfBirth = profile.DateOfBirth,
				SkinTone = profile.SkinTone,
				Height = profile.Height,
				Age = profile.Age,
				Money = profile.Money,
				JobTitle = profile.JobTitle,
				ClothingList = profile.ClothingList,
				HasCompletedCharacterCreation = profile.HasCompletedCharacterCreation,
				LastSeen = profile.LastSeen
			};
		}
		catch ( Exception ex )
		{
			Log.Error( $"[ExternalDatabase] Error getting player {steamId}: {ex.Message}" );
			return null;
		}
	}

	public async Task SavePlayer( PlayerData data )
	{
		try
		{
			var url = $"{_baseUrl}/wallet/{data.SteamId}/profile";

			var payload = new ExternalPlayerSave
			{
				DisplayName = data.DisplayName,
				Gender = data.Gender,
				DateOfBirth = data.DateOfBirth,
				SkinTone = data.SkinTone,
				Height = data.Height,
				Age = data.Age,
				Money = data.Money,
				JobTitle = data.JobTitle,
				ClothingList = data.ClothingList,
				HasCompletedCharacterCreation = data.HasCompletedCharacterCreation
			};

			var json = Json.Serialize( payload );
			var content = new StringContent( json, Encoding.UTF8, "application/json" );
			var response = await Http.RequestAsync( url, "PUT", content );

			if ( !response.IsSuccessStatusCode )
			{
				Log.Warning( $"[ExternalDatabase] SavePlayer failed for {data.SteamId}. Status: {response.StatusCode}" );
				return;
			}

			Log.Info( $"[ExternalDatabase] Saved {data.DisplayName} (SteamID: {data.SteamId})" );
		}
		catch ( Exception ex )
		{
			Log.Error( $"[ExternalDatabase] Error saving player {data.SteamId}: {ex.Message}" );
		}
	}
}

/// <summary>Matches the backend PlayerProfileDto JSON shape</summary>
public class ExternalPlayerProfile
{
	public long SteamId { get; set; }
	public string DisplayName { get; set; } = string.Empty;
	public string Gender { get; set; } = string.Empty;
	public string DateOfBirth { get; set; } = string.Empty;
	public float SkinTone { get; set; } = 0.5f;
	public float Height { get; set; } = 0.5f;
	public float Age { get; set; } = 0.5f;
	public int Money { get; set; }
	public string JobTitle { get; set; } = string.Empty;
	public string ClothingList { get; set; } = "[]";
	public bool HasCompletedCharacterCreation { get; set; }
	public DateTime LastSeen { get; set; }
}

/// <summary>Request payload for PUT /profile (no SteamId, no LastSeen)</summary>
public class ExternalPlayerSave
{
	public string DisplayName { get; set; } = string.Empty;
	public string Gender { get; set; } = string.Empty;
	public string DateOfBirth { get; set; } = string.Empty;
	public float SkinTone { get; set; } = 0.5f;
	public float Height { get; set; } = 0.5f;
	public float Age { get; set; } = 0.5f;
	public int Money { get; set; }
	public string JobTitle { get; set; } = string.Empty;
	public string ClothingList { get; set; } = "[]";
	public bool HasCompletedCharacterCreation { get; set; }
}
