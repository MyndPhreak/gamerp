using Sandbox;
using GameRP.Interactions;
using System.Collections.Generic;
using System.Linq;

namespace GameRP.Vehicles;

/// <summary>
/// Orchestrates multi-seat vehicle interaction. Place on the vehicle root alongside VehicleController.
/// Networking: Each client has authority over their own player. Mount/Dismount runs locally on the
/// authoritative client. [Sync] OccupantSteamId on VehicleSeat tells other clients who occupies each
/// seat. S&box's networking layer handles SetParent and position sync for networked objects.
/// If remote clients need additional mount/dismount effects, add [Rpc.Broadcast] methods here.
/// </summary>
public sealed class VehicleSeatManager : Component
{
	private VehicleController _controller;
	private List<VehicleSeat> _seats = new();
	private VehicleSeat _driverSeat;
	private Interactable _interactable;

	protected override void OnStart()
	{
		_controller = Components.Get<VehicleController>();
		_interactable = Components.Get<Interactable>( FindMode.InSelf );

		if ( _interactable == null )
		{
			// Try children (interaction trigger might be on a child collider)
			_interactable = Components.Get<Interactable>( FindMode.InChildren );
		}

		_seats = Components.GetAll<VehicleSeat>( FindMode.InChildren )
			.OrderBy( s => s.Priority )
			.ToList();

		_driverSeat = _seats.FirstOrDefault( s => s.Type == SeatType.Driver );

		if ( _driverSeat == null )
			Log.Warning( "[VehicleSeatManager] No driver seat found among children" );

		if ( _interactable != null )
			_interactable.OnInteract = OnVehicleInteract;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		// Check if the local player is seated and wants to exit
		if ( !Input.Pressed( "use" ) ) return;

		var localPlayer = FindLocalPlayer();
		if ( localPlayer == null ) return;

		var seat = _seats.FirstOrDefault( s => s.OccupiedBy == localPlayer );
		if ( seat != null )
			DismountPlayer( seat );
	}

	private void OnVehicleInteract()
	{
		var player = FindLocalPlayer();
		if ( player == null ) return;

		// If player is already seated in this vehicle, dismount
		var existingSeat = _seats.FirstOrDefault( s => s.OccupiedBy == player );
		if ( existingSeat != null )
		{
			DismountPlayer( existingSeat );
			return;
		}

		// Find first available seat (sorted by Priority — driver first)
		var freeSeat = _seats.FirstOrDefault( s => s.OccupiedBy == null );
		if ( freeSeat == null ) return; // vehicle full

		MountPlayer( player, freeSeat );
	}

	private void MountPlayer( RPPlayer player, VehicleSeat seat )
	{
		seat.Mount( player );

		if ( seat.Type == SeatType.Driver && _controller != null )
			_controller.Driver = player;
	}

	private void DismountPlayer( VehicleSeat seat )
	{
		var wasDriver = seat.Type == SeatType.Driver;
		seat.Dismount();

		if ( wasDriver && _controller != null )
			_controller.Driver = null;
	}

	private RPPlayer FindLocalPlayer()
	{
		foreach ( var rp in Scene.GetAllComponents<RPPlayer>() )
		{
			if ( rp.IsProxy ) continue;
			return rp;
		}
		return null;
	}
}
