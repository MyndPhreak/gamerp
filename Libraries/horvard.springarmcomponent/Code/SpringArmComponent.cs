using Sandbox;
using System;

/// <summary>
/// Spring Arm Component — highly configurable third-person camera boom.
/// 
/// Provides smooth camera following with collision detection, lag, and full rotation control.
/// Perfect for third-person games, top-down views, side-scrollers, and more.
/// 
/// Usage:
/// 1. Add this component as a child of your character/pawn
/// 2. Add a Camera component as a child of this Spring Arm
/// 3. Select a preset or configure manually
/// 4. Optionally enable UseControlRotation and set ControlRotationSource from your character controller
/// </summary>
[Icon( "videocam" )]
[Title( "Spring Arm" )]
[Category( "Camera" )]
public sealed class SpringArmComponent : Component
{
	// ====================== Presets ======================

	/// <summary>
	/// Quick configuration presets for common camera types.
	/// </summary>
	public enum SpringArmPreset
	{
		/// <summary>Manual configuration - no preset applied</summary>
		Custom,

		/// <summary>Standard third-person over-the-shoulder camera with collision</summary>
		ThirdPerson,

		/// <summary>First-person view positioned at eye level</summary>
		FirstPerson,

		/// <summary>Top-down isometric view looking down at the character</summary>
		TopDown,

		/// <summary>Side-scrolling 2.5D platformer camera</summary>
		SideScroller
	}

	/// <summary>
	/// Select a preset to quickly configure the spring arm for common camera types.
	/// Changing this will override your current settings with preset values.
	/// Set to Custom to prevent automatic configuration changes.
	/// </summary>
	[Property, Group( "Preset" )]
	public SpringArmPreset Preset
	{
		get => _preset;
		set
		{
			if ( _preset != value )
			{
				_preset = value;
				ApplyPreset( value );
			}
		}
	}
	private SpringArmPreset _preset = SpringArmPreset.Custom;

	// ====================== Dynamic Arm Settings ======================

	/// <summary>
	/// Natural length of the camera arm in world units.
	/// This is how far the camera will be from the parent when nothing is blocking it.
	/// 
	/// Typical values:
	/// - First Person: 0-50
	/// - Third Person: 300-500
	/// - Top Down: 600-1000
	/// </summary>
	[Property, Group( "Dynamic Arm" ), Range( 0f, 2000f ), Step( 10f )]
	public float TargetArmLength { get; set; } = 400f;

	/// <summary>
	/// Minimum allowed arm length when collision occurs.
	/// Prevents the camera from getting too close to the character.
	/// Set to 0 for first-person or to allow full collision compression.
	/// 
	/// Should be less than TargetArmLength.
	/// </summary>
	[Property, Group( "Dynamic Arm" ), Range( 0f, 500f ), Step( 5f )]
	public float MinArmLength { get; set; } = 50f;

	/// <summary>
	/// Enable sphere collision testing to prevent camera clipping through walls and objects.
	/// When enabled, the arm will automatically shorten when obstacles are detected.
	/// 
	/// Disable for performance in controlled environments or for fixed camera angles.
	/// </summary>
	[Property, Group( "Dynamic Arm" )]
	public bool DoCollisionTest { get; set; } = true;

	/// <summary>
	/// Radius of the collision detection sphere in world units.
	/// Should match the approximate size of the camera/head to prevent clipping.
	/// 
	/// Larger values = more aggressive collision avoidance but may feel restrictive.
	/// Smaller values = tighter to walls but more risk of clipping.
	/// 
	/// Typical values: 12-20
	/// </summary>
	[Property, Group( "Dynamic Arm" ), Range( 1f, 50f ), Step( 1f )]
	public float ProbeSize { get; set; } = 16f;

	/// <summary>
	/// Additional safety margin pulled back from collision hit point.
	/// Adds extra padding to prevent the camera from touching walls exactly.
	/// 
	/// Higher values = safer but camera pulls back more from walls.
	/// Lower values = tighter to walls but may cause occasional clipping.
	/// 
	/// Typical values: 5-20
	/// </summary>
	[Property, Group( "Dynamic Arm" ), Range( 0f, 50f ), Step( 1f )]
	public float CollisionMargin { get; set; } = 10f;

