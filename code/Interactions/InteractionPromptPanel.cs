using Sandbox;

namespace GameRP.Interactions;

/// <summary>
/// Component that manages a WorldPanel for rendering interaction prompts in 3D space.
/// This wraps the S&Box WorldPanel component and provides a custom UI.
/// </summary>
public sealed class InteractionPromptPanel : Component
{
	public string InteractionText { get; set; } = "Interact";
	public float HoldProgress { get; set; } = 0f;
	public float HoldDuration { get; set; } = 0f;
	public Color PromptColor { get; set; } = Color.White;
	public bool IsVisible { get; set; } = false;

	/// <summary>
	/// Size of the world panel in pixels
	/// </summary>
	[Property] public Vector2 PanelSize { get; set; } = new Vector2( 400, 150 );

	private Sandbox.WorldPanel _worldPanel;
	private InteractionPromptUI _ui;

	protected override void OnAwake()
	{
		// Create the WorldPanel component
		_worldPanel = Components.GetOrCreate<Sandbox.WorldPanel>();
		_worldPanel.PanelSize = PanelSize;
		_worldPanel.InteractionRange = 0; // Don't use WorldPanel's own interaction, we handle it ourselves

		// Create and add the UI
		_ui = new InteractionPromptUI( this );
		_worldPanel.Panel.AddChild( _ui );
	}

	protected override void OnUpdate()
	{
		if ( _ui != null )
		{
			_ui.StateHasChanged();
		}

		if ( _worldPanel != null && _worldPanel.Panel != null )
		{
			_worldPanel.Panel.Style.Opacity = IsVisible ? 1f : 0f;
			_worldPanel.Panel.Style.PointerEvents = IsVisible ? Sandbox.UI.PointerEvents.None : Sandbox.UI.PointerEvents.None;
		}
	}

	public void UpdateState( string text, float progress, float duration, Color color )
	{
		InteractionText = text;
		HoldProgress = progress;
		HoldDuration = duration;
		PromptColor = color;
	}
}
