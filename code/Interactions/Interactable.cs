using Sandbox;
using System;

namespace GameRP.Interactions;

/// <summary>
/// Component that makes a GameObject interactable.
/// Shows a world-space UI prompt and supports hold-to-interact functionality.
/// </summary>
public sealed class Interactable : Component
{
	/// <summary>
	/// Display text shown on the interaction prompt
	/// </summary>
	[Property] public string InteractionText { get; set; } = "Press E to interact";

	/// <summary>
	/// Maximum distance the player can interact from (in units)
	/// </summary>
	[Property] public float InteractionDistance { get; set; } = 200f;

	/// <summary>
	/// Time in seconds the player must hold the interact key (0 = instant)
	/// </summary>
	[Property] public float HoldDuration { get; set; } = 0f;

	/// <summary>
	/// Color tint for the interaction prompt
	/// </summary>
	[Property] public Color PromptColor { get; set; } = Color.White;

	/// <summary>
	/// Event triggered when interaction completes
	/// </summary>
	public Action OnInteract { get; set; }

	/// <summary>
	/// Current hold progress (0-1)
	/// </summary>
	public float HoldProgress { get; private set; } = 0f;

	/// <summary>
	/// Whether a player is currently looking at this interactable
	/// </summary>
	public bool IsLookingAt { get; set; } = false;

	/// <summary>
	/// Whether a player is currently holding the interact key
	/// </summary>
	public bool IsHolding { get; set; } = false;

	private InteractionPromptPanel _promptPanel;

	protected override void OnStart()
	{
		// Create the world panel for this interactable
		CreatePromptPanel();
	}

	protected override void OnUpdate()
	{
		// Update hold progress
		if ( IsHolding && HoldDuration > 0 )
		{
			HoldProgress += Time.Delta / HoldDuration;

			if ( HoldProgress >= 1f )
			{
				// Interaction complete
				CompleteInteraction();
			}
		}
		else if ( !IsHolding && HoldProgress > 0 )
		{
			// Reset progress when not holding
			HoldProgress -= Time.Delta * 2f; // Reset faster than fill
			HoldProgress = Math.Max( 0f, HoldProgress );
		}
	}

	private void CreatePromptPanel()
	{
		if ( _promptPanel != null )
			return;

		// Check if a prompt panel already exists as a child (e.g., saved in the scene file)
		_promptPanel = Components.GetInChildren<InteractionPromptPanel>();
		if ( _promptPanel != null )
		{
			_promptPanel.Interactable = this;
			return;
		}

		var panelGO = new GameObject( true, "InteractionPrompt" );
		panelGO.SetParent( GameObject );

		_promptPanel = panelGO.Components.Create<InteractionPromptPanel>();
		_promptPanel.PanelSize = new Vector2( 400, 150 );
		_promptPanel.Interactable = this;
	}

	private void CompleteInteraction()
	{
		Log.Info( $"[Interactable] Interaction completed: {InteractionText}" );
		OnInteract?.Invoke();

		// Reset state
		HoldProgress = 0f;
		IsHolding = false;
	}

	/// <summary>
	/// Check if this interactable is within range of a position
	/// </summary>
	public bool IsInRange( Vector3 position )
	{
		return WorldPosition.Distance( position ) <= InteractionDistance;
	}

	protected override void OnDestroy()
	{
		_promptPanel?.GameObject?.Destroy();
	}
}
