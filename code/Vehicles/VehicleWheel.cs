using Sandbox;
using System;

namespace GameRP.Vehicles;

public sealed class VehicleWheel : Component
{
	// --- Setup ---
	[Property] public float SuspensionLength { get; set; } = 20f;
	[Property] public float Radius { get; set; } = 14f;
	/// <summary>If true, Radius is computed from the wheel model's mesh bounds on startup.</summary>
	[Property] public bool AutoDetectRadius { get; set; } = true;
	[Property] public bool IsSteerable { get; set; } = false;
	[Property] public bool IsDriven { get; set; } = false;
	[Property] public bool ReverseSpinDirection { get; set; } = false;
	/// <summary>Rear wheels should have this enabled so the handbrake affects them.</summary>
	[Property] public bool IsHandbrakeWheel { get; set; } = false;

	// --- Suspension tuning ---
	/// <summary>0 = soft/bouncy, 1 = stiff/firm. Controls active ride height and damping ratio.</summary>
	[Property, Range( 0f, 1f )] public float Stiffness { get; set; } = 0.5f;

	// --- Grip tuning ---
	[Property] public float TireGripCoefficient { get; set; } = 1.0f;
	/// <summary>Max lateral corrective force per wheel. Must be several times larger than
	/// AccelerationForce per driven wheel to prevent fishtailing.</summary>
	[Property] public float LateralFriction { get; set; } = 800000f;
	/// <summary>Lateral velocity correction gain. Keep below mass/(numWheels·fixedDt) — roughly
	/// 25000 for a 1000 kg car on 4 wheels at 50 Hz physics.</summary>
	[Property] public float LateralGripStiffness { get; set; } = 25000f;
	[Property, Range( 0f, 1f )] public float LongitudinalFriction { get; set; } = 1f;

	// --- Handbrake Tuning ---
	public float HandbrakeSlidingFriction { get; set; } = 0.65f;
	public float HandbrakeLateralFriction { get; set; } = 0.8f;

	// --- Runtime (read-only in editor) ---
	[Property, ReadOnly] public bool IsGrounded { get; private set; }
	[Property, ReadOnly] public float SuspensionCompression { get; private set; }
	[Property, ReadOnly] public Vector3 GroundHitPosition { get; private set; }

	// Set by VehicleController for visuals
	public float CurrentSpeed { get; set; }
	public float SteerAngle { get; set; }
	public float Handbrake { get; set; }

	// --- Internals ---
	private ModelRenderer _wheelModel;
	private float _spinAngle;
	private Vector3 _attachLocalPos;
	private GameObject _vehicleRoot;
	// Initialised to 0.5 so the wheel sits at the correct rest position before ComputeForces runs.
	private float _smoothedCompression = 0.5f;

	public struct WheelForceResult
	{
		public Vector3 SuspensionForce;
		public Vector3 DriveForce;
		public Vector3 FrictionForce;
		public Vector3 LateralForce;
		/// <summary>Where suspension + drive forces are applied (mount point, near chassis).</summary>
		public Vector3 MountPoint;
		/// <summary>Where lateral grip + friction forces are applied (ground contact).</summary>
		public Vector3 GroundContact;
		public bool IsGrounded;
	}

	protected override void OnAwake()
	{
		_wheelModel = Components.Get<ModelRenderer>();

		if ( AutoDetectRadius && _wheelModel?.Model != null )
		{
			var size = _wheelModel.Model.Bounds.Size;
			var detected = MathF.Max( size.y, size.z ) / 2f;
			if ( detected > 0f )
			{
				Radius = detected;
				Log.Info( $"[VehicleWheel] Auto-detected radius {Radius:F1} from bounds {size}" );
			}
		}

		// Walk up to the vehicle root (VehicleController's GameObject).
		var controller = Components.GetInAncestors<VehicleController>();
		_vehicleRoot = controller?.GameObject ?? GameObject.Parent?.Parent ?? GameObject.Parent;

		_attachLocalPos = LocalPosition;
	}

	protected override void OnStart()
	{
		// If the user manually specifies a Radius (auto-detect off), physically scale the 
		// visual renderer so the 3D model accurately reflects its actual physics size!
		if ( !AutoDetectRadius && _wheelModel?.Model != null && Radius > 0f )
		{
			var size = _wheelModel.Model.Bounds.Size;
			var modelRadius = MathF.Max( size.y, size.z ) / 2f;
			if ( modelRadius > 0.01f )
			{
				var scale = Radius / modelRadius;
				_wheelModel.GameObject.LocalScale = new Vector3(scale);
				Log.Info( $"[VehicleWheel] Scaled visual wheel '{GameObject.Name}' to x{scale:F2} to match manual Radius {Radius:F1}" );
			}
		}
	}

