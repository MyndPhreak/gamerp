using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameRP.Vehicles;

public sealed class VehicleController : Component
{
	// --- Drivetrain ---
	[Property, Group("Drivetrain")] public float Horsepower { get; set; } = 250f;
	[Property, Group("Drivetrain")] public float PeakHpRPM { get; set; } = 6500f;
	[Property, Group("Drivetrain")] public float PeakTorqueRPM { get; set; } = 4500f;
	[Property, Group("Drivetrain")] public float IdleRPM { get; set; } = 800f;
	[Property, Group("Drivetrain")] public float MaxRPM { get; set; } = 7000f;
	[Property, Group("Drivetrain")] public List<float> GearRatios { get; set; } = new() { 3.20f, 1.95f, 1.30f, 1.0f, 0.80f, 0.60f };
	[Property, Group("Drivetrain")] public float FinalDrive { get; set; } = 3.42f;
	[Property, Group("Drivetrain")] public float ReverseGearRatio { get; set; } = 2.90f;
	[Property, Group("Drivetrain")] public float DrivetrainEfficiency { get; set; } = 0.85f;
	[Property, Group("Drivetrain")] public bool AutomaticTransmission { get; set; } = true;
	[Property, Group("Drivetrain")] public float UpshiftRPM { get; set; } = 6500f;
	[Property, Group("Drivetrain")] public float DownshiftRPM { get; set; } = 2500f;

	// --- Braking ---
	[Property, Group("Brakes"), Range( 0f, 50000f )] public float BrakeForce { get; set; } = 8000f;

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
	
	public enum ForwardAxisDirection
	{
		PositiveX_Forward,
		NegativeX_Backward,
		PositiveY_Left,
		NegativeY_Right
	}
	
	/// <summary>
	/// If your imported model naturally drives sideways (e.g. facing Left instead of Forward),
	/// change this axis so the math knows which way the "front" of the car points.
	/// </summary>
	[Property] public ForwardAxisDirection ForwardAxis { get; set; } = ForwardAxisDirection.PositiveX_Forward;

	public Vector3 GetForwardVector()
	{
		return ForwardAxis switch
		{
			ForwardAxisDirection.PositiveX_Forward => Vector3.Forward,
			ForwardAxisDirection.NegativeX_Backward => Vector3.Backward,
			ForwardAxisDirection.PositiveY_Left => Vector3.Left,
			ForwardAxisDirection.NegativeY_Right => Vector3.Right,
			_ => Vector3.Forward
		};
	}

	// --- Runtime (read-only in editor) ---
	[Property, ReadOnly] public float CurrentSpeed { get; private set; }
	[Property, ReadOnly, Group("Runtime")] public float SpeedKmh => MathF.Abs(CurrentSpeed) * 0.09144f;
	[Property, ReadOnly, Group("Runtime")] public float SpeedMph => MathF.Abs(CurrentSpeed) * 0.056818f;
	[Property, ReadOnly] public float SteerAngle { get; private set; }
	[Property, ReadOnly, Group("Runtime")] public float EngineRPM { get; private set; }
	[Property, ReadOnly, Group("Runtime")] public int CurrentGear { get; private set; } = 1;
	[Property, ReadOnly, Group("Runtime")] public float BrakeInput => _brakeInput;
	[Property, ReadOnly, Group("Runtime")] public float ThrottleInput => _throttleInput;
	[Property, ReadOnly, Group("Runtime")] public float HandbrakeInput => _handbrakeInput;

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
	private float _wheelRadiusMeters = 14f * 0.0254f;
	private float _wheelRadiusUnits = 14f;

	// Input cached from OnUpdate, consumed in OnFixedUpdate
	private float _throttleInput;
	private float _brakeInput;
	private float _handbrakeInput;
	private TimeSince _timeSinceLastShift = 10f;

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

