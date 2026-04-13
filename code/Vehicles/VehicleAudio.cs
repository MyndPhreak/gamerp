using Sandbox;
using System;

namespace GameRP.Vehicles;

public sealed class VehicleAudio : Component, Component.ICollisionListener
{
	[Property, Group( "Configuration" )] public VehicleController Controller { get; set; }
	[Property, Group( "Configuration" )] public bool IsElectric { get; set; } = false;
	
	[Property, Group( "Engine Sounds" )] public SoundEvent EngineSound { get; set; }
	[Property, Group( "Engine Sounds" ), ShowIf( "IsElectric", false )] public SoundEvent ShiftSound { get; set; }
	[Property, Group( "Engine Sounds" )] public SoundEvent IgnitionSound { get; set; }
	[Property, Group( "Engine Sounds" )] public SoundEvent ShutoffSound { get; set; }
	
	[Property, Group( "Impact Sounds" )] public SoundEvent CrashSound { get; set; }
	[Property, Group( "Impact Sounds" )] public float CrashCooldown { get; set; } = 0.2f;

	[Property, Group( "Brake Sounds" )] public SoundEvent AirbrakeReleaseSound { get; set; }
	[Property, Group( "Brake Sounds" )] public SoundEvent BrakeSquealSound { get; set; }

	[Property, Group( "Tuning" )] public float MinimumPitch { get; set; } = 0.8f;
	[Property, Group( "Tuning" )] public float MaximumPitch { get; set; } = 2.0f;
	
	private SoundHandle _engineHandle;
	private int _lastGear = 1;
	private TimeSince _timeSinceLastCrash;
	private bool _wasDriven = false;
	private SoundHandle _ignitionHandle;
	private SoundHandle _brakeSquealHandle;
	private bool _wasHandbrakeEngaged = true;

	protected override void OnAwake()
	{
		if ( Controller == null )
			Controller = Components.Get<VehicleController>();
	}

	protected override void OnStart()
	{
		if ( EngineSound != null )
		{
			_engineHandle = Sound.Play( EngineSound, Transform.Position );
			if ( _engineHandle != null )
			{
				_engineHandle.ListenLocal = false; // Make sure it's spatialized 3D
			}
		}
	}

	protected override void OnUpdate()
	{
		if ( _engineHandle != null )
		{
			// Keep the sound localized to the vehicle
			_engineHandle.Position = Transform.Position;
		}
			
		UpdateEngineSound();
		UpdateBrakeSounds();
	}

	private void UpdateBrakeSounds()
	{
		if ( Controller == null ) return;

		// Treat empty cars as having handbrake effectively on
		bool hasHandbrake = Controller.HandbrakeInput > 0.5f || Controller.Driver == null;
		
		// If we just released the handbrake (e.g. driving off)
		if ( _wasHandbrakeEngaged && !hasHandbrake )
		{
			if ( AirbrakeReleaseSound != null )
			{
				Sound.Play( AirbrakeReleaseSound, Transform.Position );
			}
		}
		_wasHandbrakeEngaged = hasHandbrake;

		// Brake squeal logic - only when moving at decent speed
		bool isBraking = Controller.BrakeInput > 0.1f || Controller.HandbrakeInput > 0.1f;
		bool isFastEnough = MathF.Abs(Controller.CurrentSpeed) > 150f;

		if ( isBraking && isFastEnough && Controller.Driver != null )
		{
			if ( _brakeSquealHandle == null && BrakeSquealSound != null )
			{
				_brakeSquealHandle = Sound.Play( BrakeSquealSound, Transform.Position );
			}
			
			if ( _brakeSquealHandle != null )
			{
				_brakeSquealHandle.Position = Transform.Position;
				// Smoothly increase squeal volume based on brake pressure
				float pressure = MathF.Max( Controller.BrakeInput, Controller.HandbrakeInput );
				float targetVolume = MathX.Lerp( 0.2f, 1.0f, pressure );
				_brakeSquealHandle.Volume = MathX.Lerp( _brakeSquealHandle.Volume, targetVolume, Time.Delta * 10f );
			}
		}
		else
		{
			if ( _brakeSquealHandle != null )
			{
				_brakeSquealHandle.Stop();
				_brakeSquealHandle = null;
			}
		}
	}

