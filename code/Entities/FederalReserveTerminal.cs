using Sandbox;
using GameRP.Interactions;
using GameRP.UI;
using System.Linq;

namespace GameRP.Entities;

/// <summary>
/// Federal Reserve terminal that players can interact with to view the economy dashboard.
/// Place in the scene with an Interactable component configured in the editor.
/// </summary>
public sealed class FederalReserveTerminal : Component
{
	private Interactable _interactable;

	protected override void OnStart()
	{
		_interactable = Components.Get<Interactable>();
		if ( _interactable != null )
		{
			_interactable.OnInteract = OnTerminalInteract;
		}
		else
		{
			Log.Warning( "[FedTerminal] No Interactable component found - add one in the editor" );
		}
	}

	private void OnTerminalInteract()
	{
		Log.Info( "[FedTerminal] Player interacted with Federal Reserve terminal" );

		var screen = Scene.GetAllComponents<FedReserveScreen>().FirstOrDefault();
		if ( screen != null )
		{
			screen.Open();
		}
		else
		{
			Log.Error( "[FedTerminal] Could not find FedReserveScreen component in scene!" );
		}
	}
}