		if ( _axles.Count > 0 )
		{
			var poweredAxle = _axles.FirstOrDefault( a => a.IsPowered ) ?? _axles.First();
			_wheelRadiusUnits = poweredAxle.WheelRadius;
			_wheelRadiusMeters = _wheelRadiusUnits * 0.0254f;
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

			// Manual shifting inputs
			if ( !AutomaticTransmission )
			{
				// Assuming standard sandbox bindings, e.g. "Run" for shift up, "Duck" for shift down,
				// or you can map these to actual custom actions like "ShiftUp", "ShiftDown".
				// For now we will watch for specific pressed keys:
				if ( Input.Pressed( "Run" ) && CurrentGear < GearRatios.Count )
				{
					CurrentGear++;
					_timeSinceLastShift = 0f;
				}
				else if ( Input.Pressed( "Duck" ) && CurrentGear > 1 )
				{
					CurrentGear--;
					_timeSinceLastShift = 0f;
				}
			}
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
		// Use a reference top speed of ~1200 units (80 kmh) for steering limits natively
		var speedFraction = (MathF.Abs( CurrentSpeed ) / 1200f).Clamp( 0f, 1f );
		var steerReduction = 1f - speedFraction * SteerSpeedFactor;
		var targetSteer = steerInput * MaxSteerAngle * steerReduction;
		var diff = targetSteer - SteerAngle;
		var maxStep = SteerRate * Time.Delta;
		SteerAngle += MathF.Sign( diff ) * MathF.Min( MathF.Abs( diff ), maxStep );

		// --- Push steer angle and speed to wheels for visuals ---
		var hasDriver = Driver != null;
		foreach ( var wheel in _allWheels )
		{
			wheel.SteerAngle = SteerAngle;
			wheel.CurrentSpeed = CurrentSpeed;
			wheel.Handbrake = hasDriver ? _handbrakeInput : 1f;
		}
	}

	protected override void OnFixedUpdate()
	{
		// Suspension runs even without a driver so the car stays grounded at all times.
		if ( IsProxy || _body == null ) return;

		var hasDriver = Driver != null;

		// --- Simulate Drivetrain (Engine RPM & Auto-Shifting) ---
		float currentGearRatio = (CurrentGear > 0 && CurrentGear <= GearRatios.Count) ? GearRatios[CurrentGear - 1] : 1.0f;
		
		// Wheel RPM accurately estimated from forward speed and true wheel radius
		float wheelRPM = MathF.Abs(CurrentSpeed) * 60f / (2f * MathF.PI * _wheelRadiusUnits);
		
		// Reverse tracking from user throttle intent
		bool isReversing = _throttleInput < 0f && CurrentSpeed < 10f;
		float activeRatio = isReversing ? ReverseGearRatio : currentGearRatio;
		
		float targetRPM = wheelRPM * activeRatio * FinalDrive;
		
		// Handbrake Neutral (Free Revving simulator)
		bool isFreeRevving = hasDriver && _handbrakeInput > 0.5f;

		if ( isFreeRevving )
		{
			float revTarget = (MathF.Abs(_throttleInput) > 0f) ? MaxRPM : IdleRPM;
			// Fast rev up, slower rev down
			float revSpeed = (revTarget > EngineRPM) ? 15f : 5f;
			EngineRPM = MathX.Lerp(EngineRPM, revTarget, Time.Delta * revSpeed);
		}
		// Torque Converter / Clutch Stall Simulator
		else if ( targetRPM < IdleRPM + 1000f )
		{
			float throttleFrac = hasDriver ? MathF.Abs(_throttleInput) : 0f;
			float stallRPM = MathX.Lerp(IdleRPM, IdleRPM + 1500f, throttleFrac);
			
			// Always take the HIGHER of either true mechanical RPM or the artificial stall revs.
			// This completely eliminates the "RPM drop" glitch when the wheels catch up!
			float desiredRPM = MathF.Max(targetRPM, stallRPM);
			EngineRPM = MathX.Lerp(EngineRPM, desiredRPM, Time.Delta * 8f);
		}
		else
		{
			// Hard mechanical lock mapping above stall speed
			EngineRPM = MathX.Lerp(EngineRPM, targetRPM, Time.Delta * 12f);
		}

		// Auto-Shift Down/Up Logic (Only if Automatic)
		if ( AutomaticTransmission && _timeSinceLastShift > 0.5f )
		{
			if ( EngineRPM > UpshiftRPM && CurrentGear < GearRatios.Count && CurrentSpeed > 50f && !isReversing )
			{
				CurrentGear++;
				_timeSinceLastShift = 0f;
			}
			else if ( EngineRPM < DownshiftRPM && CurrentGear > 1 && !isReversing )
			{
				CurrentGear--;
				_timeSinceLastShift = 0f;
			}
		}

		// Calculate Peak Torque from HP
		float peakTorque = (Horsepower * 5252f) / PeakHpRPM;

		// Simple parabolic torque curve: peaks at PeakTorqueRPM and drops off toward redline
		float rpmFrac = (EngineRPM - IdleRPM) / (MaxRPM - IdleRPM);
		float peakFrac = (PeakTorqueRPM - IdleRPM) / (MaxRPM - IdleRPM);

		// Creates a smooth curve that drops off if you over-rev or under-rev
		float curveGrip = 1f - MathF.Pow(rpmFrac - peakFrac, 2f) * 2f; 
		float currentTorque = peakTorque * MathF.Max(curveGrip, 0.2f); // Never drop below 20% torque

		// True Physical Drive Force (Newtons)
		float driveForce = (currentTorque * currentGearRatio * FinalDrive * DrivetrainEfficiency) / _wheelRadiusMeters;
		float reverseForce = (currentTorque * ReverseGearRatio * FinalDrive * DrivetrainEfficiency) / _wheelRadiusMeters;

		// Convert Newtons to Sandbox units (1 meter = 39.37 inches)
		float mToIn = 39.37f;
		int drivenWheels = _allWheels.Count(w => w.IsDriven);
		if ( drivenWheels == 0 ) drivenWheels = 1;

		float sboxDrive = (driveForce * mToIn) / drivenWheels;
		float sboxReverse = (reverseForce * mToIn) / drivenWheels;
		float sboxBrake = BrakeForce * mToIn;
		float sboxHandbrake = HandbrakeForce * mToIn;

		// Authentic Shift Delay Mechanism (0.3 seconds to change gear where clutch is disengaged)
		// We also enforce 0 drive force when "free revving" with the handbrake!
		if ( (_timeSinceLastShift < 0.3f && !isReversing) || isFreeRevving )
		{
			sboxDrive = 0f;
		}

		// Hard Redline Limiter Bounce 
		if ( EngineRPM > MaxRPM )
		{
			sboxDrive = 0f;
			EngineRPM = MaxRPM; 
		}

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
				sboxDrive,
				sboxBrake,
				sboxReverse,
				hasDriver ? _handbrakeInput : 1f,
				sboxHandbrake,
				GetForwardVector() );

