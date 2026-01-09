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
	private Label _textLabel;

	public InteractionPromptUI( InteractionPromptPanel promptPanel )
	{
		_promptPanel = promptPanel;

		StyleSheet.Load( "/code/Interactions/InteractionPromptUI.scss" );

		AddClass( "interaction-ui" );

		BuildUI();
	}

	private void BuildUI()
	{
		_container = Add.Panel( "interaction-container" );
		_content = _container.Add.Panel( "interaction-content" );

		// Progress ring (for hold-to-interact)
		_progressRing = _content.Add.Panel( "progress-ring" );
		_progressRing.AddClass( "hidden" );

		var svg = _progressRing.Add.Panel( "svg-container" );
		svg.Style.Width = 80;
		svg.Style.Height = 80;

		// We'll draw the progress using CSS instead of SVG for simplicity
		var progressBg = _progressRing.Add.Panel( "progress-ring-bg" );
		var progressFill = _progressRing.Add.Panel( "progress-ring-fill" );

		_keyLabel = _progressRing.Add.Label( "E", "progress-key" );

		// Instant key (for instant interact)
		_instantKey = _content.Add.Panel( "instant-key" );
		_instantKey.Add.Label( "E" );

		// Text label
		_textLabel = _content.Add.Label( "Interact", "interaction-text" );
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

			// Update progress ring
			var progress = _promptPanel.HoldProgress;
			var progressFill = _progressRing.GetChild( 2 ); // progress-ring-fill

			if ( progressFill != null )
			{
				// Calculate stroke-dashoffset for circular progress
				var circumference = 2f * MathF.PI * 34f;
				var offset = circumference * (1f - progress);

				progressFill.Style.Set( "stroke-dashoffset", $"{offset}px" );
			}
		}
		else
		{
			_progressRing.SetClass( "hidden", true );
			_instantKey.SetClass( "hidden", false );
		}
	}
}
