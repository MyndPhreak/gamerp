using Sandbox;
using System;

namespace GameRP.Vehicles;

public sealed class VehicleWheel : Component
{
	// --- Setup ---
	[Property] public float SuspensionLength { get; set; } = 20f;
	[Property] public float Radius { get; set; } = 14f;
	[Property] public bool IsSteerable { get; set; } = false;
	[Property] public bool IsDriven { get; set; } = false;

	// --- Suspension tuning ---
	[Property] public float SpringStrength { get; set; } = 8000f;
	[Property] public float DampStrength { get; set; } = 400f;

	// --- Grip tuning ---
	[Property] public float LateralFriction { get; set; } = 6000f;
	[Property, Range( 0f, 1f )] public float LongitudinalFriction { get; set; } = 1f;

	// Minimum forward speed (units/sec) required for braking to apply.
	// Prevents jitter at near-standstill. S&Box units ≈ 1 unit/sec.
	private const float BrakeSpeedThreshold = 5f;

	// --- Runtime (read-only in editor) ---
	[Property, ReadOnly] public bool IsGrounded { get; private set; }
	[Property, ReadOnly] public float SuspensionCompression { get; private set; }
	public Vector3 GroundHitPosition { get; private set; }

	// Set by VehicleController for visuals
	public float CurrentSpeed { get; set; }
	public float SteerAngle { get; set; }

	// --- Internals ---
	private ModelRenderer _wheelModel;
	private float _spinAngle;
	private Vector3 _attachLocalPos;
	private Rotation _baseLocalRot;

	public struct WheelForceResult
	{
		public Vector3 SuspensionForce;
		public Vector3 DriveForce;
		public Vector3 LateralForce;
		public Vector3 ApplicationPoint;
		public bool IsGrounded;
	}

	protected override void OnAwake()
	{
		_wheelModel = Components.Get<ModelRenderer>();
		if ( _wheelModel == null )
			Log.Warning( "[VehicleWheel] No ModelRenderer found — wheel visuals will not update" );

		_attachLocalPos = LocalPosition;
		_baseLocalRot = LocalRotation;
	}

	/// <summary>
	/// Called by VehicleController each OnFixedUpdate. Returns forces to apply to the car Rigidbody.
	/// </summary>
	public WheelForceResult ComputeForces(
		float throttle,
		float brake,
		float steerAngle,
		Rigidbody body,
		float accelerationForce,
		float brakeForce,
		float reverseForce )
	{
		var result = new WheelForceResult();

		var parent = GameObject.Parent;
		var worldUp = parent.WorldRotation.Up;
		var rayOrigin = parent.WorldPosition + parent.WorldRotation * _attachLocalPos;
		var rayDir = -worldUp;
		var rayLength = SuspensionLength + Radius;

		var trace = Scene.Trace
			.Ray( new Ray( rayOrigin, rayDir ), rayLength )
			.WithoutTags( "vehicle" )
			.Run();

		if ( !trace.Hit )
		{
			IsGrounded = false;
			SuspensionCompression = 0f;
			GroundHitPosition = rayOrigin + rayDir * rayLength;
			return result; // all forces zero
		}

		IsGrounded = true;
		SuspensionCompression = 1f - ((trace.Distance - Radius).Clamp( 0f, SuspensionLength ) / SuspensionLength);
		GroundHitPosition = trace.HitPosition;

		result.IsGrounded = true;
		result.ApplicationPoint = trace.HitPosition;

		// --- Velocity at contact point ---
		// v_contact = v_linear + ω × r
		// AngularVelocity is in radians/second (Havok convention).
		// If your S&Box version returns deg/s, multiply AngularVelocity by (MathF.PI / 180f) first.
		var r = trace.HitPosition - body.WorldPosition;
		var contactVelocity = body.Velocity + Vector3.Cross( body.AngularVelocity, r );

		// --- Suspension (spring + damper) ---
		var vertVel = Vector3.Dot( contactVelocity, worldUp );
		var springForce = SuspensionCompression * SpringStrength;
		var dampForce = vertVel * DampStrength;
		result.SuspensionForce = worldUp * (springForce - dampForce);

		// --- Wheel forward direction (steered for front wheels) ---
		var wheelForward = Rotation.FromAxis( worldUp, IsSteerable ? steerAngle : 0f ) * parent.WorldRotation.Forward;
		var forwardSpeed = Vector3.Dot( contactVelocity, wheelForward );

		// --- Drive / Brake (driven wheels only) ---
		if ( IsDriven )
		{
			if ( throttle > 0f )
			{
				// Accelerate forward
				result.DriveForce = wheelForward * throttle * accelerationForce * LongitudinalFriction;
			}
			else if ( brake > 0f && MathF.Abs( forwardSpeed ) > BrakeSpeedThreshold )
			{
				// Braking: oppose current forward motion
				result.DriveForce = -wheelForward * MathF.Sign( forwardSpeed ) * brake * brakeForce * LongitudinalFriction;
			}
			else if ( throttle < 0f )
			{
				// Reverse
				result.DriveForce = wheelForward * throttle * reverseForce * LongitudinalFriction;
			}
		}

		// --- Lateral friction (all grounded wheels) ---
		// rightDir is perpendicular to wheel forward, in the ground plane
		var rightDir = Vector3.Cross( worldUp, wheelForward ).Normal;
		var lateralVel = Vector3.Dot( contactVelocity, rightDir );
		// Clamp to avoid overcorrection (no Time.Delta — physics integrator handles timestep)
		var lateralMag = (lateralVel * LateralFriction).Clamp( -LateralFriction, LateralFriction );
		result.LateralForce = -rightDir * lateralMag;

		return result;
	}

	protected override void OnUpdate()
	{
		UpdateVisuals();
	}

	private void UpdateVisuals()
	{
		var suspensionOffset = IsGrounded
			? (SuspensionLength * (1f - SuspensionCompression))
			: SuspensionLength;

		LocalPosition = _attachLocalPos + Vector3.Down * suspensionOffset;

		if ( _wheelModel == null ) return;

		_spinAngle = (_spinAngle + CurrentSpeed * Time.Delta * (360f / (MathF.Tau * Radius))) % 360f;
		var spinRotation = Rotation.FromAxis( Vector3.Right, _spinAngle );
		var steerRotation = IsSteerable
			? Rotation.FromAxis( Vector3.Up, SteerAngle )
			: Rotation.Identity;

		LocalRotation = _baseLocalRot * steerRotation * spinRotation;
	}
}
