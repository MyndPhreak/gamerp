using Sandbox;
using Sandbox.UI;

namespace GameRP.Interactions;

/// <summary>
/// WorldPanel that renders the interaction prompt UI in 3D space
/// </summary>
public sealed class InteractionPromptPanel : WorldPanel
{
	public string InteractionText { get; private set; } = "Interact";
	public float HoldProgress { get; private set; } = 0f;
	public float HoldDuration { get; private set; } = 0f;
	public Color PromptColor { get; private set; } = Color.White;
	public bool IsVisible { get; set; } = false;

	protected override void OnEnabled()
	{
		base.OnEnabled();

		// Set up the world panel
		StyleSheet = StyleSheet.FromFile( "/code/Interactions/InteractionPrompt.razor.scss" );

		// Add the UI component
		AddChild<InteractionPrompt>();
	}

	public void UpdateState( string text, float progress, float duration, Color color )
	{
		InteractionText = text;
		HoldProgress = progress;
		HoldDuration = duration;
		PromptColor = color;

		// Update visibility
		Style.Opacity = IsVisible ? 1f : 0f;
		Style.PointerEvents = IsVisible ? PointerEvents.All : PointerEvents.None;
	}

	protected override int BuildHash()
	{
		return System.HashCode.Combine( InteractionText, HoldProgress, IsVisible );
	}
}