			if ( !result.IsGrounded ) continue;
			pendingForces.Add( result );
		}

		// --- Phase 2: Apply all forces at once ---
		foreach ( var r in pendingForces )
		{
			// Apply suspension at GroundContact to allow the chassis to correctly heave/pitch
			_body.PhysicsBody.ApplyImpulseAt( r.GroundContact, r.SuspensionForce * Time.Delta );
			
			// Apply lateral forces at MountPoint (axle). Applying this at GroundContact creates a massive 
			// lever arm that causes the ultra-stiff tire grip to violently fight natural chassis roll, 
			// resulting in rapid left/right vibrations.
			_body.PhysicsBody.ApplyImpulseAt( r.MountPoint, r.LateralForce * Time.Delta );
			
			// Apply drive force at MountPoint to prevent massive wheelies/squat!
			// While real tires push at ground contact, applying it there natively treats the chassis 
			// as a solid rigid body being dragged by its lowest extremity. In a real car, the thrust 
			// travels through suspension control arms directly into the chassis mount points, 
			// structurally reducing the extreme pitch leverage.
			_body.PhysicsBody.ApplyImpulseAt( r.MountPoint, r.DriveForce * Time.Delta );

			// Friction and braking rigidly applies at GroundContact to ensure the velocity constraint
			// (which is calculated strictly based on contact patch velocity) remains mathematically pure.
			_body.PhysicsBody.ApplyImpulseAt( r.GroundContact, r.FrictionForce * Time.Delta );
		}

		// (Artificial Parking Drag originally went here - removed to ensure empty vehicles
		// behave purely organically according to collision bounds and tire friction)

		// --- Per-axle anti-roll ---
		foreach ( var axle in _axles )
		{
			axle.ApplyAntiRoll( _body );
		}

		// Always update speed so wheel spin visuals reflect actual car velocity while coasting.
		var modelForward = WorldRotation * GetForwardVector();
		CurrentSpeed = Vector3.Dot( _body.Velocity, modelForward );
	}
}
