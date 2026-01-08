using Sandbox;
using GameRP.Interactions;

namespace GameRP.Interactions.Examples;

/// <summary>
/// Example component showing how to use the Interactable system.
/// This creates a simple test object that changes color when interacted with.
/// </summary>
public sealed class TestInteractable : Component
{
	[Property] public ModelRenderer ModelRenderer { get; set; }
	[Property] public Color NormalColor { get; set; } = Color.Blue;
	[Property] public Color InteractedColor { get; set; } = Color.Green;

	private Interactable _interactable;
	private bool _isActivated = false;
	private int _interactionCount = 0;

	protected override void OnStart()
	{
		// Get or create the Interactable component
		_interactable = Components.Get<Interactable>( true );

		if ( _interactable == null )
		{
			Log.Warning( "[TestInteractable] No Interactable component found!" );
			return;
		}

		// Configure the interactable
		_interactable.InteractionText = "Press E to activate";
		_interactable.HoldDuration = 1f; // 1 second hold
		_interactable.InteractionDistance = 300f;
		_interactable.PromptColor = Color.White;

		// Subscribe to the interaction event
		_interactable.OnInteract += OnInteracted;

		// Set initial color
		UpdateColor();
	}

	private void OnInteracted()
	{
		_interactionCount++;
		_isActivated = !_isActivated;

		Log.Info( $"[TestInteractable] Interacted! Count: {_interactionCount}, Active: {_isActivated}" );

		// Update the interaction text
		if ( _isActivated )
		{
			_interactable.InteractionText = "Press E to deactivate";
		}
		else
		{
			_interactable.InteractionText = "Press E to activate";
		}

		// Update color
		UpdateColor();

		// Play a sound effect (if you have sound system)
		// Sound.Play( "sounds/interact.vsnd", WorldPosition );
	}

	private void UpdateColor()
	{
		if ( ModelRenderer != null )
		{
			ModelRenderer.Tint = _isActivated ? InteractedColor : NormalColor;
		}
	}

	protected override void OnDestroy()
	{
		if ( _interactable != null )
		{
			_interactable.OnInteract -= OnInteracted;
		}
	}
}
