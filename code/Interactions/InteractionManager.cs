using Sandbox;
using System.Linq;

namespace GameRP.Interactions;

/// <summary>
/// Manages interaction raycasting and detection.
/// Attach this to the player as a sibling of PlayerController.
/// Hooks into PlayerController.IEvents so the built-in use system
/// routes through to our Interactable components.
/// </summary>
public sealed class InteractionManager : Component, PlayerController.IEvents
{
	/// <summary>
	/// The camera to raycast from (if null, uses Scene.Camera)
	/// </summary>
	[Property] public CameraComponent Camera { get; set; }

	/// <summary>
	/// Maximum raycast distance
	/// </summary>
	[Property] public float MaxRaycastDistance { get; set; } = 500f;

	/// <summary>
	/// Debug visualization
	/// </summary>
	[Property] public bool ShowDebug { get; set; } = false;

	private Interactable _currentInteractable;
	private Interactable _previousInteractable;
	private bool _isPressing;

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		// Get the camera
		var camera = Camera ?? Scene.Camera;
		if ( camera == null )
			return;

		// Raycast from camera
		var ray = new Ray( camera.WorldPosition, camera.WorldRotation.Forward );
		var trace = Scene.Trace
			.Ray( ray, MaxRaycastDistance )
			.WithoutTags( "player" )
			.Run();

		// Debug visualization
		if ( ShowDebug )
		{
			Gizmo.Draw.Color = trace.Hit ? Color.Green : Color.Red;
			Gizmo.Draw.Line( ray.Position, trace.Hit ? trace.HitPosition : ray.Position + ray.Forward * MaxRaycastDistance );

			if ( trace.Hit )
			{
				Gizmo.Draw.Color = Color.Yellow;
				Gizmo.Draw.LineSphere( trace.HitPosition, 5f );
			}
		}

		// Find interactable
		_currentInteractable = null;

		if ( trace.Hit && trace.GameObject != null )
		{
			var interactable = trace.GameObject.Components.GetInAncestorsOrSelf<Interactable>();

			if ( interactable != null && interactable.Enabled )
			{
				if ( interactable.IsInRange( camera.WorldPosition ) )
				{
					_currentInteractable = interactable;
				}
			}
		}

		// Update interactable states
		UpdateInteractableStates();

		// Update hold state while pressing
		if ( _isPressing && _currentInteractable != null && _currentInteractable.HoldDuration > 0 )
		{
			_currentInteractable.IsHolding = true;
		}
	}

	/// <summary>
	/// Called by PlayerController when it finds a GameObject in its use trace.
	/// We return the Interactable if one exists, telling PlayerController it's usable.
	/// </summary>
	public Component GetUsableComponent( GameObject go )
	{
		var interactable = go.Components.GetInAncestorsOrSelf<Interactable>();
		if ( interactable != null && interactable.Enabled && interactable.IsInRange( (Camera ?? Scene.Camera).WorldPosition ) )
		{
			return interactable;
		}

		return null;
	}

	/// <summary>
	/// PlayerController started pressing use on a target.
	/// </summary>
	public void StartPressing( Component target )
	{
		if ( target is not Interactable interactable )
			return;

		_isPressing = true;

		if ( interactable.HoldDuration <= 0 )
		{
			// Instant interact
			interactable.OnInteract?.Invoke();
			Log.Info( $"[InteractionManager] Instant interact: {interactable.InteractionText}" );
		}
	}

	/// <summary>
	/// PlayerController stopped pressing use.
	/// </summary>
	public void StopPressing( Component target )
	{
		_isPressing = false;

		if ( target is Interactable interactable )
		{
			interactable.IsHolding = false;
		}
	}

	private void UpdateInteractableStates()
	{
		if ( _previousInteractable != null && _previousInteractable != _currentInteractable )
		{
			_previousInteractable.IsLookingAt = false;
			_previousInteractable.IsHolding = false;
		}

		if ( _currentInteractable != null )
		{
			_currentInteractable.IsLookingAt = true;
		}

		_previousInteractable = _currentInteractable;
	}

	protected override void OnDisabled()
	{
		if ( _currentInteractable != null )
		{
			_currentInteractable.IsLookingAt = false;
			_currentInteractable.IsHolding = false;
		}
	}
}