	/// <summary>
	/// World-space offset from parent's pivot point to the start of the spring arm.
	/// Use this to position the arm origin at character's head/shoulder height.
	/// 
	/// Common usage: Set Z to character height (e.g., 60-80 for human height)
	/// 
	/// This offset is NOT affected by parent rotation - it's always in world space.
	/// </summary>
	[Property, Group( "Dynamic Arm" )]
	public Vector3 TargetOffset { get; set; } = new Vector3( 0, 0, 60f );

	/// <summary>
	/// Local-space offset applied at the END of the spring arm (where camera sits).
	/// Use this for fine-tuning camera position relative to the arm endpoint.
	/// 
	/// Useful for:
	/// - Raising camera slightly above arm endpoint (positive Z)
	/// - Over-the-shoulder offset (positive/negative Y)
	/// - Forward/backward adjustment (X)
	/// 
	/// This offset rotates with the spring arm.
	/// Keep Z values low to avoid ceiling collision issues.
	/// </summary>
	[Property, Group( "Dynamic Arm" )]
	public Vector3 SocketOffset { get; set; } = new Vector3( 0, 0, 10f );

	// ====================== Lag Settings ======================

	/// <summary>
	/// Enable smooth position interpolation lag.
	/// Creates a soft, floating camera feel as it catches up to its target position.
	/// 
	/// Recommended: TRUE for most games (gives natural camera motion)
	/// Disable for instant, locked camera positioning.
	/// </summary>
	[Property, Group( "Lag" )]
	public bool EnablePositionLag { get; set; } = true;

	/// <summary>
	/// How quickly the camera catches up to its target position.
	/// Uses exponential smoothing for frame-rate independent motion.
	/// 
	/// Higher values = faster catch-up (more responsive, less lag)
	/// Lower values = slower catch-up (more cinematic float)
	/// 
	/// Typical values:
	/// - Responsive: 15-20
	/// - Balanced: 8-12
	/// - Floaty/Cinematic: 3-6
	/// </summary>
	[Property, Group( "Lag" ), Range( 0.1f, 30f ), Step( 0.5f )]
	public float PositionLagSpeed { get; set; } = 10f;

	/// <summary>
	/// Maximum distance the camera can lag behind its target position.
	/// Prevents excessive separation during fast movement or teleportation.
	/// 
	/// Set to 0 for unlimited lag distance.
	/// Recommended: 150-300 for most games
	/// 
	/// When the lag exceeds this distance, camera is pulled to stay within range.
	/// </summary>
	[Property, Group( "Lag" ), Range( 0f, 1000f ), Step( 10f )]
	public float MaxPositionLagDistance { get; set; } = 200f;

	/// <summary>
	/// Enable smooth rotation interpolation lag.
	/// Creates natural camera rotation motion instead of instant snapping.
	/// 
	/// Recommended: TRUE for third-person, FALSE for first-person
	/// </summary>
	[Property, Group( "Lag" )]
	public bool EnableRotationLag { get; set; } = true;

	/// <summary>
	/// How quickly camera rotation catches up to target rotation.
	/// Uses exponential smoothing for frame-rate independent motion.
	/// 
	/// Higher values = snappier rotation
	/// Lower values = smoother, more gradual rotation
	/// 
	/// Typical values:
	/// - First Person: 0 (disabled) or 20+ (instant)
	/// - Third Person: 8-15
	/// - Cinematic: 3-6
	/// </summary>
	[Property, Group( "Lag" ), Range( 0.1f, 30f ), Step( 0.5f )]
	public float RotationLagSpeed { get; set; } = 10f;

	/// <summary>
	/// Enable separate lag for tracking parent's MOVEMENT (not rotation).
	/// Creates additional smoothing when the character moves/teleports.
	/// 
	/// Independent from position lag - affects arm origin point, not endpoint.
	/// Useful for:
	/// - Smoothing fast character movement
	/// - Reducing camera shake during sprinting/jumping
	/// - Creating cinematic chase camera effect
	/// 
	/// Can be combined with position lag for double-smoothing effect.
	/// </summary>
	[Property, Group( "Lag" )]
	public bool EnableCameraLag { get; set; } = false;

