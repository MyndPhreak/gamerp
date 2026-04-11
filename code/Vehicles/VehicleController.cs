using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameRP.Vehicles;

public sealed class VehicleController : Component
{
	// --- Drive ---
	/// <summary>Top speed in units/sec. 1200 ≈ 80 km/h in S&box units.</summary>
	[Property] public float MaxSpeed { get; set; } = 1200f;
	/// <summary>Drive force per driven wheel. For a 1000 kg car: ~3000 = gentle, ~8000 = sporty.</summary>
	[Property] public float AccelerationForce { get; set; } = 5000f;
	/// <summary>Brake force per wheel. Typically 1.5–2× AccelerationForce.</summary>
	[Property] public float BrakeForce { get; set; } = 8000f;
	/// <summary>Reverse drive force per driven wheel.</summary>
	[Property] public float ReverseForce { get; set; } = 3000f;

	// --- Steering ---
	[Property] public float MaxSteerAngle { get; set; } = 35f;
	/// <summary>Fraction of MaxSteerAngle removed at MaxSpeed. 0 = no reduction, 1 = fully locked at speed.</summary>
	[Property, Range( 0f, 1f )] public float SteerSpeedFactor { get; set; } = 0.6f;
	/// <summary>How fast the steering angle moves toward the target (degrees per second).</summary>
	[Property] public float SteerRate { get; set; } = 90f;

	// --- Handbrake ---
	[Property] public float HandbrakeForce { get; set; } = 150000f;

	// --- Parking ---
	/// <summary>
	/// Velocity decay rate (per second) applied when the car has no driver.
	/// Higher = faster coast-to-stop. Car still reacts to external impacts.
	/// </summary>
	[Property, Range( 0f, 10f )] public float ParkingDrag { get; set; } = 3f;

	// --- Physics ---
	[Property] public Vector3 CenterOfMass { get; set; } = Vector3.Zero;

	// --- Runtime (read-only in editor) ---
	[Property, ReadOnly] public float CurrentSpeed { get; private set; }
	[Property, ReadOnly] public float SteerAngle { get; private set; }

	private RPPlayer _driver;
	public RPPlayer Driver
	{
		get => _driver;
		set
		{
			_driver = value;
			if ( _driver == null )
			{
				_throttleInput = 0f;
				_brakeInput = 0f;
				_handbrakeInput = 0f;
			}
			// MotionEnabled is always true — the car reacts to impacts and coasts to a stop
			// under ParkingDrag rather than freezing instantly.
		}
	}

	// --- Internals ---
	private Rigidbody _body;
	private List<VehicleAxle> _axles = new();
	private List<VehicleWheel> _allWheels = new();

	// Input cached from OnUpdate, consumed in OnFixedUpdate
	private float _throttleInput;
	private float _brakeInput;
	private float _handbrakeInput;

	protected override void OnAwake()
	{
		_body = Components.Get<Rigidbody>();
		if ( _body == null )
		{
			Log.Error( "[VehicleController] Rigidbody component required on this GameObject" );
			return;
		}

		// Apply center of mass offset
		if ( CenterOfMass != Vector3.Zero )
		{
			_body.OverrideMassCenter = true;
			_body.MassCenterOverride = CenterOfMass;
		}

		// Ensure vehicle tag for collision filtering
		GameObject.Tags.Add( "vehicle" );

		// Discover axles (wheels are spawned by axles in their own OnAwake)
		_axles = Components.GetAll<VehicleAxle>( FindMode.InChildren ).ToList();
		if ( _axles.Count == 0 )
			Log.Warning( "[VehicleController] No VehicleAxle components found in children" );
	}

	protected override void OnStart()
	{
		// Build flat wheel list after axles have spawned their wheels
		_allWheels = _axles.SelectMany( a => a.GetWheels() ).ToList();
		if ( _allWheels.Count == 0 )
		{
			Log.Warning( "[VehicleController] Axles produced no wheels" );
		}
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

			_handbrakeInput = Input.Down( "Jump" ) ? 1f : 0f;
		}

