using Sandbox;
using System;

namespace GameRP.Vehicles;

public sealed class SteeringWheel : Component
{
	[Property] public float LockToLockRange { get; set; } = 900f;
	
	public enum AxisDirection
	{
		PositiveX_Forward,
		NegativeX_Backward,
		PositiveY_Left,
		NegativeY_Right,
		PositiveZ_Up,
		NegativeZ_Down
	}
	
	/// <summary>
	/// Represents the local axis that the steering wheel spins around.
	/// Depending on how the wheel was modeled/imported, this might be Y or Z.
	/// </summary>
	[Property] public AxisDirection RotateAxis { get; set; } = AxisDirection.PositiveX_Forward;
	
	private VehicleController _vehicle;
	private Rotation _baseRotation;

	protected override void OnStart()
	{
		_vehicle = Components.GetInAncestors<VehicleController>();
		_baseRotation = GameObject.LocalRotation;
		
		if ( _vehicle == null )
		{
			Log.Warning( $"[SteeringWheel] No VehicleController found in ancestors of {GameObject.Name}!" );
		}
	}

	protected override void OnUpdate()
	{
		// Let it sleep safely if there is no vehicle controller.
		if ( _vehicle == null ) return;
		
		// Map the current steer angle (-Max to Max) down to a normalized -1.0 to 1.0
		float normalizedSteer = 0f;
		if ( _vehicle.MaxSteerAngle > 0f )
		{
			// Example: if wheel is at -35 deg, normalized is -1.0
			normalizedSteer = _vehicle.SteerAngle / _vehicle.MaxSteerAngle;
		}

		// Calculate total rotation based on lock-to-lock (e.g. 900 / 2 = max 450 degrees per side)
		float targetAngle = normalizedSteer * (LockToLockRange * 0.5f);

		// Determine the axis vector
		Vector3 axisVector = RotateAxis switch
		{
			AxisDirection.PositiveX_Forward => Vector3.Forward,
			AxisDirection.NegativeX_Backward => Vector3.Backward,
			AxisDirection.PositiveY_Left => Vector3.Left,
			AxisDirection.NegativeY_Right => Vector3.Right,
			AxisDirection.PositiveZ_Up => Vector3.Up,
			AxisDirection.NegativeZ_Down => Vector3.Down,
			_ => Vector3.Forward
		};
		
		// Apply local rotation offset relative to the original rest orientation
		GameObject.LocalRotation = _baseRotation * Rotation.FromAxis( axisVector, targetAngle );
	}
}
