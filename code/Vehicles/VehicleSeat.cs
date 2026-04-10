using Sandbox;
using GameRP.Interactions;

namespace GameRP.Vehicles;

public sealed class VehicleSeat : Component
{
	[Property] public Vector3 SeatOffset { get; set; } = new Vector3( 0, 0, 40 );
	[Property] public Vector3 ExitOffset { get; set; } = new Vector3( 0, 80, 0 );

	public RPPlayer OccupiedBy { get; private set; }

	private Interactable _interactable;
	private VehicleController _vehicleController;
	private PlayerController _playerController;
	private CharacterController _characterController;
	private SkinnedModelRenderer _playerModel;
	private GameObject _playerObject;

	protected override void OnStart()
	{
		_interactable = Components.Get<Interactable>();
		_vehicleController = Components.GetInAncestorsOrSelf<VehicleController>();

		if ( _interactable == null )
		{
			Log.Warning( "[VehicleSeat] No Interactable component found on this GameObject" );
			return;
		}

		if ( _vehicleController == null )
		{
			Log.Warning( "[VehicleSeat] No VehicleController found on this or parent GameObjects" );
			return;
		}

		_interactable.OnInteract = OnSeatInteract;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;
		if ( OccupiedBy == null ) return;

		// Check for exit input
		if ( Input.Pressed( "use" ) )
		{
			Dismount();
			return;
		}

		// Keep player positioned at seat
		if ( _playerObject != null )
		{
			_playerObject.LocalPosition = SeatOffset;
			_playerObject.LocalRotation = Rotation.Identity;
		}
	}

	private void OnSeatInteract()
	{
		if ( OccupiedBy != null ) return;
		if ( _vehicleController.Driver != null ) return;

		// Find local player
		var player = FindLocalPlayer();
		if ( player == null ) return;

		Mount( player );
	}

	private RPPlayer FindLocalPlayer()
	{
		foreach ( var pc in Scene.GetAllComponents<PlayerController>() )
		{
			if ( pc.IsProxy ) continue;
			var rp = pc.Components.Get<RPPlayer>();
			if ( rp != null ) return rp;
		}
		return null;
	}

	private void Mount( RPPlayer player )
	{
		OccupiedBy = player;
		_playerObject = player.GameObject;

		// Cache player components
		_playerController = _playerObject.Components.Get<PlayerController>();
		_characterController = _playerObject.Components.Get<CharacterController>();
		_playerModel = _playerObject.Components.GetInChildren<SkinnedModelRenderer>();

		// Disable player movement and look
		_playerController.WishVelocity = Vector3.Zero;
		_playerController.UseInputControls = false;
		_playerController.UseLookControls = false;
		_characterController.Velocity = Vector3.Zero;
		_characterController.Enabled = false;

		// Parent player to vehicle
		_playerObject.SetParent( GameObject );
		_playerObject.LocalPosition = SeatOffset;
		_playerObject.LocalRotation = Rotation.Identity;

		// Hide player model
		if ( _playerModel != null )
			_playerModel.Enabled = false;

		// Tell controller we have a driver
		_vehicleController.Driver = player;

		// Hide interaction prompt while occupied
		_interactable.Enabled = false;

		Log.Info( "[VehicleSeat] Player mounted vehicle" );
	}

	private void Dismount()
	{
		if ( OccupiedBy == null ) return;

		// Unparent player
		_playerObject.SetParent( null );

		// Position player at exit point (world space, relative to vehicle)
		var exitWorldPos = WorldPosition + WorldRotation * ExitOffset;
		_playerObject.WorldPosition = exitWorldPos;

		// Re-enable player movement and look
		_characterController.Enabled = true;
		_playerController.UseInputControls = true;
		_playerController.UseLookControls = true;

		// Show player model
		if ( _playerModel != null )
			_playerModel.Enabled = true;

		// Clear driver
		_vehicleController.Driver = null;

		Log.Info( "[VehicleSeat] Player dismounted vehicle" );

		// Clean up references
		OccupiedBy = null;
		_playerObject = null;
		_playerController = null;
		_characterController = null;
		_playerModel = null;

		// Re-enable interaction prompt
		_interactable.Enabled = true;
	}

	protected override void OnDestroy()
	{
		if ( OccupiedBy != null )
			Dismount();
	}
}
