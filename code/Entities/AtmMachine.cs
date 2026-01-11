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
	/// <summary>
	/// The model to use for the ATM
	/// </summary>
	[Property] public Model AtmModel { get; set; }

	private ModelRenderer _modelRenderer;
	private Interactable _interactable;

	protected override void OnAwake()
	{
		// Set up the model renderer
		_modelRenderer = Components.GetOrCreate<ModelRenderer>();
		if ( AtmModel != null )
		{
			_modelRenderer.Model = AtmModel;
		}

		// Set up the interactable component
		_interactable = Components.GetOrCreate<Interactable>();
		_interactable.InteractionText = "Use ATM";
		_interactable.InteractionDistance = 150f;
		_interactable.HoldDuration = 0f; // Instant interaction
		_interactable.OnInteract = OnAtmInteract;
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
		// Try to get from the local connection
		if ( Connection.Local != null && Connection.Local.IsActive )
		{
			return Connection.Local.SteamId;
		}

		// Fallback for testing
		Log.Warning( "[ATM] Using fallback Steam ID for testing" );
		return 76561198012345678;
	}
}
