using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameRP.Vehicles;

[Flags]
public enum AxleType
{
	Steering = 1 << 0,
	Powered  = 1 << 1,
}

public sealed class VehicleAxle : Component
{
	// --- Axle configuration ---
	[Property] public AxleType Type { get; set; } = 0;
	[Property] public float Width { get; set; } = 65f;
	[Property] public Model WheelModel { get; set; }
	[Property] public float WheelRadius { get; set; } = 14f;
	[Property] public bool AutoDetectRadius { get; set; } = true;
	[Property] public bool IsHandbrake { get; set; } = false;

	// --- Suspension tuning ---
	/// <summary>How far the suspension can travel (units). Longer = more ground clearance and travel.</summary>
	[Property] public float SuspensionLength { get; set; } = 20f;
	/// <summary>0 = soft/bouncy, 1 = stiff/firm. Spring and damper are auto-calculated from vehicle mass.</summary>
	private float _stiffness = 0.5f;
	[Property, Range( 0f, 1f )] public float Stiffness 
	{ 
		get => _stiffness; 
		set 
		{ 
			_stiffness = value; 
			if (LeftWheel != null) LeftWheel.Stiffness = value;
			if (RightWheel != null) RightWheel.Stiffness = value;
		} 
	}

	// --- Grip tuning ---
	/// <summary>Max lateral corrective impulse per wheel per second (units/s²·kg equivalent).
	/// Must be several times larger than AccelerationForce to prevent fishtailing.</summary>
	[Property] public float LateralFriction { get; set; } = 800000f;
	/// <summary>Lateral velocity correction gain per wheel. Keep below mass/(numWheels·fixedDt)
	/// to avoid oscillation — roughly 25000 for a 1000 kg car on 4 wheels at 50 Hz.</summary>
	[Property] public float LateralGripStiffness { get; set; } = 25000f;
	[Property, Range( 0f, 1f )] public float LongitudinalFriction { get; set; } = 1f;

	// --- Anti-roll (per-axle) ---
	/// <summary>Torque applied to resist body roll. Higher = less lean in corners. 0 = disabled.</summary>
	[Property] public float AntiRollStrength { get; set; } = 5000f;

	// --- Runtime references ---
	public VehicleWheel LeftWheel { get; private set; }
	public VehicleWheel RightWheel { get; private set; }

	public bool IsSteering => Type.HasFlag( AxleType.Steering );
	public bool IsPowered => Type.HasFlag( AxleType.Powered );

	protected override void OnAwake()
	{
		// Guard: skip if wheels already exist (hot-reload safety)
		if ( GameObject.Children.Any( c => c.Components.Get<VehicleWheel>() != null ) )
		{
			foreach ( var child in GameObject.Children )
			{
				var wheel = child.Components.Get<VehicleWheel>();
				if ( wheel == null ) continue;

				if ( child.Name.EndsWith( "_L" ) ) LeftWheel = wheel;
				else if ( child.Name.EndsWith( "_R" ) ) RightWheel = wheel;
			}
			return;
		}

		LeftWheel = SpawnWheel( $"{GameObject.Name}_L", Width / 2f, isLeftSide: true );
		RightWheel = SpawnWheel( $"{GameObject.Name}_R", -Width / 2f, isLeftSide: false );
	}


	private VehicleWheel SpawnWheel( string name, float yOffset, bool isLeftSide )
	{
		var wheelGo = new GameObject( true, name );
		wheelGo.SetParent( GameObject );
		wheelGo.LocalPosition = new Vector3( 0f, yOffset, 0f );
		// Left wheel rotated 180° so both wheels face outward
		wheelGo.LocalRotation = isLeftSide ? Rotation.FromAxis( Vector3.Up, 180f ) : Rotation.Identity;
		// No need to tag wheels — they inherit "vehicle" from the root GameObject.
		// Wheel raycasts already use .WithoutTags("vehicle") to avoid self-collision.

		// Add wheel model if specified
		if ( WheelModel != null )
		{
			var renderer = wheelGo.Components.Create<ModelRenderer>();
			renderer.Model = WheelModel;
		}

		// Configure wheel physics
		var wheel = wheelGo.Components.Create<VehicleWheel>();
		wheel.SuspensionLength = SuspensionLength;
		wheel.AutoDetectRadius = AutoDetectRadius;
		// Only set radius manually if auto-detect is off (OnAwake already detected from model bounds)
		if ( !AutoDetectRadius )
			wheel.Radius = WheelRadius;
		wheel.IsSteerable = Type.HasFlag( AxleType.Steering );
		wheel.IsDriven = Type.HasFlag( AxleType.Powered );
		wheel.IsHandbrakeWheel = IsHandbrake;
		wheel.ReverseSpinDirection = isLeftSide; // Flip spin to compensate for 180° model rotation
		wheel.Stiffness = Stiffness; // Synchronize stiffness downward
		wheel.LateralFriction = LateralFriction;
		wheel.LateralGripStiffness = LateralGripStiffness;
		wheel.LongitudinalFriction = LongitudinalFriction;

		return wheel;
	}

