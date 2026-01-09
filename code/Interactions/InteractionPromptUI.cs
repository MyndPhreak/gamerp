using Sandbox;
using Sandbox.UI;
using System;

namespace GameRP.Interactions;

/// <summary>
/// UI Panel for the interaction prompt shown in world space
/// </summary>
public class InteractionPromptUI : Panel
{
	private InteractionPromptPanel _promptPanel;
	private Panel _container;
	private Panel _content;
	private Panel _progressRing;
	private Panel _instantKey;
	private Label _keyLabel;
	private Label _instantKeyLabel;
	private Label _textLabel;
	private Panel _progressFill;

	public InteractionPromptUI( InteractionPromptPanel promptPanel )
	{
		_promptPanel = promptPanel;

		StyleSheet.Load( "/code/Interactions/InteractionPromptUI.scss" );

		AddClass( "interaction-ui" );

		BuildUI();
	}

	private void BuildUI()
	{
		// Main container
		_container = new Panel();
		_container.AddClass( "interaction-container" );
		AddChild( _container );

		// Content panel
		_content = new Panel();
		_content.AddClass( "interaction-content" );
		_container.AddChild( _content );

		// Progress ring (for hold-to-interact)
		_progressRing = new Panel();
		_progressRing.AddClass( "progress-ring" );
		_progressRing.AddClass( "hidden" );
		_content.AddChild( _progressRing );

		// Progress ring background
		var progressBg = new Panel();
		progressBg.AddClass( "progress-ring-bg" );
		_progressRing.AddChild( progressBg );

		// Progress ring fill (animated)
		_progressFill = new Panel();
		_progressFill.AddClass( "progress-ring-fill" );
		_progressRing.AddChild( _progressFill );

		// Key label in center of ring
		_keyLabel = new Label( "E" );
		_keyLabel.AddClass( "progress-key" );
		_progressRing.AddChild( _keyLabel );

		// Instant key (for instant interact)
		_instantKey = new Panel();
		_instantKey.AddClass( "instant-key" );
		_content.AddChild( _instantKey );

		_instantKeyLabel = new Label( "E" );
		_instantKey.AddChild( _instantKeyLabel );

		// Text label
		_textLabel = new Label( "Interact" );
		_textLabel.AddClass( "interaction-text" );
		_content.AddChild( _textLabel );
	}

	public override void Tick()
	{
		base.Tick();

		if ( _promptPanel == null )
			return;

		// Update text
		_textLabel.Text = _promptPanel.InteractionText;

		// Update visibility based on hold duration
		if ( _promptPanel.HoldDuration > 0 )
		{
			_progressRing.SetClass( "hidden", false );
			_instantKey.SetClass( "hidden", true );

			// Update progress ring rotation
			var progress = _promptPanel.HoldProgress;
			var degrees = -90f + (progress * 360f); // Start at -90deg (top) and rotate clockwise

			// Rotate the progress fill
			_progressFill.Style.Set( "transform", $"rotate({degrees}deg)" );
		}
		else
		{
			_progressRing.SetClass( "hidden", true );
			_instantKey.SetClass( "hidden", false );
		}
	}
}
