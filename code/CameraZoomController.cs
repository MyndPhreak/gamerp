using Sandbox;

/// <summary>
/// Alt + Scroll Wheel to zoom the third person camera in/out.
/// Adjusts PlayerController.CameraOffset distance.
/// Add as a sibling component to PlayerController.
/// </summary>
public sealed class CameraZoomController : Component
{
	[Property] public float MinDistance { get; set; } = 80f;
	[Property] public float MaxDistance { get; set; } = 600f;
	[Property] public float ZoomStep { get; set; } = 30f;
	[Property, ReadOnly] public float CurrentDistance { get; set; } = 256f;

	private PlayerController _playerController;

	protected override void OnAwake()
	{
		_playerController = Components.Get<PlayerController>();
		if ( _playerController != null )
		{
			CurrentDistance = _playerController.CameraOffset.x;
		}
	}

	protected override void OnUpdate()
	{
		if ( IsProxy || _playerController == null ) return;

		var altHeld = Input.Keyboard.Down( "Alt" );

		if ( !altHeld ) return;

		var scroll = Input.MouseWheel.y;
		if ( scroll == 0 ) return;

		CurrentDistance = (CurrentDistance - scroll * ZoomStep).Clamp( MinDistance, MaxDistance );

		var offset = _playerController.CameraOffset;
		_playerController.CameraOffset = new Vector3( CurrentDistance, offset.y, offset.z );
	}
}
