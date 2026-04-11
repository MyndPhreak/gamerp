using Sandbox;
using Sandbox.Citizen;

namespace GameRP.Vehicles;

public enum SeatType { Driver, Passenger }

public sealed class VehicleSeat : Component
{
	[Property] public SeatType Type { get; set; } = SeatType.Passenger;
	[Property] public int Priority { get; set; } = 0;
	[Property] public Vector3 SeatOffset { get; set; } = new Vector3( 0, 0, 40 );
	[Property] public Vector3 ExitOffset { get; set; } = new Vector3( 0, 80, 0 );
	[Property] public float CameraDistance { get; set; } = 300f;
	[Property] public float MinCameraDistance { get; set; } = 80f;
	[Property] public float MaxCameraDistance { get; set; } = 800f;
	[Property] public float CameraZoomStep { get; set; } = 30f;
	[Property] public Vector3 CameraLookOffset { get; set; } = new Vector3( 0, 0, 30 );
	[Property] public float CameraSensitivity { get; set; } = 0.15f;

	public RPPlayer OccupiedBy { get; private set; }
	[Sync] public ulong OccupantSteamId { get; private set; }

	private VehicleController _vehicleController;
	private PlayerController _playerController;
	private CharacterController _characterController;
	private PlayerInteraction _playerInteraction;
	private CitizenAnimationHelper _citizenAnim;
	private CameraComponent _camera;
	private GameObject _playerObject;

	// Orbit camera state
	private Angles _orbitAngles;
	private bool _isFirstPerson = false;

	protected override void OnStart()
	{
		_vehicleController = Components.GetInAncestors<VehicleController>();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;
		if ( OccupiedBy == null ) return;

		// Keep PlayerController thinking we're stationary so its animator shows idle
		if ( _playerController != null )
			_playerController.WishVelocity = Vector3.Zero;

		// Orbit camera angles updated from mouse input
		_orbitAngles.yaw -= Input.MouseDelta.x * CameraSensitivity;
		_orbitAngles.pitch = (_orbitAngles.pitch + Input.MouseDelta.y * CameraSensitivity).Clamp( -20f, 70f );

		// First / Third person toggle
		if ( Input.Pressed( "View" ) || Input.Keyboard.Pressed( "c" ) || Input.Keyboard.Pressed( "C" ) )
		{
			_isFirstPerson = !_isFirstPerson;
			
			// Optional if PlayerController handles hiding the body locally when it sees first person offset
			// We just let it be for now, or manually hide SkinnedModelRenderer later if there are clipping issues
			// Assuming CitizenAnimationHelper handles the head bone properly based on camera proximity
		}

		// Zoom logic
		var altHeld = Input.Keyboard.Down( "Alt" );
		if ( altHeld && !_isFirstPerson )
		{
			var scroll = Input.MouseWheel.y;
			if ( scroll != 0 )
			{
				CameraDistance = (CameraDistance - scroll * CameraZoomStep).Clamp( MinCameraDistance, MaxCameraDistance );
			}
		}
	}

	protected override void OnPreRender()
	{
		if ( OccupiedBy == null ) return;

		// Lock player to seat root after all component updates have run
		if ( _playerObject != null )
		{
			_playerObject.LocalPosition = Vector3.Zero;
			_playerObject.LocalRotation = Rotation.Identity;
		}

		// Keep sitting pose pinned each frame after CitizenAnimationHelper updates
		if ( _citizenAnim != null )
		{
			_citizenAnim.IsSitting = true;
			_citizenAnim.Sitting = CitizenAnimationHelper.SittingStyle.Chair;
			_citizenAnim.SittingPose = 1f;
			_citizenAnim.SittingOffsetHeight = 0f;
			_citizenAnim.GameObject.LocalRotation = Rotation.Identity;
			_citizenAnim.WithLook( GameObject.WorldRotation.Forward, 1f, 1f, 1f );
		}

		if ( _camera == null || _vehicleController == null ) return;

		if ( _isFirstPerson && _playerObject != null )
		{
			// First person camera logic: attach to eyes or approximate head position
			var modelRenderer = _playerObject.Components.Get<SkinnedModelRenderer>( FindMode.InChildren );
			if ( modelRenderer != null )
			{
				var headTransform = modelRenderer.GetAttachment( "eyes" ) ?? modelRenderer.GetAttachment( "head" );
				if ( headTransform.HasValue )
				{
					_camera.WorldPosition = headTransform.Value.Position;
				}
				else
				{
					_camera.WorldPosition = _playerObject.WorldPosition + Vector3.Up * 64f; // Fallback
				}
			}
			else
			{
				_camera.WorldPosition = _playerObject.WorldPosition + Vector3.Up * 64f;
			}

			_camera.WorldRotation = Rotation.From( _orbitAngles );
			
			// If clipping occurs with the player's own head, you could disable its rendering here,
			// or rely on PlayerController's logic. If needed: modelRenderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
		}
		else
		{
			var lookTarget = _vehicleController.WorldPosition + CameraLookOffset;
			var orbitRot = Rotation.From( _orbitAngles );

			_camera.WorldPosition = lookTarget + orbitRot.Forward * -CameraDistance;
			_camera.WorldRotation = Rotation.LookAt( lookTarget - _camera.WorldPosition, Vector3.Up );
		}
	}