	/// <summary>
	/// Called by VehicleController each OnFixedUpdate. Returns forces to apply to the car Rigidbody.
	/// </summary>
	public WheelForceResult ComputeForces(
		float throttle,
		float brake,
		float steerAngle,
		Rigidbody body,
		int totalWheels,
		float accelerationForce,
		float brakeForce,
		float reverseForce,
		float handbrake,
		float handbrakeForce,
		Vector3 localForward )
	{
		var result = new WheelForceResult();

		// Deferred root lookup if OnAwake ran before hierarchy was ready
		if ( _vehicleRoot == null || !_vehicleRoot.IsValid )
		{
			var controller = Components.GetInAncestors<VehicleController>();
			_vehicleRoot = controller?.GameObject ?? GameObject.Parent?.Parent ?? GameObject.Parent;
		}
		if ( _vehicleRoot == null ) return result;

		var worldUp = _vehicleRoot.WorldRotation.Up;
		// Ray origin = fixed mount point on the axle, NOT the moving wheel visual.
		var axle = GameObject.Parent;
		var mountPoint = axle.WorldPosition + axle.WorldRotation * _attachLocalPos;
		// Offset upward so axle position ≈ wheel center at rest.
		var rayOrigin = mountPoint + worldUp * (SuspensionLength * 0.5f);
		var rayDir = -worldUp;
		var rayLength = SuspensionLength + Radius;

		var trace = Scene.Trace
			.Ray( new Ray( rayOrigin, rayDir ), rayLength )
			.IgnoreGameObjectHierarchy( _vehicleRoot )
			.Run();

		if ( !trace.Hit )
		{
			IsGrounded = false;
			SuspensionCompression = 0f;
			GroundHitPosition = rayOrigin + rayDir * rayLength;
			return result;
		}

		IsGrounded = true;
		SuspensionCompression = 1f - ((trace.Distance - Radius).Clamp( 0f, SuspensionLength ) / SuspensionLength);
		GroundHitPosition = trace.HitPosition;

		result.IsGrounded = true;
		// Suspension and drive forces applied at the mount point (near chassis).
		// This reduces pitch/roll torques since the mount point is close to the center of mass.
		result.MountPoint = rayOrigin;
		// Lateral grip applied at ground contact for correct yaw torque during cornering.
		result.GroundContact = trace.HitPosition;

		// --- Velocity at contact point (for drive/lateral calculations) ---
		var contactVelocity = body.GetVelocityAtPoint( trace.HitPosition );

		// --- Active Suspension (spring + damper) ---
		// Computes required forces dynamically based on current Rigidbody mass to prevent sagging under load.
		var gravity = MathF.Abs( Scene.PhysicsWorld?.Gravity.z ?? 800f );
		if (gravity < 1f) gravity = 800f; // S&box fallback roughly 800 units/s2
		
		var effectiveMass = (body.PhysicsBody?.Mass ?? 1000f) / MathF.Max( 1, totalWheels );

		// --- Adaptive Engine & Handling (Mass-Independent Tuning) ---
		// We retain massRatio scaling ONLY for lateral grip (so heavy cars still turn properly).
		// Drive forces are pure Newtons computed correctly by the controller.
		var massRatio = effectiveMass / 250f;
		var adaptiveLateralStiffness = LateralGripStiffness * massRatio;
		
		// Map stiffness to a natural resting compression point (e.g. 50% vs 30% of travel used at rest)
		var restCompression = MathX.Lerp( 0.6f, 0.3f, Stiffness );
		var targetSpring = (effectiveMass * gravity) / restCompression;
		
		var dampRatio = MathX.Lerp( 0.4f, 0.8f, Stiffness );
		// The true physical spring constant 'k' is targetSpring / SuspensionLength.
		// Missing this division caused the damper to be Sqrt(SuspensionLength) times too strong (overdamped/bouncy rock)
		var k_true = targetSpring / MathF.Max( 0.1f, SuspensionLength );
		var targetDamper = dampRatio * 2f * MathF.Sqrt( k_true * effectiveMass );

		// Use suspension mount point velocity (vertical only). This correctly resists roll and pitch,
		// unlike using center-of-mass velocity.
		var pointVel = body.GetVelocityAtPoint( result.MountPoint );
		var vertVel = Vector3.Dot( pointVel, worldUp );

		var springForce = SuspensionCompression * targetSpring;
		var dampForce = vertVel * targetDamper;

		// Anti-spazz constraint: Prevent damper from applying a force so massive that it reverses
		// the suspension velocity within a single physics tick.
		var maxDampForce = (effectiveMass * MathF.Abs(vertVel)) / Time.Delta;
		dampForce = MathF.Sign(dampForce) * MathF.Min(MathF.Abs(dampForce), maxDampForce);

		// Suspension can only push the chassis UP. Rebound damping shouldn't suck the car into the dirt!
		var totalForce = springForce - dampForce;
		if ( totalForce < 0f ) totalForce = 0f;

		// Hard cap the maximum upward force to ~5 Gs per wheel. 
		// If the car drops from the sky, the suspension bottoms out softly and lets the rigid chassis
		// take the actual impact with the ground. This completely prevents wild off-center physics spins.
		var maxForceGClamp = effectiveMass * gravity * 5f;
		if ( totalForce > maxForceGClamp ) totalForce = maxForceGClamp;

		result.SuspensionForce = worldUp * totalForce;

		// --- Wheel forward direction (steered for front wheels) ---
		var modelForward = _vehicleRoot.WorldRotation * localForward;
		var wheelForward = Rotation.FromAxis( worldUp, IsSteerable ? steerAngle : 0f ) * modelForward;
		var forwardSpeed = Vector3.Dot( contactVelocity, wheelForward );

		float handbrakeFactor = (IsHandbrakeWheel && handbrake > 0f) ? handbrake : 0f;

		// --- Pillar 1: LOAD-DEPENDENT GRIP ---
		// The absolute limit of grip this tire has is dictated strictly by the weight currently pressing on it.
		var normalForce = MathF.Max(springForce, 0f); 
		var maxGrip = normalForce * LongitudinalFriction * 1.2f * TireGripCoefficient; 

		// --- LONGITUDINAL FORCES (Drive / Brake) ---
		var rawDrive = 0f;
		bool isFriction = false;

		if ( handbrakeFactor > 0f )
		{
			var kineticBrake = normalForce * HandbrakeSlidingFriction;
			rawDrive = -MathF.Sign(forwardSpeed) * kineticBrake * handbrakeFactor;
			isFriction = true;
		}
		else 
		{
			if ( IsDriven && throttle > 0f ) 
				rawDrive = throttle * accelerationForce * LongitudinalFriction;
			else if ( brake > 0f ) 
			{
				rawDrive = -MathF.Sign(forwardSpeed) * brake * brakeForce * LongitudinalFriction;
				isFriction = true;
			}
			else if ( IsDriven && throttle < 0f ) 
				rawDrive = throttle * reverseForce * LongitudinalFriction;
			else
			{
				// Natural rolling resistance to prevent infinite glides
				var rollingDrag = normalForce * 0.015f; 
				// Simulator idle engine braking (driveline drag)
				if ( IsDriven ) rollingDrag += (accelerationForce * 0.05f); 
				
				rawDrive = -MathF.Sign(forwardSpeed) * rollingDrag * LongitudinalFriction;
				isFriction = true;
			}
		}

		// Cap longitudinal demand strictly at its absolute physical grip limit
		var appliedDrive = rawDrive.Clamp(-maxGrip, maxGrip);

		// Anti-spazz constraint (only applies to friction/braking so we don't cap hard acceleration requests)
		if ( isFriction && MathF.Sign(appliedDrive) != MathF.Sign(forwardSpeed) && MathF.Abs(appliedDrive) > 0f )
		{
			// The exact force required to cleanly zero the velocity in one tick without jitter
			var maxBrakeConstraint = (effectiveMass * MathF.Abs(forwardSpeed)) / Time.Delta;
			if ( MathF.Abs(appliedDrive) > maxBrakeConstraint )
			{
				appliedDrive = MathF.Sign(appliedDrive) * maxBrakeConstraint;
			}
		}

		if ( isFriction )
		{
			result.FrictionForce = wheelForward * appliedDrive;
			result.DriveForce = Vector3.Zero;
		}
		else
		{
			result.DriveForce = wheelForward * appliedDrive;
			result.FrictionForce = Vector3.Zero;
		}

		// --- Pillar 2: THE FRICTION CIRCLE ---
		// A tire has a finite grip budget. We subtract whatever grip the longitudinal forces (braking/accelerating) 
		// stole from the tire to find our TRUE remaining lateral steering grip.
		// If you brake 100%, you get 0% steering grip.
		var usedGrip = MathF.Abs(appliedDrive);
		var maxLateralGrip = MathF.Sqrt( MathF.Max((maxGrip * maxGrip) - (usedGrip * usedGrip), 0f) );

		// Handbrake overrides the friction circle by manually snapping the tire loose into a slide
		if ( handbrakeFactor > 0f )
		{
			var slidingLateralGrip = normalForce * HandbrakeLateralFriction; 
			maxLateralGrip = MathX.Lerp(maxLateralGrip, slidingLateralGrip, handbrakeFactor);
		}

		// --- Pillar 3: ORGANIC SLIP-ANGLE CURVE (PACEJKA APPROX) ---
		var rightDir = Vector3.Cross( worldUp, wheelForward ).Normal;
		var lateralVel = Vector3.Dot( contactVelocity, rightDir );

		// 'Stiffness' controls how snappy the steering is (how fast grip builds as you turn the wheel).
		// Instead of hitting a brick wall and snapping into understeer, we use an arc-tangent curve 
		// to create a buttery smooth limit breakaway, mimicking organic rubber flexing.
		var slipDemand = lateralVel * adaptiveLateralStiffness;

		float lateralMag = 0f;
		if ( maxLateralGrip > 1f )
		{
			// Normalize demand to a 0.0 - 1.0 (and beyond) scale representing how deeply we exceeded grip
			var normalizedSlip = slipDemand / maxLateralGrip;
			// Atan smoothly rolls off peak grip, mimicking the exact shape of a Pacejka tire curve
			lateralMag = maxLateralGrip * (2f / MathF.PI) * MathF.Atan(normalizedSlip * MathF.PI / 2f);
		}

		// Prevent lateral slip-stick spazzing by strictly capping lateral forces so they don't over-correct
		var maxLatConstraint = (effectiveMass * MathF.Abs(lateralVel)) / Time.Delta;
		lateralMag = MathF.Sign(lateralMag) * MathF.Min(MathF.Abs(lateralMag), maxLatConstraint);

		result.LateralForce = -rightDir * lateralMag;

		// (Anti-Creep Artificial Hill-Hold removed. Vehicles now rely entirely on their 
		// physical weight and natural mechanical driveline friction. They WILL roll down steep 
		// hills in neutral just like real physical objects and will react correctly if hit.)

		return result;
	}

