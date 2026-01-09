using Sandbox;

namespace GameRP.Interactions;

/// <summary>
/// Component that manages the interaction prompt WorldPanel.
/// This is a simple wrapper that the Interactable creates.
/// </summary>
public sealed class InteractionPromptPanel : Component
{
	[Property] public Vector2 PanelSize { get; set; } = new Vector2( 400, 150 );
	[Property] public Interactable Interactable { get; set; }

	private Sandbox.WorldPanel _worldPanel;
	private InteractionPromptWorldPanel _ui;

	protected override void OnAwake()
	{
		// Create WorldPanel component
		_worldPanel = Components.GetOrCreate<Sandbox.WorldPanel>();
		_worldPanel.PanelSize = PanelSize;

		// Create the UI component on the same GameObject
		_ui = Components.GetOrCreate<InteractionPromptWorldPanel>();
		_ui.Interactable = Interactable;
	}
}
