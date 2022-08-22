#ifndef UNIVERSAL_HAIR_SURFACE_DATA_INCLUDED
#define UNIVERSAL_HAIR_SURFACE_DATA_INCLUDED

// Must match Universal ShaderGraph master node
struct MarschnerHairSurfaceData
{
	half3	geomNormalWS;
	half3	hairStrandDirection;
	half3   normalWS;
	half    cuticleAngle;
	half    perceptualRadialSmoothness;
};

void BuildSurfaceData(SurfaceDescription surfaceDescription, InputData inputData, half alpha, out MarschnerHairSurfaceData hairSurfaceData, out SurfaceData surfaceData)
{
	ZERO_INITIALIZE(MarschnerHairSurfaceData, hairSurfaceData);
	ZERO_INITIALIZE(SurfaceData, surfaceData);

	surfaceData.albedo							= surfaceDescription.BaseColor;
	surfaceData.smoothness						= saturate(surfaceDescription.Smoothness),
	surfaceData.occlusion						= surfaceDescription.Occlusion,
	surfaceData.emission						= surfaceDescription.Emission,
	surfaceData.alpha							= saturate(alpha),
	
	hairSurfaceData.geomNormalWS				= inputData.tangentToWorld[2];
	hairSurfaceData.hairStrandDirection         = normalize(TransformTangentToWorld(surfaceDescription.HairStrandDirection, inputData.tangentToWorld));
#if _USE_LIGHT_FACING_NORMAL
	hairSurfaceData.normalWS					= ComputeViewFacingNormal(inputData.viewDirectionWS, hairSurfaceData.hairStrandDirection);
#else
	hairSurfaceData.normalWS					= inputData.normalWS; // with normal map
#endif
#if _NORMAL_DROPOFF_TS
	surfaceData.normalTS						= surfaceDescription.NormalTS;
#endif
	hairSurfaceData.cuticleAngle                = surfaceDescription.CuticleAngle,
	hairSurfaceData.perceptualRadialSmoothness  = surfaceDescription.RadialSmoothness;
}
#endif