	/// <summary>
	/// How quickly the arm origin follows the parent's position changes.
	/// Similar to PositionLagSpeed but applies to parent tracking specifically.
	/// 
	/// Typical values: 5-10 (slower than position lag for smoother motion)
	/// </summary>
	[Property, Group( "Lag" ), Range( 0.1f, 30f ), Step( 0.5f )]
	public float CameraLagSpeed { get; set; } = 8f;

	/// <summary>
	/// Maximum distance the arm origin can lag behind the parent.
	/// Prevents camera from getting left too far behind during fast movement.
	/// 
	/// Typical values: 100-200
	/// </summary>
	[Property, Group( "Lag" ), Range( 0f, 1000f ), Step( 10f )]
	public float MaxCameraLagDistance { get; set; } = 150f;

	// ====================== Rotation Settings ======================

	/// <summary>
	/// Use custom rotation source instead of parent's WorldRotation.
	/// When enabled, you must set ControlRotationSource from your character controller code.
	/// 
	/// Use cases:
	/// - Character model faces different direction than camera looks
	/// - Custom camera control systems
	/// - Decoupled character and camera rotation
	/// 
	/// Example usage in your controller:
	///   springArm.ControlRotationSource = eyeAngles.ToRotation();
	/// </summary>
	[Property, Group( "Rotation" )]
	public bool UseControlRotation { get; set; } = false;

	/// <summary>
	/// Custom rotation source when UseControlRotation is enabled.
	/// Set this property from your character controller code each frame.
	/// 
	/// This allows camera rotation to be independent of character model rotation.
	/// Ignored when UseControlRotation is false.
	/// </summary>
	public Rotation ControlRotationSource { get; set; }

	/// <summary>
	/// Inherit pitch (up/down rotation) from parent or control rotation.
	/// 
	/// TRUE: Camera tilts up/down with parent rotation
	/// FALSE: Camera pitch stays at 0 (looks straight ahead horizontally)
	/// 
	/// Typically TRUE for first/third person, FALSE for top-down/side-scroller.
	/// </summary>
	[Property, Group( "Rotation" )]
	public bool InheritPitch { get; set; } = true;

	/// <summary>
	/// Inherit yaw (left/right rotation) from parent or control rotation.
	/// 
	/// TRUE: Camera rotates left/right with parent
	/// FALSE: Camera yaw stays fixed (doesn't rotate horizontally)
	/// 
	/// Typically TRUE for first/third person, FALSE for fixed-angle cameras.
	/// </summary>
	[Property, Group( "Rotation" )]
	public bool InheritYaw { get; set; } = true;

	/// <summary>
	/// Inherit roll (tilt/banking rotation) from parent or control rotation.
	/// 
	/// TRUE: Camera tilts/rolls with parent (like in flight games)
	/// FALSE: Camera stays upright (horizon stays level)
	/// 
	/// Usually FALSE for most games to prevent disorienting camera tilt.
	/// Set TRUE for vehicles, aircraft, or stylized camera effects.
	/// </summary>
	[Property, Group( "Rotation" )]
	public bool InheritRoll { get; set; } = false;

	/// <summary>
	/// Minimum pitch angle in degrees (how far down you can look).
	/// Clamps camera pitch to prevent looking too far down.
	/// 
	/// Negative values = looking down
	/// Typical range: -80 to -60
	/// 
	/// Set to -90 for full downward rotation.
	/// </summary>
	[Property, Group( "Rotation" ), Range( -90f, 0f ), Step( 1f )]
	public float MinPitch { get; set; } = -80f;

	/// <summary>
	/// Maximum pitch angle in degrees (how far up you can look).
	/// Clamps camera pitch to prevent looking too far up.
	/// 
	/// Positive values = looking up
	/// Typical range: 60 to 80
	/// 
	/// Set to 90 for full upward rotation.
	/// </summary>
	[Property, Group( "Rotation" ), Range( 0f, 90f ), Step( 1f )]
	public float MaxPitch { get; set; } = 80f;

