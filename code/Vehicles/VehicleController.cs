using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameRP.Vehicles;

public sealed class VehicleController : Component
{
	// --- Drive ---
	[Property] public float MaxSpeed { get; set; } = 1200f;
	[Property] public float AccelerationForce { get; set; } = 2000f;
	[Property] public float BrakeForce { get; set; } = 3000f;
	[Property] public float ReverseForce { get; set; } = 800f;

	// --- Steering ---
	[Property] public float MaxSteerAngle { get; set; } = 35f;
	/// <summary>Fraction of MaxSteerAngle removed at MaxSpeed. 0 = no reduction, 1 = fully locked at speed.</summary>
	[Property, Range( 0f, 1f )] public float SteerSpeedFactor { get; set; } = 0.6f;

	// --- Anti-roll ---
	[Property] public float AntiRollStrength { get; set; } = 1500f;

	// --- Runtime (read-only in editor) ---
	[Property, ReadOnly] public float CurrentSpeed { get; private set; }
	[Property, ReadOnly] public float SteerAngle { get; private set; }

	public RPPlayer Driver { get; set; }

	// --- Internals ---
	private Rigidbody _body;
	private List<VehicleWheel> _wheels = new();

	// Input cached from OnUpdate, consumed in OnFixedUpdate
	private float _throttleInput;
	private float _brakeInput;

	protected override void OnAwake()
	{
		_body = Components.Get<Rigidbody>();
		if ( _body == null )
			Log.Error( "[VehicleController] Rigidbody component required on this GameObject" );

		_wheels = Components.GetAll<VehicleWheel>( FindMode.InChildren ).ToList();
		if ( _wheels.Count == 0 )
			Log.Warning( "[VehicleController] No VehicleWheel components found in children" );
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		// --- Read input ---
		var steerInput = 0f;
		var rawThrottle = 0f;

		if ( Driver != null )
		{
			if ( Input.Down( "Left" ) ) steerInput += 1f;
			if ( Input.Down( "Right" ) ) steerInput -= 1f;

			if ( Input.Down( "Forward" ) ) rawThrottle = 1f;
			else if ( Input.Down( "Backward" ) ) rawThrottle = -1f;
		}

		// Separate throttle from brake.
		// CurrentSpeed is signed: positive = forward, negative = backward.
		// Note: CurrentSpeed lags OnFixedUpdate by up to one tick — acceptable for input decisions.
		if ( rawThrottle > 0f && CurrentSpeed > -10f )
		{
			// Accelerate forward (or brake out of slow reverse)
			_throttleInput = rawThrottle;
			_brakeInput = 0f;
		}
		else if ( rawThrottle > 0f && CurrentSpeed <= -10f )
		{
			// Moving backward fast: forward input = brake
			_throttleInput = 0f;
			_brakeInput = 1f;
		}
		else if ( rawThrottle < 0f && CurrentSpeed > 10f )
		{
			// Moving forward fast: backward input = brake
			_throttleInput = 0f;
			_brakeInput = 1f;
		}
		else
		{
			// Stopped or slow: direct throttle (positive = forward, negative = reverse)
			_throttleInput = rawThrottle;
			_brakeInput = 0f;
		}

		// --- Speed-sensitive steering ---
		var speedFraction = (MathF.Abs( CurrentSpeed ) / MaxSpeed).Clamp( 0f, 1f );
		var steerReduction = 1f - speedFraction * SteerSpeedFactor;
		SteerAngle = steerInput * MaxSteerAngle * steerReduction;

		// --- Push steer angle and speed to wheels for visuals ---
		foreach ( var wheel in _wheels )
		{
			wheel.SteerAngle = SteerAngle;
			wheel.CurrentSpeed = CurrentSpeed;
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy || _body == null ) return;

		// --- Collect and apply wheel forces ---
		foreach ( var wheel in _wheels )
		{
			var result = wheel.ComputeForces(
				_throttleInput,
				_brakeInput,
				SteerAngle,
				_body,
				AccelerationForce,
				BrakeForce,
				ReverseForce );

			if ( !result.IsGrounded ) continue;

			var totalForce = result.SuspensionForce + result.DriveForce + result.LateralForce;
			ApplyForceAtPoint( totalForce, result.ApplicationPoint );
		}

		// --- Hard velocity clamp ---
		// Direct Velocity write bypasses the physics constraint solver.
		// If micro-jitter appears at top speed, lower AccelerationForce or raise Rigidbody.LinearDamping instead.
		if ( _body.Velocity.Length > MaxSpeed )
			_body.Velocity = _body.Velocity.Normal * MaxSpeed;

		CurrentSpeed = Vector3.Dot( _body.Velocity, WorldRotation.Forward );

		ApplyAntiRoll();
	}

	/// <summary>
	/// Applies a world-space force at a world-space point on the car Rigidbody.
	/// Decomposes into linear force (at COM) + torque (r × F).
	/// </summary>
	private void ApplyForceAtPoint( Vector3 force, Vector3 worldPoint )
	{
		_body.PhysicsBody.ApplyForce( force );
		var r = worldPoint - _body.WorldPosition;
		_body.PhysicsBody.ApplyTorque( Vector3.Cross( r, force ) );
	}

	private void ApplyAntiRoll()
	{
		// Group wheels into axles by local forward (Y) position.
		// Front axle = positive local Y, rear axle = negative local Y.
		// Within each axle, sort by local X to get left/right.
		var front = _wheels
			.Where( w => w.LocalPosition.y >= 0f )
			.OrderBy( w => w.LocalPosition.x )
			.ToList();

		var rear = _wheels
			.Where( w => w.LocalPosition.y < 0f )
			.OrderBy( w => w.LocalPosition.x )
			.ToList();

		if ( front.Count < 2 && rear.Count < 2 )
			Log.Warning( "[VehicleController] Anti-roll: could not find 2+ wheels in either axle group. Check that wheel positions straddle the vehicle pivot on the Y axis." );

		ApplyAntiRollToAxle( front );
		ApplyAntiRollToAxle( rear );
	}

	private void ApplyAntiRollToAxle( List<VehicleWheel> axleWheels )
	{
		if ( axleWheels.Count < 2 ) return;

		var left = axleWheels[0];
		var right = axleWheels[axleWheels.Count - 1];

		var compressionDiff = left.SuspensionCompression - right.SuspensionCompression;
		var torque = WorldRotation.Forward * compressionDiff * AntiRollStrength;
		_body.PhysicsBody.ApplyTorque( torque );
	}
}
