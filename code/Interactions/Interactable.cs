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
	/// Local offset from the GameObject's position for the UI panel
	/// </summary>
	[Property] public Vector3 PanelOffset { get; set; } = new Vector3( 0, 0, 50 );

	/// <summary>
	/// Scale of the world panel (larger = bigger UI)
	/// </summary>
	[Property] public float PanelScale { get; set; } = 0.3f;

	/// <summary>
	/// Whether the panel should always face the camera
	/// </summary>
	[Property] public bool Billboard { get; set; } = true;

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
		if ( _promptPanel == null )
			return;

		// Update panel position
		var worldPos = WorldPosition + WorldRotation * PanelOffset;
		_promptPanel.WorldPosition = worldPos;

		// Billboard behavior - face the camera
		if ( Billboard && Scene.Camera != null )
		{
			_promptPanel.WorldRotation = Rotation.LookAt( Scene.Camera.WorldPosition - worldPos );
		}
		else
		{
			_promptPanel.WorldRotation = WorldRotation;
		}

		_promptPanel.WorldScale = PanelScale;

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

		// Update panel visibility and state
		_promptPanel.IsVisible = IsLookingAt;
		_promptPanel.UpdateState( InteractionText, HoldProgress, HoldDuration, PromptColor );
	}

	private void CreatePromptPanel()
	{
		if ( _promptPanel != null )
			return;

		var panelGO = new GameObject( true, "InteractionPrompt" );
		panelGO.SetParent( GameObject );

		_promptPanel = panelGO.Components.Create<InteractionPromptPanel>();
		_promptPanel.PanelSize = new Vector2( 400, 150 );
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