	/// <summary>
	/// Additional rotation offset applied to the spring arm (in degrees).
	/// Adds a fixed angle offset on top of inherited/control rotation.
	/// 
	/// Useful for:
	/// - Over-the-shoulder cameras (Y offset: 10-20°)
	/// - Angled top-down views (X/pitch offset: -45° to -70°)
	/// - Side-scroller cameras (Y/yaw offset: 90° or -90°)
	/// - Looking slightly up/down by default
	/// 
	/// Applied AFTER rotation inheritance and BEFORE pitch clamping.
	/// </summary>
	[Property, Group( "Rotation" )]
	public Angles CameraRotationOffset { get; set; } = Angles.Zero;

	// ====================== Debug ======================

	/// <summary>
	/// Draw visual debug information in the editor/game.
	/// Shows spring arm configuration, collision detection, and camera positioning.
	/// 
	/// Debug visualization:
	/// 🟡 Yellow line - Spring arm from origin to endpoint
	/// 🔴 Red sphere - Collision detection probe
	/// 🔵 Cyan line - Socket offset from arm end to final camera position
	/// 🟢 Green sphere - Arm origin point (parent + TargetOffset)
	/// 🔵 Blue sphere - Final camera position
	/// 🟣 Magenta arrow - Camera look direction
	/// 
	/// Enable this when setting up or troubleshooting the spring arm.
	/// </summary>
	[Property, Group( "Debug" )]
	public bool DrawDebug { get; set; } = false;

	// Internal state
	private Rotation _currentRotation;
	private Vector3 _currentPosition;
	private Vector3 _previousParentPosition;
	private bool _isInitialized = false;

	protected override void OnEnabled()
	{
		InitializeState();
	}

	protected override void OnUpdate()
	{
		if ( GameObject.Parent is not GameObject parent || !Scene.IsValid ) return;

		// Initialize on first frame if not already done
		if ( !_isInitialized )
		{
			InitializeState();
			_previousParentPosition = parent.WorldPosition;
		}

		float delta = Time.Delta;
		if ( delta <= 0f ) return;

		// Get control rotation (either from custom source or parent)
		Rotation controlRotation = GetControlRotation( parent );

		// Calculate target rotation with inheritance and pitch clamp
		Angles controlAngles = controlRotation.Angles();
		Angles targetAngles = new Angles(
			InheritPitch ? controlAngles.pitch : 0f,
			InheritYaw ? controlAngles.yaw : 0f,
			InheritRoll ? controlAngles.roll : 0f
		);

		// Apply rotation offset
		targetAngles += CameraRotationOffset;

		// Clamp pitch to prevent extreme angles
		targetAngles.pitch = targetAngles.pitch.Clamp( MinPitch, MaxPitch );
		Rotation targetRotation = targetAngles.ToRotation();

		// Apply rotation lag
		if ( EnableRotationLag && RotationLagSpeed > 0f )
		{
			float alpha = 1f - MathF.Exp( -RotationLagSpeed * delta );
			_currentRotation = Rotation.Slerp( _currentRotation, targetRotation, alpha );
		}
		else
		{
			_currentRotation = targetRotation;
		}

		// Calculate arm origin with optional camera lag
		Vector3 targetParentPosition = parent.WorldPosition;
		Vector3 laggedParentPosition = targetParentPosition;

		if ( EnableCameraLag && CameraLagSpeed > 0f )
		{
			float lagAlpha = 1f - MathF.Exp( -CameraLagSpeed * delta );
			laggedParentPosition = Vector3.Lerp( _previousParentPosition, targetParentPosition, lagAlpha );

			// Clamp camera lag distance
			if ( MaxCameraLagDistance > 0f )
			{
				Vector3 lagVector = laggedParentPosition - targetParentPosition;
				float lagDistance = lagVector.Length;

				if ( lagDistance > MaxCameraLagDistance )
				{
					laggedParentPosition = targetParentPosition + lagVector.Normal * MaxCameraLagDistance;
				}
			}

			_previousParentPosition = laggedParentPosition;
		}
		else
		{
			_previousParentPosition = targetParentPosition;
		}

		Vector3 armOrigin = laggedParentPosition + TargetOffset;

		// Calculate ideal arm end position (before collision)
		Vector3 idealArmEnd = armOrigin + _currentRotation.Forward * -TargetArmLength;

		// Perform collision test
		float usedArmLength = TargetArmLength;
		if ( DoCollisionTest && ProbeSize > 0f )
		{
			var trace = Scene.Trace.Sphere( ProbeSize, armOrigin, idealArmEnd )
				.IgnoreGameObject( GameObject )
				.IgnoreGameObject( parent )
				.WithoutTags( "trigger", "ragdoll", "player" )
				.Run();

			if ( trace.Hit )
			{
				// Calculate shortened arm length with margin
				float hitDistance = trace.Distance;
				usedArmLength = MathF.Max( hitDistance - ProbeSize - CollisionMargin, MinArmLength );
			}
		}

		// Calculate actual arm end position with collision
		Vector3 armEnd = armOrigin + _currentRotation.Forward * -usedArmLength;

		// Apply socket offset in local space of the arm
		Vector3 targetPosition = armEnd + _currentRotation * SocketOffset;

		// Apply position lag
		if ( EnablePositionLag && PositionLagSpeed > 0f )
		{
			float alpha = 1f - MathF.Exp( -PositionLagSpeed * delta );
			_currentPosition = Vector3.Lerp( _currentPosition, targetPosition, alpha );

			// Clamp lag distance if needed
			if ( MaxPositionLagDistance > 0f )
			{
				Vector3 lagVector = _currentPosition - targetPosition;
				float lagDistance = lagVector.Length;

				if ( lagDistance > MaxPositionLagDistance )
				{
					_currentPosition = targetPosition + lagVector.Normal * MaxPositionLagDistance;
				}
			}
		}
		else
		{
			_currentPosition = targetPosition;
		}

		// Apply final transform
		WorldPosition = _currentPosition;
		WorldRotation = _currentRotation;

		// Draw debug visualization
		if ( DrawDebug )
		{
			DrawDebugInfo( armOrigin, armEnd, targetPosition );
		}
	}

