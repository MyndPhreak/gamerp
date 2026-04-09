
HEADER
{
	Description = "Procedural sky dome";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	Forward();
	ToolsShadingComplexity( "tools_shading_complexity.shader" );
}

COMMON
{
	#ifndef S_ALPHA_TEST
	#define S_ALPHA_TEST 0
	#endif
	#ifndef S_TRANSLUCENT
	#define S_TRANSLUCENT 0
	#endif

	#include "common/shared.hlsl"
	#include "procedural.hlsl"

	#define S_UV2 1
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
	float4 vColor : COLOR0 < Semantic( Color ); >;
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
	float3 vPositionOs : TEXCOORD14;
	float3 vNormalOs : TEXCOORD15;
	float4 vTangentUOs_flTangentVSign : TANGENT	< Semantic( TangentU_SignV ); >;
	float4 vColor : COLOR0;
	float4 vTintColor : COLOR1;
	#if ( PROGRAM == VFX_PROGRAM_PS )
		bool vFrontFacing : SV_IsFrontFace;
	#endif
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput v )
	{

		PixelInput i = ProcessVertex( v );
		i.vPositionOs = v.vPositionOs.xyz;
		i.vColor = v.vColor;

		ExtraShaderData_t extraShaderData = GetExtraPerInstanceShaderData( v.nInstanceTransformID );
		i.vTintColor = extraShaderData.vTint;

		VS_DecodeObjectSpaceNormalAndTangent( v, i.vNormalOs, i.vTangentUOs_flTangentVSign );
		return FinalizeVertex( i );

	}
}

