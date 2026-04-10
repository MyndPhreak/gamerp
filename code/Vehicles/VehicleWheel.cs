using Sandbox;
using System;

namespace GameRP.Vehicles;

public sealed class VehicleWheel : Component
{
	[Property] public float SuspensionLength { get; set; } = 20f;
	[Property] public float Radius { get; set; } = 14f;
	[Property] public bool IsSteerable { get; set; } = false;
	[Property] public bool IsDriven { get; set; } = false;

	[Property, ReadOnly] public bool IsGrounded { get; private set; }
	[Property, ReadOnly] public float GroundDistance { get; private set; }
	[Property, ReadOnly] public float SuspensionCompression { get; private set; }

	/// <summary>
	/// Set by VehicleController each frame — current forward speed for spin.
	/// </summary>
	public float CurrentSpeed { get; set; }

	/// <summary>
	/// Set by VehicleController each frame — current steer angle in degrees.
	/// </summary>
	public float SteerAngle { get; set; }

	private ModelRenderer _wheelModel;
	private float _spinAngle;
	private Vector3 _attachLocalPos;   // editor-placed local position — suspension base
	private Rotation _baseLocalRot;    // editor-placed local rotation — spin/steer applied on top

	protected override void OnAwake()
	{
		_wheelModel = Components.Get<ModelRenderer>();
		if ( _wheelModel == null )
			Log.Warning( "[VehicleWheel] No ModelRenderer found — wheel visuals will not update" );

		_attachLocalPos = LocalPosition;
		_baseLocalRot = LocalRotation;
	}

	protected override void OnUpdate()
	{
		UpdateSuspension();
		UpdateVisuals();
	}

	private void UpdateSuspension()
	{
		// Raycast from the attach point (world space), not the current WorldPosition which
		// includes the suspension offset — avoids a feedback loop causing bouncing
		var parent = GameObject.Parent;
		var rayOrigin = parent.WorldPosition + parent.WorldRotation * _attachLocalPos;
		var rayDirection = -parent.WorldRotation.Up;
		var rayLength = SuspensionLength + Radius;

		var trace = Scene.Trace
			.Ray( new Ray( rayOrigin, rayDirection ), rayLength )
			.WithoutTags( "vehicle" )
			.Run();

		if ( trace.Hit )
		{
			IsGrounded = true;
			GroundDistance = trace.Distance;
			SuspensionCompression = 1f - (trace.Distance - Radius).Clamp( 0, SuspensionLength ) / SuspensionLength;
		}
		else
		{
			IsGrounded = false;
			GroundDistance = rayLength;
			SuspensionCompression = 0f;
		}
	}

	private void UpdateVisuals()
	{
		// Move the whole wheel GO up/down for suspension travel,
		// offset from the editor-placed attach point
		var suspensionOffset = IsGrounded
			? GroundDistance - Radius
			: SuspensionLength;

		LocalPosition = _attachLocalPos + Vector3.Down * suspensionOffset;

		if ( _wheelModel == null ) return;

		// Spin wheel based on speed, applied on top of editor base rotation
		_spinAngle = (_spinAngle + CurrentSpeed * Time.Delta * (360f / (MathF.Tau * Radius))) % 360f;
		var spinRotation = Rotation.FromAxis( Vector3.Right, _spinAngle );

		// Steer rotation for steerable wheels
		var steerRotation = IsSteerable
			? Rotation.FromAxis( Vector3.Up, SteerAngle )
			: Rotation.Identity;

		LocalRotation = _baseLocalRot * steerRotation * spinRotation;
	}
}
