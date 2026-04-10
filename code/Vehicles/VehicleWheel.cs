using Sandbox;

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

	protected override void OnAwake()
	{
		_wheelModel = Components.Get<ModelRenderer>();
	}

	protected override void OnUpdate()
	{
		UpdateSuspension();
		UpdateVisuals();
	}

	private void UpdateSuspension()
	{
		var rayOrigin = GameObject.Parent.WorldPosition + GameObject.LocalPosition;
		var rayDirection = -GameObject.Parent.WorldRotation.Up;
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
		if ( _wheelModel == null ) return;

		// Position wheel at suspension height
		var suspensionOffset = IsGrounded
			? GroundDistance - Radius
			: SuspensionLength;

		var localDown = -Vector3.Up;
		_wheelModel.LocalPosition = localDown * suspensionOffset;

		// Spin wheel based on speed
		_spinAngle += CurrentSpeed * Time.Delta * (360f / (2f * MathF.PI * Radius));
		var spinRotation = Rotation.FromAxis( Vector3.Right, _spinAngle );

		// Steer rotation for front wheels
		var steerRotation = IsSteerable
			? Rotation.FromAxis( Vector3.Up, SteerAngle )
			: Rotation.Identity;

		_wheelModel.LocalRotation = steerRotation * spinRotation;
	}
}