	/// <summary>
	/// Applies anti-roll torque to resist body roll on this axle.
	/// Called by VehicleController each OnFixedUpdate.
	/// </summary>
	public void ApplyAntiRoll( Rigidbody body )
	{
		if ( LeftWheel == null || RightWheel == null ) return;
		if ( !LeftWheel.IsGrounded && !RightWheel.IsGrounded ) return;

		// compressionDiff = left − right.
		// Right more compressed (car leaning right) → compressionDiff < 0
		// → torque = Forward * negative = negative roll around Forward = right side rises. ✓
		var compressionDiff = LeftWheel.SuspensionCompression - RightWheel.SuspensionCompression;
		var torque = body.WorldRotation.Forward * compressionDiff * AntiRollStrength;
		body.PhysicsBody.ApplyTorque( torque );
	}

	public List<VehicleWheel> GetWheels()
	{
		var wheels = new List<VehicleWheel>( 2 );
		if ( LeftWheel != null ) wheels.Add( LeftWheel );
		if ( RightWheel != null ) wheels.Add( RightWheel );
		return wheels;
	}

	protected override void DrawGizmos()
	{
		// Draw axle bar and wheel outlines for editor visualization
		var leftPos = new Vector3( 0f, Width / 2f, 0f );
		var rightPos = new Vector3( 0f, -Width / 2f, 0f );

		// Axle bar
		Gizmo.Draw.Color = Color.Yellow;
		Gizmo.Draw.Line( leftPos, rightPos );

		// Wheel circles — use runtime radius if wheels exist (auto-detected), else fallback
		var radius = LeftWheel?.Radius ?? WheelRadius;
		Gizmo.Draw.Color = IsSteering ? Color.Green : (IsPowered ? Color.Cyan : Color.Gray);

		DrawWheelGizmo( leftPos, radius );
		DrawWheelGizmo( rightPos, radius );

		// Suspension travel lines (centered on axle position)
		var halfTravel = SuspensionLength * 0.5f;
		Gizmo.Draw.Color = Color.Red.WithAlpha( 0.5f );
		Gizmo.Draw.Line( leftPos + Vector3.Up * halfTravel, leftPos + Vector3.Down * halfTravel );
		Gizmo.Draw.Line( rightPos + Vector3.Up * halfTravel, rightPos + Vector3.Down * halfTravel );

		// Label
		Gizmo.Draw.Color = Color.White;
		var label = Type == 0 ? "Passive" :
			(IsSteering && IsPowered) ? "Steer+Drive" :
			IsSteering ? "Steering" : "Powered";
		if ( IsHandbrake ) label += " [HB]";
		Gizmo.Draw.ScreenText( label, new Vector2( 0.5f, 0f ) );
	}

	private void DrawWheelGizmo( Vector3 center, float radius )
	{
		// Draw a circle in the YZ plane (perpendicular to axle direction)
		const int segments = 16;
		for ( int i = 0; i < segments; i++ )
		{
			var angle1 = (float)i / segments * MathF.Tau;
			var angle2 = (float)(i + 1) / segments * MathF.Tau;
			var p1 = center + new Vector3( MathF.Cos( angle1 ), 0f, MathF.Sin( angle1 ) ) * radius;
			var p2 = center + new Vector3( MathF.Cos( angle2 ), 0f, MathF.Sin( angle2 ) ) * radius;
			Gizmo.Draw.Line( p1, p2 );
		}
	}
}