	/// <summary>
	/// Mount a player into this seat. Called by VehicleSeatManager.
	/// </summary>
	public void Mount( RPPlayer player )
	{
		OccupiedBy = player;
		OccupantSteamId = Game.SteamId;
		_playerObject = player.GameObject;

		// Cache components
		_playerController = _playerObject.Components.Get<PlayerController>();
		_characterController = _playerObject.Components.Get<CharacterController>();
		_playerInteraction = _playerObject.Components.Get<PlayerInteraction>();
		_citizenAnim = _playerObject.Components.Get<CitizenAnimationHelper>( FindMode.InChildren );
		_camera = _playerObject.Components.Get<CameraComponent>( FindMode.InChildren );

		// Disable ALL player physics BEFORE parenting to prevent collision with vehicle body
		if ( _characterController != null )
		{
			_characterController.Velocity = Vector3.Zero;
			_characterController.Enabled = false;
		}
		if ( _playerController != null )
		{
			_playerController.WishVelocity = Vector3.Zero;
			_playerController.UseInputControls = false;
			_playerController.UseLookControls = false;
			_playerController.UseAnimatorControls = false;
			_playerController.HideBodyInFirstPerson = false;
		}
		if ( _playerInteraction != null ) _playerInteraction.Enabled = false;

		// Disable all colliders and rigidbodies on the player
		foreach ( var col in _playerObject.Components.GetAll<Collider>( FindMode.EverythingInDescendants ) )
			col.Enabled = false;
		foreach ( var rb in _playerObject.Components.GetAll<Rigidbody>( FindMode.EverythingInDescendants ) )
			rb.Enabled = false;

		// Teleport player to seat root BEFORE parenting (avoids one-frame overlap)
		_playerObject.WorldPosition = GameObject.WorldPosition;
		_playerObject.WorldRotation = GameObject.WorldRotation;

		// Now parent safely
		_playerObject.SetParent( GameObject );
		_playerObject.LocalPosition = Vector3.Zero;
		_playerObject.LocalRotation = Rotation.Identity;

		// Apply sitting pose via CitizenAnimationHelper
		if ( _citizenAnim != null )
		{
			_citizenAnim.IsSitting = true;
			_citizenAnim.Sitting = CitizenAnimationHelper.SittingStyle.Chair;
			_citizenAnim.SittingPose = 1f;
			_citizenAnim.SittingOffsetHeight = 0f;
			_citizenAnim.GameObject.LocalRotation = Rotation.Identity;

			var handL = FindNamedChild( _vehicleController?.GameObject ?? GameObject, "HandPosL" );
			if ( handL != null ) _citizenAnim.IkLeftHand = handL;

			var handR = FindNamedChild( _vehicleController?.GameObject ?? GameObject, "HandPosR" );
			if ( handR != null ) _citizenAnim.IkRightHand = handR;
		}

		// Start orbit from behind the vehicle
		if ( _vehicleController != null )
		{
			var vehicleYaw = _vehicleController.WorldRotation.Angles().yaw;
			_orbitAngles = new Angles( 20f, vehicleYaw + 180f, 0f );
		}
	}

	/// <summary>
	/// Dismount the player from this seat. Called by VehicleSeatManager.
	/// </summary>
	public void Dismount()
	{
		if ( OccupiedBy == null ) return;

		_playerObject.SetParent( null );

		var exitWorldPos = WorldPosition + WorldRotation * ExitOffset;
		_playerObject.WorldPosition = exitWorldPos;

		// Clear sitting pose
		if ( _citizenAnim != null )
		{
			_citizenAnim.IsSitting = false;
			_citizenAnim.Sitting = CitizenAnimationHelper.SittingStyle.None;
			_citizenAnim.SittingPose = 0f;
			_citizenAnim.IkLeftHand = null;
			_citizenAnim.IkRightHand = null;
		}

		// Re-enable player physics and colliders
		if ( _characterController != null ) _characterController.Enabled = true;
		foreach ( var col in _playerObject.Components.GetAll<Collider>( FindMode.EverythingInDescendants ) )
			col.Enabled = true;
		foreach ( var rb in _playerObject.Components.GetAll<Rigidbody>( FindMode.EverythingInDescendants ) )
			rb.Enabled = true;
		if ( _playerController != null )
		{
			_playerController.UseInputControls = true;
			_playerController.UseLookControls = true;
			_playerController.UseAnimatorControls = true;
			_playerController.HideBodyInFirstPerson = true;
		}

		_isFirstPerson = false; // Reset view state when dismounting

		if ( _playerInteraction != null ) _playerInteraction.Enabled = true;

		OccupiedBy = null;
		OccupantSteamId = 0;
		_playerObject = null;
		_playerController = null;
		_characterController = null;
		_playerInteraction = null;
		_citizenAnim = null;
		_camera = null;
	}

	protected override void OnDestroy()
	{
		if ( OccupiedBy != null )
			Dismount();
	}

	private GameObject FindNamedChild( GameObject root, string name )
	{
		if ( root == null ) return null;
		if ( root.Name == name ) return root;
		foreach ( var child in root.Children )
		{
			var found = FindNamedChild( child, name );
			if ( found != null ) return found;
		}
		return null;
	}
}
