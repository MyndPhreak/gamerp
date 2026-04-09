using Sandbox;
using System;

public sealed class DayNightCycle : Component
{
	[Property, Range( 0f, 24f )] public float TimeOfDay { get; set; } = 12.0f;
	[Property] public bool AutoAdvance { get; set; } = false;
	[Property, Range( 0.1f, 60f )] public float TimeScale { get; set; } = 1.0f;
	[Property] public DirectionalLight SunLight { get; set; }
	[Property] public SkyBox2D SkyBox { get; set; }
	[Property, Range( 0.005f, 0.05f )] public float SunSize { get; set; } = 0.01f;
	[Property, Range( 0.005f, 0.05f )] public float MoonSize { get; set; } = 0.02f;
	[Property, Range( 0f, 1f )] public float CloudDensity { get; set; } = 0.3f;

	public float NormalizedTime => TimeOfDay / 24f;

	protected override void OnStart()
	{
		if ( SkyBox == null )
		{
			var skyGo = new GameObject( true, "Sky Box" );
			skyGo.Parent = GameObject;

			SkyBox = skyGo.AddComponent<SkyBox2D>();
			SkyBox.SkyMaterial = Material.Load( "materials/skydome.vmat" );
			Log.Info( $"[DayNight] SkyBox2D created, material: {SkyBox.SkyMaterial != null}" );
		}
	}

	protected override void OnUpdate()
	{
		if ( !IsProxy && AutoAdvance )
		{
			TimeOfDay += (Time.Delta / 60f) * TimeScale;
			if ( TimeOfDay >= 24f )
				TimeOfDay -= 24f;
		}

		float elevation = MathF.Sin( NormalizedTime * MathF.PI * 2f - MathF.PI * 0.5f );
		float dayFactor = MathX.Clamp( elevation * 2f, 0f, 1f );

		UpdateSunLight( elevation, dayFactor );
		UpdateSkyBox( elevation, dayFactor );
	}

	private void UpdateSunLight( float elevation, float dayFactor )
	{
		if ( SunLight == null ) return;

		// Sun rotation
		float sunAngle = NormalizedTime * 360f - 90f;
		SunLight.GameObject.WorldRotation = new Angles( sunAngle, 0f, 0f ).ToRotation();

		// Sun color: warm at horizon, white overhead
		float warmth = 1f - MathF.Abs( elevation );
		float intensity = MathX.Clamp( elevation * 3f, 0.05f, 1f );
		Color tint = Color.Lerp( Color.White, new Color( 1f, 0.5f, 0.2f ), warmth * warmth );
		SunLight.LightColor = tint * intensity;

		// Ambient sky color — prevents pure-black shadows
		Color daySky = new Color( 0.08f, 0.12f, 0.2f );
		Color nightSky = new Color( 0.02f, 0.02f, 0.05f );
		SunLight.SkyColor = Color.Lerp( nightSky, daySky, dayFactor );
	}

	private void UpdateSkyBox( float elevation, float dayFactor )
	{
		if ( SkyBox?.SceneObject == null ) return;

		var so = SkyBox.SceneObject;
		float nightFactor = 1f - dayFactor;

		// Sun direction for the shader
		Vector3 sunDir = SunLight != null
			? SunLight.GameObject.WorldRotation.Forward
			: Vector3.Up;

		// Day sky parameters
		so.Attributes.Set( "SunSize", SunSize );
		so.Attributes.Set( "SkyBrightness", MathX.Lerp( 0.05f, 1f, dayFactor ) );

		// Moon — opposite sun direction
		so.Attributes.Set( "MoonDirection", -sunDir );
		so.Attributes.Set( "MoonSize", MoonSize );
		so.Attributes.Set( "MoonBrightness", nightFactor );

		// Stars fade in at night
		so.Attributes.Set( "StarBrightness", 1.5f * nightFactor );
		so.Attributes.Set( "NightSkyBrightness", 0.3f * nightFactor );

		// Clouds
		so.Attributes.Set( "CloudDensity", CloudDensity );
	}
}
