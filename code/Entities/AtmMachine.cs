using Sandbox;
using GameRP.Interactions;
using GameRP.UI;
using System.Linq;

namespace GameRP.Entities; 
 
/// <summary>
/// ATM machine that players can use to deposit/withdraw money
/// </summary>
public sealed class AtmMachine : Component
{
	private Interactable _interactable;

	protected override void OnStart()
	{
		// Hook into the sibling Interactable (configured in the editor)
		_interactable = Components.Get<Interactable>();
		if ( _interactable != null )
		{
			_interactable.OnInteract = OnAtmInteract;
		}
		else
		{
			Log.Warning( "[ATM] No Interactable component found - add one in the editor" );
		}
	}

	/// <summary>
	/// Called when a player interacts with the ATM
	/// </summary>
	private void OnAtmInteract()
	{
		Log.Info( "[ATM] Player interacted with ATM" );

		// Get the player's Steam ID
		var steamId = GetPlayerSteamId();

		if ( steamId == 0 )
		{
			Log.Warning( "[ATM] Could not get player Steam ID" );
			return;
		}

		// Find the ATM screen component in the scene
		var atmScreen = Scene.GetAllComponents<AtmScreen>().FirstOrDefault();
		if ( atmScreen != null )
		{
			atmScreen.Open( steamId );
		}
		else
		{
			Log.Error( "[ATM] Could not find AtmScreen component in scene!" );
		}
	}

	/// <summary>
	/// Get the Steam ID from the local player
	/// </summary>
	private long GetPlayerSteamId()
	{
		return Game.SteamId;
	}
}
