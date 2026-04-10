using Sandbox;
using GameRP.Interactions;

namespace GameRP.Vehicles;

public sealed class VehicleSeat : Component
{
	[Property] public Vector3 SeatOffset { get; set; } = new Vector3( 0, 0, 40 );
	[Property] public Vector3 ExitOffset { get; set; } = new Vector3( 0, 80, 0 );
	[Property] public float CameraDistance { get; set; } = 300f;
	[Property] public Vector3 CameraLookOffset { get; set; } = new Vector3( 0, 0, 30 );
	[Property] public float CameraSensitivity { get; set; } = 0.15f;

	public RPPlayer OccupiedBy { get; private set; }

	private Interactable _interactable;
	private VehicleController _vehicleController;
	private PlayerController _playerController;
	private CharacterController _characterController;
	private PlayerInteraction _playerInteraction;
	private CameraComponent _camera;
	private GameObject _playerObject;

	// Orbit camera state
	private Angles _orbitAngles;

	protected override void OnStart()
	{
		_interactable = Components.Get<Interactable>();
		_vehicleController = Components.Get<VehicleController>( FindMode.InAncestors | FindMode.InSelf );

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

		// Keep player at seat position
		if ( _playerObject != null )
		{
			_playerObject.LocalPosition = SeatOffset;
			_playerObject.LocalRotation = Rotation.Identity;
		}

		// Orbit camera angles updated from mouse input (inverted)
		_orbitAngles.yaw -= Input.MouseDelta.x * CameraSensitivity;
		_orbitAngles.pitch = (_orbitAngles.pitch + Input.MouseDelta.y * CameraSensitivity).Clamp( -20f, 70f );
	}

	protected override void OnPreRender()
	{
		// Position camera after all transforms settle — same pattern as Closet.cs
		if ( OccupiedBy == null || _camera == null ) return;

		var lookTarget = _vehicleController.WorldPosition + CameraLookOffset;
		var orbitRot = Rotation.From( _orbitAngles );

		_camera.WorldPosition = lookTarget + orbitRot.Forward * -CameraDistance;
		_camera.WorldRotation = Rotation.LookAt( lookTarget - _camera.WorldPosition, Vector3.Up );
	}

	private void OnSeatInteract()
	{
		if ( OccupiedBy != null ) return;
		if ( _vehicleController.Driver != null ) return;

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

		// Cache components
		_playerController = _playerObject.Components.Get<PlayerController>();
		_characterController = _playerObject.Components.Get<CharacterController>();
		_playerInteraction = _playerObject.Components.Get<PlayerInteraction>();
		_camera = _playerObject.Components.Get<CameraComponent>( FindMode.InChildren );

		// Disable player movement and the foot-camera controller
		_playerController.WishVelocity = Vector3.Zero;
		_playerController.UseInputControls = false;
		_playerController.UseLookControls = false;
		_playerController.HideBodyInFirstPerson = false; // keep model visible in vehicle
		_characterController.Velocity = Vector3.Zero;
		_characterController.Enabled = false;
		if ( _playerInteraction != null ) _playerInteraction.Enabled = false;

		// Start orbit from behind the vehicle
		var vehicleYaw = _vehicleController.WorldRotation.Angles().yaw;
		_orbitAngles = new Angles( 20f, vehicleYaw + 180f, 0f );

		// Parent player to vehicle seat
		_playerObject.SetParent( GameObject );
		_playerObject.LocalPosition = SeatOffset;
		_playerObject.LocalRotation = Rotation.Identity;

		_vehicleController.Driver = player;
		_interactable.Enabled = false;
	}

	private void Dismount()
	{
		if ( OccupiedBy == null ) return;

		_playerObject.SetParent( null );

		var exitWorldPos = WorldPosition + WorldRotation * ExitOffset;
		_playerObject.WorldPosition = exitWorldPos;

		_characterController.Enabled = true;
		_playerController.UseInputControls = true;
		_playerController.UseLookControls = true;
		_playerController.HideBodyInFirstPerson = true;
		if ( _playerInteraction != null ) _playerInteraction.Enabled = true;

		_vehicleController.Driver = null;

		OccupiedBy = null;
		_playerObject = null;
		_playerController = null;
		_characterController = null;
		_playerInteraction = null;
		_camera = null;

		_interactable.Enabled = true;
	}

	protected override void OnDestroy()
	{
		if ( OccupiedBy != null )
			Dismount();
	}
}
