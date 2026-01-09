using Sandbox;
using GameRP.Interactions;

/// <summary>
/// Simple component that adds interaction and camera functionality to a player.
/// Works with any existing CharacterController - doesn't interfere with movement.
/// </summary>
public sealed class PlayerInteraction : Component
{
	[Property] public CameraComponent Camera { get; set; }
	[Property] public float MouseSensitivity { get; set; } = 0.1f;

	private Angles _eyeAngles;
	private InteractionManager _interactionManager;

	protected override void OnAwake()
	{
		// Find or create camera
		if ( Camera == null )
		{
			Camera = Components.GetInDescendants<CameraComponent>();
		}

		if ( Camera == null )
		{
			Log.Warning( "[PlayerInteraction] No camera found!" );
			return;
		}

		// Add interaction manager
		_interactionManager = Components.GetOrCreate<InteractionManager>();
		_interactionManager.Camera = Camera;
		_interactionManager.ShowDebug = false;
	}

	protected override void OnStart()
	{
		if ( !IsProxy )
		{
			Mouse.Visibility = MouseVisibility.Hidden;
		}
	}

	protected override void OnUpdate()
	{
		if ( IsProxy || Camera == null ) return;

		// Handle mouse look
		if ( Mouse.Visibility == MouseVisibility.Hidden )
		{
			_eyeAngles.pitch += Input.MouseDelta.y * MouseSensitivity;
			_eyeAngles.yaw -= Input.MouseDelta.x * MouseSensitivity;
			_eyeAngles.pitch = _eyeAngles.pitch.Clamp( -89, 89 );
		}

		// Update camera rotation
		Camera.WorldRotation = Rotation.From( _eyeAngles.pitch, _eyeAngles.yaw, 0 );

		// Toggle mouse for UI (F1 key)
		if ( Input.Pressed( "Menu" ) )
		{
			Mouse.Visibility = Mouse.Visibility == MouseVisibility.Hidden
				? MouseVisibility.Visible
				: MouseVisibility.Hidden;
		}
	}
}