	/// <summary>
	/// Gets the rotation to use for the spring arm.
	/// Returns ControlRotationSource if UseControlRotation is enabled, otherwise parent's rotation.
	/// </summary>
	private Rotation GetControlRotation( GameObject parent )
	{
		if ( UseControlRotation )
		{
			return ControlRotationSource;
		}

		return parent.WorldRotation;
	}

	/// <summary>
	/// Draws debug visualization showing spring arm configuration and state.
	/// </summary>
	private void DrawDebugInfo( Vector3 armOrigin, Vector3 armEnd, Vector3 targetPosition )
	{
		// Draw arm line from origin to end
		Gizmo.Draw.Color = Color.Yellow;
		Gizmo.Draw.Line( armOrigin, armEnd );

		// Draw collision sphere at end
		Gizmo.Draw.Color = Color.Red.WithAlpha( 0.3f );
		Gizmo.Draw.LineSphere( armEnd, ProbeSize );

		// Draw socket offset line
		if ( SocketOffset.Length > 0.1f )
		{
			Gizmo.Draw.Color = Color.Cyan;
			Gizmo.Draw.Line( armEnd, targetPosition );
		}

		// Draw target origin point
		Gizmo.Draw.Color = Color.Green;
		Gizmo.Draw.LineSphere( armOrigin, 5f );

		// Draw final position
		Gizmo.Draw.Color = Color.Blue;
		Gizmo.Draw.LineSphere( targetPosition, 8f );

		// Draw rotation direction
		Gizmo.Draw.Color = Color.Magenta;
		Gizmo.Draw.Arrow( targetPosition, targetPosition + WorldRotation.Forward * 50f, 5f );
	}

	/// <summary>
	/// Initialize internal state to current calculated position.
	/// Called automatically on enable and when snapping.
	/// </summary>
	private void InitializeState()
	{
		if ( GameObject.Parent is not GameObject parent || !Scene.IsValid )
		{
			_isInitialized = false;
			return;
		}

		// Get control rotation
		Rotation controlRotation = GetControlRotation( parent );

		// Calculate initial rotation
		Angles controlAngles = controlRotation.Angles();
		Angles initialAngles = new Angles(
			InheritPitch ? controlAngles.pitch : 0f,
			InheritYaw ? controlAngles.yaw : 0f,
			InheritRoll ? controlAngles.roll : 0f
		);
		initialAngles += CameraRotationOffset;
		initialAngles.pitch = initialAngles.pitch.Clamp( MinPitch, MaxPitch );
		_currentRotation = initialAngles.ToRotation();

		// Calculate initial position
		Vector3 armOrigin = parent.WorldPosition + TargetOffset;
		Vector3 armEnd = armOrigin + _currentRotation.Forward * -TargetArmLength;
		_currentPosition = armEnd + _currentRotation * SocketOffset;

		// Initialize parent position tracking
		_previousParentPosition = parent.WorldPosition;

		// Set transform immediately
		WorldPosition = _currentPosition;
		WorldRotation = _currentRotation;

		_isInitialized = true;
	}

