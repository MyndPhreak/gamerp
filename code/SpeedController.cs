using Sandbox;

/// <summary>
/// Scroll wheel speed control (Star Citizen style).
/// Adjusts the PlayerController's walk/run speed between min and max.
/// Add as a sibling component to PlayerController.
/// </summary>
public sealed class SpeedController : Component
{
	[Property] public float MinSpeed { get; set; } = 50f;
	[Property] public float MaxSpeed { get; set; } = 320f;
	[Property] public float ScrollStep { get; set; } = 20f;
	[Property, ReadOnly] public float CurrentSpeed { get; set; } = 110f;

	private PlayerController _playerController;

	protected override void OnAwake()
	{
		_playerController = Components.Get<PlayerController>();
		if ( _playerController != null )
		{
			CurrentSpeed = _playerController.WalkSpeed;
		}
	}

	protected override void OnUpdate()
	{
		if ( IsProxy || _playerController == null ) return;
		if ( Input.Keyboard.Down( "Alt" ) ) return;

		var scroll = Input.MouseWheel.y;
		if ( scroll == 0 ) return;

		CurrentSpeed = (CurrentSpeed + scroll * ScrollStep).Clamp( MinSpeed, MaxSpeed );

		_playerController.WalkSpeed = CurrentSpeed;

		// If scrolled above the run threshold, also adjust run speed
		if ( CurrentSpeed > _playerController.RunSpeed )
		{
			_playerController.RunSpeed = CurrentSpeed;
		}
	}
}
