using Sandbox;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameRP.Inventory;

// ---- DTOs (must match backend shapes) ----

public class InventoryItemDto
{
	public string InstanceId { get; set; } = "";
	public string Container { get; set; } = "main";
	public int SlotIndex { get; set; }
	public string ItemId { get; set; } = "";
	public int Quantity { get; set; }
	public int Durability { get; set; } = -1;
	public string NbtJson { get; set; } = "{}";
}

public class InventoryDto
{
	public long SteamId { get; set; }
	public int MainSlots { get; set; } = 27;
	public int HotbarSlots { get; set; } = 9;
	public int SelectedHotbarSlot { get; set; } = 0;
	public List<InventoryItemDto> Items { get; set; } = new();
	public string RowVersion { get; set; } = "";
}

public class SaveInventoryRequest
{
	public int MainSlots { get; set; } = 27;
	public int HotbarSlots { get; set; } = 9;
	public int SelectedHotbarSlot { get; set; } = 0;
	public List<InventoryItemDto> Items { get; set; } = new();
	public string RowVersion { get; set; } = "";
}

// ---- Client ----

public interface IInventoryApi
{
	Task<InventoryDto> GetInventoryAsync( long steamId );
	Task<InventoryDto> SaveInventoryAsync( long steamId, SaveInventoryRequest request );
}

public class InventoryApiClient : IInventoryApi
{
	private readonly string _baseUrl;

	public InventoryApiClient( string baseUrl = "http://localhost:8080/api" )
	{
		_baseUrl = baseUrl;
	}

	public async Task<InventoryDto> GetInventoryAsync( long steamId )
	{
		try
		{
			var url = $"{_baseUrl}/inventory/{steamId}";
			var response = await Http.RequestAsync( url );

			if ( !response.IsSuccessStatusCode )
			{
				Log.Warning( $"[InventoryAPI] GET failed: {response.StatusCode}" );
				return null;
			}

			var json = await response.Content.ReadAsStringAsync();
			return Json.Deserialize<InventoryDto>( json );
		}
		catch ( Exception ex )
		{
			Log.Error( $"[InventoryAPI] GET error: {ex.Message}" );
			return null;
		}
	}

	public async Task<InventoryDto> SaveInventoryAsync( long steamId, SaveInventoryRequest request )
	{
		try
		{
			var url = $"{_baseUrl}/inventory/{steamId}/save";
			var json = Json.Serialize( request );
			var content = new System.Net.Http.StringContent( json, System.Text.Encoding.UTF8, "application/json" );
			var response = await Http.RequestAsync( url, "POST", content );

			if ( !response.IsSuccessStatusCode )
			{
				Log.Warning( $"[InventoryAPI] Save failed: {response.StatusCode}" );
				return null;
			}

			var responseJson = await response.Content.ReadAsStringAsync();
			return Json.Deserialize<InventoryDto>( responseJson );
		}
		catch ( Exception ex )
		{
			Log.Error( $"[InventoryAPI] Save error: {ex.Message}" );
			return null;
		}
	}
}

/// <summary>
/// Static facade — like EconomySystem but for inventories.
/// </summary>
public static class InventorySystem
{
	private static IInventoryApi _api;

	public static IInventoryApi Api
	{
		get
		{
			if ( _api == null ) _api = new InventoryApiClient();
			return _api;
		}
	}

	public static void Initialize( string apiUrl = "http://localhost:8080/api" )
	{
		_api = new InventoryApiClient( apiUrl );
	}
}
