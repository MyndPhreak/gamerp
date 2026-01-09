using Sandbox;
using Sandbox.UI;

namespace GameRP.Interactions;

/// <summary>
/// Component that manages a WorldPanel for rendering interaction prompts in 3D space.
/// This creates a simple WorldPanel with a custom RootPanel UI.
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
	private InteractionPromptUI _promptUI;

	protected override void OnAwake()
	{
		// Create the WorldPanel component
		_worldPanel = Components.GetOrCreate<Sandbox.WorldPanel>();
		_worldPanel.PanelSize = PanelSize;
		_worldPanel.InteractionRange = 0; // Don't use WorldPanel's own interaction, we handle it ourselves

		// The WorldPanel will automatically create a RootPanel
		// We'll add our UI to it in OnStart after it's ready
	}

	protected override void OnStart()
	{
		// Wait one frame for WorldPanel to initialize its RootPanel
		if ( _worldPanel?.RootPanel != null )
		{
			// Create and add our custom UI
			_promptUI = new InteractionPromptUI( this );
			_worldPanel.RootPanel.AddChild( _promptUI );
		}
	}

	protected override void OnUpdate()
	{
		if ( _promptUI != null )
		{
			_promptUI.StateHasChanged();
		}

		// Update visibility
		if ( _worldPanel?.RootPanel != null )
		{
			_worldPanel.RootPanel.Style.Opacity = IsVisible ? 1f : 0f;
			_worldPanel.RootPanel.Style.PointerEvents = PointerEvents.None; // Never intercept mouse
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