	protected override void OnUpdate()
	{
		UpdateVisuals();
	}

	private void UpdateVisuals()
	{
		// Resolve vehicle root lazily — may not have been ready during OnAwake.
		if ( _vehicleRoot == null || !_vehicleRoot.IsValid )
		{
			var controller = Components.GetInAncestors<VehicleController>();
			_vehicleRoot = controller?.GameObject ?? GameObject.Parent?.Parent ?? GameObject.Parent;
		}
		if ( _vehicleRoot == null ) return;

		// Recompute mount point every frame directly from the axle transform.
		// This works whether or not ComputeForces has run (i.e. car has no driver yet).
		var axle = GameObject.Parent;
		if ( axle == null ) return;
		var worldUp = _vehicleRoot.WorldRotation.Up;
		var mountPoint = axle.WorldPosition + axle.WorldRotation * _attachLocalPos;

		// When grounded, track actual compression; when airborne or parked without a driver,
		// hold the current value so wheels don't snap to fully-extended.
		var targetCompression = IsGrounded ? SuspensionCompression : _smoothedCompression;
		_smoothedCompression = MathX.Lerp( _smoothedCompression, targetCompression, Time.Delta * 10f );

		// Position: mount point is at axle height; add halfTravel to reach ray origin,
		// then subtract suspensionDrop to place wheel center.
		var halfTravel = SuspensionLength * 0.5f;
		var suspensionDrop = SuspensionLength * (1f - _smoothedCompression);
		WorldPosition = mountPoint + worldUp * halfTravel - worldUp * suspensionDrop;

		if ( _wheelModel == null ) return;

		var spinDir = ReverseSpinDirection ? 1f : -1f;

		var visualSpeed = CurrentSpeed;
		if ( IsHandbrakeWheel && Handbrake > 0f )
		{
			visualSpeed = MathX.Lerp( visualSpeed, 0f, Handbrake );
		}

		_spinAngle = (_spinAngle + spinDir * visualSpeed * Time.Delta * (360f / (MathF.Tau * Radius))) % 360f;

		var flipRot = ReverseSpinDirection ? Rotation.FromAxis( Vector3.Up, 180f ) : Rotation.Identity;
		var steerRot = IsSteerable ? Rotation.FromAxis( Vector3.Up, SteerAngle ) : Rotation.Identity;
		var spinRot = Rotation.FromAxis( Vector3.Right, _spinAngle );

		// Base from axle world rotation — wheels tilt with body roll/pitch, staying perpendicular to the axle.
		WorldRotation = axle.WorldRotation * flipRot * steerRot * spinRot;
	}
}