PS
{
	#include "common/pixel.hlsl"

	float g_flTimeOfDay      < Attribute( "TimeOfDay" );      Default( 0.5 ); >;
	float3 g_vSunDirection   < Attribute( "SunDirection" );   Default3( 0, 0, 1 ); >;
	float g_flSunDiscSize    < Attribute( "SunDiscSize" );    Default( 0.05 ); >;
	float g_flMoonDiscSize   < Attribute( "MoonDiscSize" );   Default( 0.04 ); >;
	float g_flStarVisibility < Attribute( "StarVisibility" ); Default( 0.0 ); >;

	// --- Utility ---

	float starHash( float3 p )
	{
		p = frac( p * float3( 443.8975, 397.2973, 491.1871 ) );
		p += dot( p, p.yzx + 19.19 );
		return frac( (p.x + p.y) * p.z );
	}

	// --- Rayleigh + Mie scattering ---

	// Rayleigh phase function
	float phaseRayleigh( float cosTheta )
	{
		return 0.75 * (1.0 + cosTheta * cosTheta);
	}

	// Henyey-Greenstein phase function for Mie scattering
	float phaseMie( float cosTheta, float mieG )
	{
		float g2 = mieG * mieG;
		float denom = 1.0 + g2 - 2.0 * mieG * cosTheta;
		return (1.0 - g2) / (4.0 * 3.14159265 * pow( denom, 1.5 ));
	}

	float3 atmosphericScattering( float3 dir, float3 sunDir, float dayFactor )
	{
		float cosTheta = dot( dir, sunDir );
		float sunAltitude = sunDir.z; // how high the sun is

		// Rayleigh coefficients (wavelength-dependent: more blue scattered)
		float3 betaR = float3( 5.8e-3, 1.35e-2, 3.31e-2 );

		// Mie coefficient (wavelength-independent, forward scatter)
		float3 betaM = float3( 4.0e-3, 4.0e-3, 4.0e-3 );

		// Optical depth approximation based on view elevation
		float viewAlt = max( dir.z, 0.001 );
		float opticalDepth = 1.0 / (viewAlt + 0.15);

		// Extinction
		float3 extinction = exp( -(betaR + betaM) * opticalDepth );

		// In-scattering
		float3 rayleigh = betaR * phaseRayleigh( cosTheta );
		float3 mie = betaM * phaseMie( cosTheta, 0.76 );

		// Sun intensity varies with its altitude
		float sunIntensity = max( sunAltitude * 2.0, 0.0 );

		// Combine scattering (extinction attenuates light along the view ray)
		float3 scatter = (rayleigh + mie) * sunIntensity * opticalDepth * extinction;

		// Night sky base color
		float3 nightColor = float3( 0.005, 0.007, 0.02 );

		// Blend between night and scattered sky
		float3 sky = lerp( nightColor, scatter, dayFactor );

		// Horizon glow at sunset/sunrise
		float horizonMask = pow( saturate( 1.0 - abs( dir.z ) ), 4.0 );
		float sunsetFactor = pow( saturate( 1.0 - abs( sunAltitude ) * 3.0 ), 0.5 );
		float3 sunsetColor = float3( 1.0, 0.3, 0.05 ) * sunsetFactor * horizonMask * 0.8;
		sky += sunsetColor;

		return sky;
	}

	// --- Stars ---

	float starField( float3 dir, float time )
	{
		// High-resolution grid for tiny stars
		float3 cell = floor( dir * 300.0 );
		float3 cellCenter = (cell + 0.5) / 300.0;
		float3 cellNorm = normalize( cellCenter );

		// Random brightness per cell
		float h = starHash( cell );

		// Only a small fraction of cells have stars (sparse)
		float starMask = step( 0.985, h );

		// Distance from cell center for point-like falloff
		float dist = length( dir - cellNorm ) * 300.0;
		float pointShape = exp( -dist * dist * 20.0 );

		// Twinkle
		float twinkle = 0.7 + 0.3 * sin( h * 200.0 + time * 3.0 );

		return starMask * pointShape * twinkle * h;
	}

	// --- Main ---

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float3 dir = normalize( i.vNormalWs );
		float3 sunDir = normalize( g_vSunDirection );
		float elevation = sin( g_flTimeOfDay * 3.14159265 * 2.0 - 3.14159265 * 0.5 );
		float dayFactor = saturate( elevation * 2.0 );

		// Atmospheric scattering sky
		float3 color = atmosphericScattering( dir, sunDir, dayFactor );

		// Sun disc
		float sunDot = dot( dir, sunDir );
		float sunEdge = 1.0 - g_flSunDiscSize;
		float sunDisc = smoothstep( sunEdge - 0.002, sunEdge, sunDot );
		float sunGlow = pow( saturate( sunDot ), 256.0 ) * 0.5;
		float sunVis = saturate( elevation * 5.0 );
		color += float3( 10.0, 8.0, 6.0 ) * sunDisc * sunVis;
		color += float3( 1.0, 0.6, 0.2 ) * sunGlow * sunVis;

		// Moon disc
		float3 moonDir = normalize( -sunDir );
		float moonDot = dot( dir, moonDir );
		float moonEdge = 1.0 - g_flMoonDiscSize;
		float moonDisc = smoothstep( moonEdge - 0.003, moonEdge, moonDot );
		color = lerp( color, float3( 0.85, 0.90, 1.0 ), moonDisc * g_flStarVisibility );

		// Stars
		float stars = starField( dir, g_flTimeOfDay * 6.28318 );
		color += stars * g_flStarVisibility * 0.6;

		// Tone mapping (simple Reinhard to avoid blowout)
		color = color / (1.0 + color);

		// Output as unlit emission
		Material m = Material::Init( i );
		m.Albedo = float3( 0, 0, 0 );
		m.Emission = color;
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		m.TintMask = 1;
		m.Opacity = 1;
		m.Transmission = 0;

		m.Normal = TransformNormal( float3( 0, 0, 1 ), i.vNormalWs, i.vTangentUWs, i.vTangentVWs );
		m.WorldTangentU = i.vTangentUWs;
		m.WorldTangentV = i.vTangentVWs;
		m.TextureCoords = i.vTextureCoords.xy;

		return ShadingModelStandard::Shade( m );
	}
}
