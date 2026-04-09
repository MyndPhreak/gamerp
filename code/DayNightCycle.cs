using Sandbox;
using System;
using System.Linq;

public sealed class DayNightCycle : Component
{
	[Property, Range( 0f, 24f )] public float TimeOfDay { get; set; } = 12.0f;
	[Property] public bool AutoAdvance { get; set; } = false;
	[Property, Range( 0.1f, 60f )] public float TimeScale { get; set; } = 1.0f;
	[Property] public DirectionalLight SunLight { get; set; }
	[Property] public ModelRenderer SkyRenderer { get; set; }
	[Property, Range( 0.001f, 0.02f )] public float SunDiscSize { get; set; } = 0.003f;
	[Property, Range( 0.001f, 0.01f )] public float MoonDiscSize { get; set; } = 0.002f;
	[Property] public bool HijackMapLight { get; set; } = true;

	private bool _mapOverridden;
	private GameObject _mapLightEnv;

	public float NormalizedTime => TimeOfDay / 24f;

	protected override void OnStart()
	{
		HijackMapLighting();

		if ( SkyRenderer == null )
		{
			var skyGo = new GameObject( true, "Sky Dome" );
			skyGo.Parent = GameObject;
			skyGo.WorldScale = Vector3.One * -300f;

			SkyRenderer = skyGo.AddComponent<ModelRenderer>();
			SkyRenderer.Model = Model.Load( "models/dev/sphere.vmdl" );
			SkyRenderer.MaterialOverride = Material.Load( "materials/skydome.vmat" );
			Log.Info( $"[SkyDome] Material: {SkyRenderer.MaterialOverride != null}" );
		}
	}

	private void HijackMapLighting()
	{
		// Disable map skyboxes
		foreach ( var sky in Scene.GetAllComponents<SkyBox2D>() )
		{
			Log.Info( $"[DayNight] Disabling map skybox: {sky.GameObject.Name}" );
			sky.Enabled = false;
		}

		// Hijack the map's light_environment — rotate it with our day/night cycle
		if ( HijackMapLight )
		{
			foreach ( var go in Scene.GetAllObjects( true ) )
			{
				if ( go.Name != "light_environment" ) continue;

				Log.Info( $"[DayNight] Hijacking map light_environment: {go.Name}" );
				_mapLightEnv = go;
				_mapOverridden = true;
				return;
			}
		}
		else
		{
			_mapOverridden = true;
		}
	}

	protected override void OnUpdate()
	{
		// Maps load async — keep trying until we find the map's light
		if ( !_mapOverridden )
		{
			HijackMapLighting();
		}

		if ( !IsProxy && AutoAdvance )
		{
			TimeOfDay += (Time.Delta / 60f) * TimeScale;
			if ( TimeOfDay >= 24f )
				TimeOfDay -= 24f;
		}

		float elevation = MathF.Sin( NormalizedTime * MathF.PI * 2f - MathF.PI * 0.5f );
		float dayFactor = MathX.Clamp( elevation * 2f, 0f, 1f );
		float sunAngle = NormalizedTime * 360f - 90f;

		if ( SunLight != null )
		{

			if ( elevation > 0f )
			{
				// Daytime: light follows sun
				SunLight.GameObject.WorldRotation = new Angles( sunAngle, 0f, 0f ).ToRotation();

				float warmth = 1f - MathF.Abs( elevation );
				float intensity = MathX.Clamp( elevation * 3f, 0.1f, 1f );
				Color tint = Color.Lerp( Color.White, new Color( 1f, 0.5f, 0.2f ), warmth * warmth );
				SunLight.LightColor = tint * intensity;
			}
			else
			{
				// Nighttime: light follows moon (opposite sun) with dim cool color
				float moonAngle = sunAngle + 180f;
				SunLight.GameObject.WorldRotation = new Angles( moonAngle, 0f, 0f ).ToRotation();

				float moonElevation = -elevation; // moon is high when sun is low
				float moonIntensity = MathX.Clamp( moonElevation * 0.5f, 0.02f, 0.15f );
				SunLight.LightColor = new Color( 0.4f, 0.5f, 0.7f ) * moonIntensity;
			}

			// Ambient sky color — prevents pure-black shadows
			Color daySky = new Color( 0.08f, 0.12f, 0.2f );
			Color nightSky = new Color( 0.02f, 0.02f, 0.05f );
			SunLight.SkyColor = Color.Lerp( nightSky, daySky, dayFactor );
		}

		// Sync the map's light_environment rotation with our sun/moon
		if ( HijackMapLight && _mapLightEnv != null )
		{
			_mapLightEnv.WorldRotation = SunLight.GameObject.WorldRotation;
		}

		if ( SkyRenderer?.SceneObject != null )
		{
			// Always pass the sun's actual direction (not the light, which follows moon at night)
			var sunRotation = new Angles( sunAngle, 0f, 0f ).ToRotation();

			var so = SkyRenderer.SceneObject;
			so.Attributes.Set( "TimeOfDay", NormalizedTime );
			so.Attributes.Set( "SunDirection", sunRotation.Forward );
			so.Attributes.Set( "SunDiscSize", SunDiscSize );
			so.Attributes.Set( "MoonDiscSize", MoonDiscSize );
			so.Attributes.Set( "StarVisibility", MathX.Clamp( -elevation * 3f, 0f, 1f ) );
		}
	}

	protected override void OnPreRender()
	{
		if ( SkyRenderer == null ) return;
		var cam = Scene.Camera;
		if ( cam != null )
			SkyRenderer.GameObject.WorldPosition = cam.WorldPosition;
	}
}