		// Separate throttle from brake.
		// CurrentSpeed is signed: positive = forward, negative = backward.
		if ( rawThrottle > 0f && CurrentSpeed > -10f )
		{
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
			_throttleInput = rawThrottle;
			_brakeInput = 0f;
		}

		// --- Speed-sensitive steering ---
		var speedFraction = (MathF.Abs( CurrentSpeed ) / MaxSpeed).Clamp( 0f, 1f );
		var steerReduction = 1f - speedFraction * SteerSpeedFactor;
		var targetSteer = steerInput * MaxSteerAngle * steerReduction;
		var diff = targetSteer - SteerAngle;
		var maxStep = SteerRate * Time.Delta;
		SteerAngle += MathF.Sign( diff ) * MathF.Min( MathF.Abs( diff ), maxStep );

		// --- Push steer angle and speed to wheels for visuals ---
		foreach ( var wheel in _allWheels )
		{
			wheel.SteerAngle = SteerAngle;
			wheel.CurrentSpeed = CurrentSpeed;
		}
	}

	protected override void OnFixedUpdate()
	{
		// Suspension runs even without a driver so the car stays grounded at all times.
		if ( IsProxy || _body == null ) return;

		var hasDriver = Driver != null;

		// --- Phase 1: Compute all wheel forces with the SAME body velocity snapshot ---
		var pendingForces = new List<VehicleWheel.WheelForceResult>( _allWheels.Count );

		foreach ( var wheel in _allWheels )
		{
			var result = wheel.ComputeForces(
				hasDriver ? _throttleInput : 0f,
				hasDriver ? _brakeInput : 0f,
				SteerAngle,
				_body,
				_allWheels.Count,
				AccelerationForce,
				BrakeForce,
				ReverseForce,
				hasDriver ? _handbrakeInput : 0f,
				HandbrakeForce );

			if ( !result.IsGrounded ) continue;
			pendingForces.Add( result );
		}

		// --- Phase 2: Apply all forces at once ---
		foreach ( var r in pendingForces )
		{
			// Apply suspension at GroundContact to allow the chassis to correctly heave/pitch
			_body.PhysicsBody.ApplyImpulseAt( r.GroundContact, r.SuspensionForce * 0.02f );
			
			// Apply lateral forces at MountPoint (axle). Applying this at GroundContact creates a massive 
			// lever arm that causes the ultra-stiff tire grip to violently fight natural chassis roll, 
			// resulting in rapid left/right vibrations.
			_body.PhysicsBody.ApplyImpulseAt( r.MountPoint, r.LateralForce * 0.02f );
			
			// Apply drive force at MountPoint 
			_body.PhysicsBody.ApplyImpulseAt( r.MountPoint, r.DriveForce * 0.02f );
		}

		// --- Parking drag (no driver) ---
		// Decelerates the car naturally after the driver exits so it coasts to a stop
		// instead of freezing instantly. Car still reacts to external impacts normally.
		if ( !hasDriver && _body.Velocity.LengthSquared > 0.01f )
		{
			var mass = _body.PhysicsBody?.Mass ?? 1000f;
			_body.PhysicsBody.ApplyImpulse( -_body.Velocity * mass * ParkingDrag * Time.Delta );
		}

		// --- Per-axle anti-roll ---
		foreach ( var axle in _axles )
		{
			axle.ApplyAntiRoll( _body );
		}

		// --- Hard velocity clamp ---
		if ( _body.Velocity.Length > MaxSpeed )
			_body.Velocity = _body.Velocity.Normal * MaxSpeed;

		// Always update speed so wheel spin visuals reflect actual car velocity while coasting.
		CurrentSpeed = Vector3.Dot( _body.Velocity, WorldRotation.Forward );
	}
}
