using Sandbox;
using System.Linq;

namespace GameRP.Interactions;

/// <summary>
/// Manages interaction raycasting and detection.
/// Attach this to the player or camera GameObject.
/// </summary>
public sealed class InteractionManager : Component
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
	/// The input action name for interaction (default: "use")
	/// </summary>
	[Property] public string InteractButton { get; set; } = "use";

	/// <summary>
	/// Debug visualization
	/// </summary>
	[Property] public bool ShowDebug { get; set; } = false;

	private Interactable _currentInteractable;
	private Interactable _previousInteractable;

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
			// Check if the hit GameObject has an Interactable component
			var interactable = trace.GameObject.Components.GetInAncestorsOrSelf<Interactable>();

			if ( interactable != null && interactable.Enabled )
			{
				// Check if in range
				if ( interactable.IsInRange( camera.WorldPosition ) )
				{
					_currentInteractable = interactable;
				}
			}
		}

		// Update interactable states
		UpdateInteractableStates();

		// Handle input
		HandleInput();
	}

	private void UpdateInteractableStates()
	{
		// Clear previous interactable
		if ( _previousInteractable != null && _previousInteractable != _currentInteractable )
		{
			_previousInteractable.IsLookingAt = false;
			_previousInteractable.IsHolding = false;
		}

		// Set current interactable
		if ( _currentInteractable != null )
		{
			_currentInteractable.IsLookingAt = true;
		}

		_previousInteractable = _currentInteractable;
	}

	private void HandleInput()
	{
		if ( _currentInteractable == null )
			return;

		// Check if interact button is pressed
		bool isInteracting = Input.Down( InteractButton );

		if ( _currentInteractable.HoldDuration > 0 )
		{
			// Hold-to-interact
			_currentInteractable.IsHolding = isInteracting;
		}
		else if ( Input.Pressed( InteractButton ) )
		{
			// Instant interact
			_currentInteractable.OnInteract?.Invoke();
			Log.Info( $"[InteractionManager] Instant interact: {_currentInteractable.InteractionText}" );
		}
	}

	protected override void OnDisabled()
	{
		// Clear all interactable states when disabled
		if ( _currentInteractable != null )
		{
			_currentInteractable.IsLookingAt = false;
			_currentInteractable.IsHolding = false;
		}
	}
}