	/// <summary>
	/// Instantly snaps spring arm to its calculated target position and rotation.
	/// Resets all lag and immediately moves to where the arm should be.
	/// 
	/// Use when:
	/// - Teleporting the character
	/// - Changing camera modes
	/// - Respawning
	/// - Any time you need instant camera positioning without smooth transition
	/// </summary>
	public void SnapToTarget()
	{
		InitializeState();
	}

	/// <summary>
	/// Applies preset configuration values based on selected preset type.
	/// </summary>
	private void ApplyPreset( SpringArmPreset preset )
	{
		switch ( preset )
		{
			case SpringArmPreset.ThirdPerson:
				// Standard third-person over-the-shoulder camera
				TargetArmLength = 400f;
				MinArmLength = 50f;
				TargetOffset = new Vector3( 0, 0, 60f );
				SocketOffset = new Vector3( 0, 0, 10f );
				EnablePositionLag = true;
				PositionLagSpeed = 10f;
				MaxPositionLagDistance = 200f;
				EnableRotationLag = true;
				RotationLagSpeed = 10f;
				EnableCameraLag = false;
				DoCollisionTest = true;
				ProbeSize = 16f;
				InheritPitch = true;
				InheritYaw = true;
				InheritRoll = false;
				UseControlRotation = false;
				CameraRotationOffset = Angles.Zero;
				break;

			case SpringArmPreset.FirstPerson:
				// First-person view at eye level
				TargetArmLength = 0f;
				MinArmLength = 0f;
				TargetOffset = new Vector3( 0, 0, 0f );
				SocketOffset = Vector3.Zero;
				EnablePositionLag = true;
				PositionLagSpeed = 20f;
				MaxPositionLagDistance = 50f;
				EnableRotationLag = false;
				EnableCameraLag = false;
				DoCollisionTest = false;
				InheritPitch = true;
				InheritYaw = true;
				InheritRoll = false;
				UseControlRotation = false;
				CameraRotationOffset = Angles.Zero;
				break;

			case SpringArmPreset.TopDown:
				// Top-down isometric view
				TargetArmLength = 800f;
				MinArmLength = 400f;
				TargetOffset = Vector3.Zero;
				SocketOffset = Vector3.Zero;
				EnablePositionLag = true;
				PositionLagSpeed = 8f;
				MaxPositionLagDistance = 300f;
				EnableRotationLag = false;
				EnableCameraLag = true;
				CameraLagSpeed = 5f;
				MaxCameraLagDistance = 200f;
				DoCollisionTest = true;
				ProbeSize = 20f;
				InheritPitch = false;
				InheritYaw = false;
				InheritRoll = false;
				UseControlRotation = false;
				CameraRotationOffset = new Angles( -70f, 0f, 0f );
				break;

			case SpringArmPreset.SideScroller:
				// Side-scrolling 2.5D platformer
				TargetArmLength = 600f;
				MinArmLength = 400f;
				TargetOffset = new Vector3( 0, 0, 40f );
				SocketOffset = Vector3.Zero;
				EnablePositionLag = true;
				PositionLagSpeed = 12f;
				MaxPositionLagDistance = 150f;
				EnableRotationLag = false;
				EnableCameraLag = false;
				DoCollisionTest = false;
				InheritPitch = false;
				InheritYaw = false;
				InheritRoll = false;
				UseControlRotation = false;
				CameraRotationOffset = new Angles( 0f, 90f, 0f );
				break;
		}

		if ( _isInitialized )
		{
			SnapToTarget();
		}
	}
}
