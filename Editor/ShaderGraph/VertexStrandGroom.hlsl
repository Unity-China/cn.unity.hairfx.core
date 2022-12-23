/*
This script generates the procedural hair strand polygon in the vertex program
All the function here shares with both Universal and HD Pipeline. 
UNIVERSAL_PIPELINE keyword will be automatically defined the target as Universal pipeline in the ShaderGraph
*/

#ifndef HAIRFX_STRANDS_HLSL
#define HAIRFX_STRANDS_HLSL

#ifndef SHADERGRAPH_PREVIEW
	#if UNIVERSAL_PIPELINE
		// Specific to UniversalRenderPipeline
		#define DEFINE_SHADOWPASS SHADERPASS_SHADOWCASTER
		#if (SHADERPASS == DEFINE_SHADOWPASS)
			float3 _LightDirection;
			float3 _LightPosition;
		#endif

		#define HAIRFX_MATERIAL // override _LightDirection in custom Varying.hlsl

	#else
		// Specific to HDRenderPipeline
		#define DEFINE_SHADOWPASS SHADERPASS_SHADOWS
		
	#endif
#endif

#define TipPercentage _TipColorPercentage
#define FiberTipPercentage _FiberRatioStart
#define HairWidthMultiplier 1e-3


StructuredBuffer<float4>    g_HairVertexTangents;
StructuredBuffer<float4>    g_HairVertexPositions;
StructuredBuffer<float4>    g_TessellatedTangents;
StructuredBuffer<float4>    g_TessellatedPositions;


int     _BaseVertex;
int		_NumVerticesPerStrand;
int		_NumTessellationPerStrand;
float	_FiberRatio;
float	_FiberRatioStart;
bool	_EnableThinTip;
bool    _UseTessellation;

// Correct the transform between simulation and renderer
float4x4 _TFX_Correction;// Position

inline float3 GuideHairVertexPositions(uint index)
{
	float3 vert = _UseTessellation ? g_TessellatedPositions[index].xyz : g_HairVertexPositions[index].xyz;
	return mul(_TFX_Correction, float4(vert, 1.0)).xyz;
}

// Tangent
inline float3 GuideHairVertexTangents(uint index)
{
	return _UseTessellation ? g_TessellatedTangents[index].xyz : g_HairVertexTangents[index].xyz;
}


//----------------------------------------------------------------------------------
// Strands Generation  
//----------------------------------------------------------------------------------
// AMD 3.1.1 ref: TressFXRender.hlsl #484
// AMD 4.1	 ref: TressFXStrands.hlsl #86
void GetExpandedVertex_float(uint vertexId, float HairShadowWidth, float HairWidth,			// in
							out float3 position, out float3 normal, out float3 tangent)		// out
{
#ifdef SHADERGRAPH_PREVIEW
	position = 0;
	tangent  = 0;
	normal   = 0;
#else

	// on Android platform, submesh base vertex will be added to vertexId, so we need to subtract it before calculation
	vertexId -= _BaseVertex;

	// Access the current line segment
	uint index = vertexId / 2;	// vertexId is actually the indexed vertex id when indexed triangles are used
								// Get updated positions and tangents from simulation result

	float3 v = GuideHairVertexPositions(index);
	float3 t = GuideHairVertexTangents(index);

	// Get hair strand thickness
	uint numVerticesPerStrand = (_UseTessellation ? _NumTessellationPerStrand : _NumVerticesPerStrand);
	uint indexInStrand = index % numVerticesPerStrand;
	float fractionOfStrand = (float)indexInStrand / (numVerticesPerStrand - 1);
	//#if _ENABLED_HAIR_THIN_TIP
	//float thicknessCoeff = lerp(1.0, _FiberRatio, fractionOfStrand); // no tip percentage control
	float thicknessCoeff = fractionOfStrand > 1 - FiberTipPercentage ? lerp(1.0, _FiberRatio, (fractionOfStrand - 1 + FiberTipPercentage) / (FiberTipPercentage)) : 1.0;
	
	float ratio = (_EnableThinTip) ? thicknessCoeff : 1.0;

	float3 facingDirectionWS = float3(0.0, -1.0, 0.0);
	
#if (SHADERPASS == DEFINE_SHADOWPASS)
	HairWidth *= HairShadowWidth;

	// The hair strand need to facing the light direction during compute the shadow map	
	#if UNIVERSAL_PIPELINE
			// Punctual light
		#if _CASTING_PUNCTUAL_LIGHT_SHADOW
			facingDirectionWS = v - _LightPosition;
		#else
			// Main Directional light
			facingDirectionWS = -_LightDirection;
		#endif
	#else
		//ref: HDAdditionlLightData.cs #2346 and HDShadowAtlas.cs #321
		facingDirectionWS = -_ViewMatrix[2].xyz;
	#endif

#else
	facingDirectionWS = v - _WorldSpaceCameraPos; // v - eye
#endif
	
	facingDirectionWS = normalize(facingDirectionWS);
	
	// Calculate right and projected right vectors
	float3 right = normalize(cross(t, facingDirectionWS));

	// Write output data
	float fDirIndex = (vertexId & 0x01) ? -1.0 : 1.0;

	normal = normalize(cross(t, right));
	tangent = t;

	position = v + right * fDirIndex * ratio * (HairWidth * HairWidthMultiplier);
	#if UNIVERSAL_PIPELINE	
		// Transform from World To Object Space due to Mesh Renderer
		position = mul(UNITY_MATRIX_I_M, float4(position, 1.0)).xyz;
	#endif
	
	// Transform from World To Object Space due to Mesh Renderer
#ifdef UNITY_SHADER_VARIABLES_INCLUDED
	// HDRP
	// Ref: ShaderVariables.hlsl #339
	position = mul(GetRawUnityWorldToObject(), float4(position, 1.0)).xyz;
	normal = normalize(mul((float3x3)GetRawUnityWorldToObject(), normal));
	tangent = normalize(mul((float3x3)GetRawUnityWorldToObject(), tangent));
#endif
	
#endif
}


void GetExpandedVertex_half(uint vertexId, float HairShadowWidth, float HairWidth, // in
							out float3 position, out float3 normal, out float3 tangent)	// out
{
	GetExpandedVertex_float( vertexId, HairShadowWidth, HairWidth, 
							  position, normal, tangent);

}
#endif