	private void UpdateEngineSound()
	{
		if ( Controller == null ) return;

		bool isDriven = Controller.Driver != null;

		// Handle Driver Entry (Ignition)
		if ( isDriven && !_wasDriven )
		{
			if ( IgnitionSound != null ) 
			{
				_ignitionHandle = Sound.Play( IgnitionSound, Transform.Position );
			}
		}
		// Handle Driver Exit (Shutoff)
		else if ( !isDriven && _wasDriven )
		{
			if ( ShutoffSound != null ) Sound.Play( ShutoffSound, Transform.Position );
			
			// Kill the continuous engine loop immediately
			_engineHandle?.Stop();
			_engineHandle = null;
			
			// Also kill ignition if they get out before it finishes
			_ignitionHandle?.Stop();
			_ignitionHandle = null;
		}

		_wasDriven = isDriven;

		// When nobody is driving, the engine loop is fully dead
		if ( !isDriven ) return;

		// Wait for the ignition sound to finish before starting the main engine loop
		if ( _ignitionHandle != null && _ignitionHandle.IsPlaying ) return;

		// Safely restart the sound if it finished (e.g., if a non-looping .wav was provided instead of a looping .sound event)
		if ( EngineSound != null && (_engineHandle == null || !_engineHandle.IsPlaying) )
		{
			_engineHandle = Sound.Play( EngineSound, Transform.Position );
			if ( _engineHandle != null ) _engineHandle.ListenLocal = false;
		}

		if ( _engineHandle == null ) return;

		float targetPitch;
		float targetVolume;

		if ( IsElectric )
		{
			// EVs don't usually map to geared RPM, they map to raw velocity
			var speedFrac = Math.Clamp( MathF.Abs(Controller.CurrentSpeed) / 1500f, 0f, 1f );
			targetPitch = MathX.Lerp( MinimumPitch, MaximumPitch, speedFrac );
			targetVolume = MathX.Lerp( 0.4f, 1.0f, speedFrac );
		}
		else
		{
			// True Internal Combustion Engine mapping using physical RPM 
			if ( Controller.CurrentGear > _lastGear )
			{
				if ( ShiftSound != null ) Sound.Play( ShiftSound, Transform.Position );
			}
			_lastGear = Controller.CurrentGear;

			// Normalize physical Engine RPM into a 0.0 -> 1.0 fraction
			float rpmRange = Controller.MaxRPM - Controller.IdleRPM;
			float rpmFrac = Math.Clamp( (Controller.EngineRPM - Controller.IdleRPM) / (rpmRange > 0f ? rpmRange : 1f), 0f, 1f );
			
			targetPitch = MathX.Lerp( MinimumPitch, MaximumPitch, rpmFrac );
			
			// Add slight volume rumbling boost if throttle is applied
			bool hasThrottleInput = Input.Down( "Forward" ) || Input.Down( "Backward" );
			targetVolume = MathX.Lerp( 0.5f, 1.0f, rpmFrac ) + (hasThrottleInput ? 0.2f : 0f);
		}

		// Smoothly lerp towards target pitch and volume, but violently fast to avoid double-lerping sluggishness.
		// Clamp t to [0,1] to prevent overshoot when framerate is low (e.g. Delta * 30 > 1 below ~30fps causes crackling).
		_engineHandle.Pitch = MathX.Lerp( _engineHandle.Pitch, targetPitch, Math.Clamp( Time.Delta * 30f, 0f, 1f ) );
		_engineHandle.Volume = Math.Clamp( MathX.Lerp( _engineHandle.Volume, Math.Clamp( targetVolume, 0f, 1f ), Math.Clamp( Time.Delta * 15f, 0f, 1f ) ), 0f, 1f );
	}

	protected override void OnDestroy()
	{
		_engineHandle?.Stop();
		_brakeSquealHandle?.Stop();
	}

	public void OnCollisionStart( Collision collision )
	{
		if ( CrashSound == null ) return;
		if ( _timeSinceLastCrash < CrashCooldown ) return;

		// Correctly use the magnitude of the impact along the collision normal, 
		// otherwise simply driving fast triggers a "massive collision" on microscopic bumps!
		var impactSpeed = collision.Contact.Speed.Length;
		
		// Filter out gentle bumps or scraped bounds
		if ( impactSpeed < 100f ) return;

		_timeSinceLastCrash = 0f;

		// Limit the hit max mapping 
		var volumeFraction = Math.Clamp( (impactSpeed - 100f) / 800f, 0.1f, 1f );
		
		var crashHandle = Sound.Play( CrashSound, Transform.Position );
		if ( crashHandle != null )
		{
			crashHandle.Volume = volumeFraction;
		}
	}
}
