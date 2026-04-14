using Sandbox;
using System;

namespace GameRP.Vehicles;

public sealed class VehicleAudio : Component, Component.ICollisionListener
{
	[Property, Group( "Configuration" )] public VehicleController Controller { get; set; }
	[Property, Group( "Configuration" )] public bool IsElectric { get; set; } = false;
	
	/// <summary>Looping idle rumble. Plays at constant pitch, fades out as RPM rises.</summary>
	[Property, Group( "Engine Sounds" )] public SoundEvent IdleSound { get; set; }
	/// <summary>Looping drive/rev sound. Fades in as RPM rises, pitch-shifted across a narrow range.</summary>
	[Property, Group( "Engine Sounds" )] public SoundEvent EngineSound { get; set; }
	[Property, Group( "Engine Sounds" ), ShowIf( "IsElectric", false )] public SoundEvent ShiftSound { get; set; }
	[Property, Group( "Engine Sounds" )] public SoundEvent IgnitionSound { get; set; }
	[Property, Group( "Engine Sounds" )] public SoundEvent ShutoffSound { get; set; }
	
	[Property, Group( "Impact Sounds" )] public SoundEvent CrashSound { get; set; }
	[Property, Group( "Impact Sounds" )] public float CrashCooldown { get; set; } = 0.2f;

	[Property, Group( "Brake Sounds" )] public SoundEvent AirbrakeReleaseSound { get; set; }
	[Property, Group( "Brake Sounds" )] public SoundEvent BrakeSquealSound { get; set; }

	/// <summary>Drive loop pitch at idle RPM. Keep close to 1.0 for natural sound.</summary>
	[Property, Group( "Tuning" )] public float MinimumPitch { get; set; } = 0.9f;
	/// <summary>Drive loop pitch at redline. 1.3–1.6 sounds natural; above 1.8 gets chipmunky.</summary>
	[Property, Group( "Tuning" )] public float MaximumPitch { get; set; } = 1.5f;
	
	private SoundHandle _idleHandle;
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
		// Engine sounds are started when a driver enters, not on spawn.
	}

	protected override void OnUpdate()
	{
		// Keep sounds localized to the vehicle
		if ( _idleHandle != null ) _idleHandle.Position = Transform.Position;
		if ( _engineHandle != null ) _engineHandle.Position = Transform.Position;

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

			_idleHandle?.Stop();
			_idleHandle = null;
			_engineHandle?.Stop();
			_engineHandle = null;
			_ignitionHandle?.Stop();
			_ignitionHandle = null;
		}

		_wasDriven = isDriven;

		if ( !isDriven ) return;

		// Wait for the ignition sound to finish before starting the engine loops
		if ( _ignitionHandle != null && _ignitionHandle.IsPlaying ) return;

		// Ensure both loops are playing (restart if a non-looping clip ended)
		if ( IdleSound != null && (_idleHandle == null || !_idleHandle.IsPlaying) )
		{
			_idleHandle = Sound.Play( IdleSound, Transform.Position );
			if ( _idleHandle != null ) _idleHandle.ListenLocal = false;
		}
		if ( EngineSound != null && (_engineHandle == null || !_engineHandle.IsPlaying) )
		{
			_engineHandle = Sound.Play( EngineSound, Transform.Position );
			if ( _engineHandle != null ) _engineHandle.ListenLocal = false;
		}

		// Smooth lerp factor (clamped to prevent overshoot at low framerates)
		float lerpT = Math.Clamp( Time.Delta * 15f, 0f, 1f );

		if ( IsElectric )
		{
			var speedFrac = Math.Clamp( MathF.Abs( Controller.CurrentSpeed ) / 1500f, 0f, 1f );

			if ( _idleHandle != null )
				_idleHandle.Volume = MathX.Lerp( _idleHandle.Volume, 1f - speedFrac, lerpT );

			if ( _engineHandle != null )
			{
				_engineHandle.Pitch = MathX.Lerp( _engineHandle.Pitch, MathX.Lerp( MinimumPitch, MaximumPitch, speedFrac ), lerpT );
				_engineHandle.Volume = MathX.Lerp( _engineHandle.Volume, speedFrac, lerpT );
			}
		}
		else
		{
			// Shift sound
			if ( Controller.CurrentGear > _lastGear )
			{
				if ( ShiftSound != null ) Sound.Play( ShiftSound, Transform.Position );
			}
			_lastGear = Controller.CurrentGear;

			// Normalize Engine RPM into 0–1
			float rpmRange = Controller.MaxRPM - Controller.IdleRPM;
			float rpmFrac = Math.Clamp( (Controller.EngineRPM - Controller.IdleRPM) / (rpmRange > 0f ? rpmRange : 1f), 0f, 1f );

			bool hasThrottle = Input.Down( "Forward" ) || Input.Down( "Backward" );

			// --- Idle layer: constant pitch, fades out as RPM rises ---
			if ( _idleHandle != null )
			{
				float idleVolume = (1f - rpmFrac) * (hasThrottle ? 0.7f : 1f);
				_idleHandle.Volume = MathX.Lerp( _idleHandle.Volume, Math.Clamp( idleVolume, 0f, 1f ), lerpT );
			}

			// --- Drive layer: pitch rises with RPM, fades in as RPM rises ---
			if ( _engineHandle != null )
			{
				float drivePitch = MathX.Lerp( MinimumPitch, MaximumPitch, rpmFrac );
				float driveVolume = rpmFrac + (hasThrottle ? 0.15f : 0f);

				_engineHandle.Pitch = MathX.Lerp( _engineHandle.Pitch, drivePitch, lerpT );
				_engineHandle.Volume = MathX.Lerp( _engineHandle.Volume, Math.Clamp( driveVolume, 0f, 1f ), lerpT );
			}
		}
	}

	protected override void OnDestroy()
	{
		_idleHandle?.Stop();
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
