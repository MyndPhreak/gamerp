using Sandbox;
using System;

public sealed class DayNightCycle : Component
{
	[Property, Range( 0f, 24f )] public float TimeOfDay { get; set; } = 12.0f;
	[Property] public bool AutoAdvance { get; set; } = false;
	[Property, Range( 0.1f, 60f )] public float TimeScale { get; set; } = 1.0f;
	[Property] public DirectionalLight SunLight { get; set; }
	[Property] public ModelRenderer SkyRenderer { get; set; }
	[Property, Range( 0.01f, 0.15f )] public float SunDiscSize { get; set; } = 0.05f;
	[Property, Range( 0.01f, 0.10f )] public float MoonDiscSize { get; set; } = 0.04f;

	public float NormalizedTime => TimeOfDay / 24f;

	protected override void OnStart()
	{
		if ( SkyRenderer == null )
		{
			var skyGo = new GameObject( true, "Sky Dome" );
			skyGo.Parent = GameObject;
			skyGo.WorldScale = Vector3.One * -1000f;

			SkyRenderer = skyGo.AddComponent<ModelRenderer>();
			SkyRenderer.Model = Model.Load( "models/dev/sphere.vmdl" );
			SkyRenderer.MaterialOverride = Material.Load( "materials/skydome.vmat" );
			Log.Info( $"[SkyDome] Material: {SkyRenderer.MaterialOverride != null}" );
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

		if ( SunLight != null )
		{
			float sunAngle = NormalizedTime * 360f - 90f;
			SunLight.GameObject.WorldRotation = new Angles( sunAngle, 0f, 0f ).ToRotation();

			float warmth = 1f - MathF.Abs( elevation );
			float intensity = MathX.Clamp( elevation * 3f, 0.05f, 1f );
			Color tint = Color.Lerp( Color.White, new Color( 1f, 0.5f, 0.2f ), warmth * warmth );
			SunLight.LightColor = tint * intensity;

			// Ambient sky color — prevents pure-black shadows
			Color daySky = new Color( 0.08f, 0.12f, 0.2f );
			Color nightSky = new Color( 0.02f, 0.02f, 0.05f );
			SunLight.SkyColor = Color.Lerp( nightSky, daySky, dayFactor );
		}

		if ( SkyRenderer?.SceneObject != null )
		{
			var so = SkyRenderer.SceneObject;
			so.Attributes.Set( "TimeOfDay", NormalizedTime );
			so.Attributes.Set( "SunDirection", SunLight.GameObject.WorldRotation.Forward );
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
