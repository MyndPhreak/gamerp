using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameRP.Vehicles;

public sealed class VehicleController : Component
{
	[Property] public float MaxSpeed { get; set; } = 800f;
	[Property] public float Acceleration { get; set; } = 400f;
	[Property] public float BrakeForce { get; set; } = 600f;
	[Property] public float TurnRate { get; set; } = 120f;
	[Property, Range( 0.9f, 1f )] public float Drag { get; set; } = 0.98f;

	/// <summary>Which local axis is the vehicle's forward direction. Change if driving sideways.</summary>
	[Property] public Angles DriveRotationOffset { get; set; } = new Angles( 0, 0, 0 );

	[Property, ReadOnly] public float CurrentSpeed { get; private set; }
	[Property, ReadOnly] public float SteerAngle { get; private set; }

	public Vector3 Velocity { get; private set; }
	public RPPlayer Driver { get; set; }

	public Rotation DriveRotation => WorldRotation * DriveRotationOffset.ToRotation();

	private List<VehicleWheel> _wheels;
	private List<VehicleWheel> _drivenWheels;

	protected override void OnAwake()
	{
		_wheels = Components.GetAll<VehicleWheel>( FindMode.InChildren ).ToList();
		_drivenWheels = _wheels.Where( w => w.IsDriven ).ToList();

		if ( _wheels.Count == 0 )
			Log.Warning( "[VehicleController] No VehicleWheel components found in children" );
		if ( _drivenWheels.Count == 0 )
			Log.Warning( "[VehicleController] No driven wheels (IsDriven=true) found" );
	}


protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		if ( Driver != null )
		{
			HandleInput();
		}
		else
		{
			SteerAngle = 0f;
		}

		ApplyDrag();
		ApplyMovement();
		UpdateWheels();
	}

	private void HandleInput()
	{
		// Steering
		var steerInput = 0f;
		if ( Input.Down( "Left" ) ) steerInput += 1f;
		if ( Input.Down( "Right" ) ) steerInput -= 1f;

		// Speed-dependent steering: reduce turn rate at higher speeds
		var speedFactor = 1f - (CurrentSpeed / MaxSpeed * 0.7f).Clamp( 0f, 0.7f );
		SteerAngle = steerInput * 35f; // visual wheel angle

		if ( MathF.Abs( CurrentSpeed ) > 1f )
		{
			var turnDelta = steerInput * TurnRate * speedFactor * Time.Delta;
			WorldRotation *= Rotation.FromAxis( DriveRotation.Up, turnDelta );
		}

		// Throttle / Brake
		var throttleInput = 0f;
		if ( Input.Down( "Forward" ) ) throttleInput += 1f;
		if ( Input.Down( "Backward" ) ) throttleInput -= 1f;

		if ( throttleInput == 0f ) return;

		// Traction — scale acceleration by grounded driven wheels
		var groundedDriven = _drivenWheels.Count( w => w.IsGrounded );
		var traction = _drivenWheels.Count > 0
			? (float)groundedDriven / _drivenWheels.Count
			: 0f;

		var forward = DriveRotation.Forward;

		if ( throttleInput > 0f )
		{
			// Accelerate
			Velocity += forward * Acceleration * traction * throttleInput * Time.Delta;
		}
		else
		{
			// Brake or reverse
			if ( CurrentSpeed > 10f )
			{
				// Braking
				Velocity -= Velocity.Normal * BrakeForce * Time.Delta;
				if ( Vector3.Dot( Velocity, forward ) < 0f )
					Velocity = Vector3.Zero;
			}
			else
			{
				// Reverse (half acceleration)
				Velocity += forward * Acceleration * traction * throttleInput * 0.5f * Time.Delta;
			}
		}

	}

	private void ApplyDrag()
	{
		// Drag applied per-frame, scaled by delta so it's framerate-independent
		var dragThisFrame = MathF.Pow( Drag, Time.Delta * 60f );
		Velocity *= dragThisFrame;

		if ( Velocity.Length < 1f )
			Velocity = Vector3.Zero;
	}

	private void ApplyMovement()
	{
		// Clamp to max speed
		if ( Velocity.Length > MaxSpeed )
			Velocity = Velocity.Normal * MaxSpeed;

		WorldPosition += Velocity * Time.Delta;
		CurrentSpeed = Vector3.Dot( Velocity, DriveRotation.Forward );
	}

	private void UpdateWheels()
	{
		foreach ( var wheel in _wheels )
		{
			wheel.CurrentSpeed = CurrentSpeed;
			wheel.SteerAngle = SteerAngle;
		}
	}
}
