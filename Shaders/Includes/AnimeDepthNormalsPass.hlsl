#ifndef ANIME_DEPTH_NORMALS_PASS_INCLUDED
#define ANIME_DEPTH_NORMALS_PASS_INCLUDED

// DepthNormals: the camera normals texture for SSAO and other depth consumers. Vertex normals only (a normal map is
// a lighting detail), flipped on the back faces of a two-sided surface.

#include "AnimeSurface.hlsl"
#include "AnimeFade.hlsl"

struct AnimeDepthNormalsAttributes
{
    float4 positionOS : POSITION;
    float4 tangentOS : TANGENT;
    float2 texcoord : TEXCOORD0;
    float3 normalOS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct AnimeDepthNormalsVaryings
{
    float4 positionCS : SV_POSITION;
#if defined(_ALPHATEST_ON)
    float2 uv : TEXCOORD1;
#endif
    float3 normalWS : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

AnimeDepthNormalsVaryings AnimeDepthNormalsVertex(AnimeDepthNormalsAttributes input)
{
    AnimeDepthNormalsVaryings output = (AnimeDepthNormalsVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#if defined(_ALPHATEST_ON)
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
#endif
    output.positionCS = AnimeFadeCull(TransformObjectToHClip(input.positionOS.xyz));
    output.normalWS = NormalizeNormalPerVertex(TransformObjectToWorldNormal(input.normalOS));
    return output;
}

void AnimeDepthNormalsFragment(
    AnimeDepthNormalsVaryings input,
    FRONT_FACE_TYPE isFront : FRONT_FACE_SEMANTIC,
    out half4 outNormalWS : SV_Target0
#if defined(_WRITE_RENDERING_LAYERS)
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    AnimeFadeClip(input.positionCS);
#if defined(_ALPHATEST_ON)
    AnimeAlphaClip(AnimeSampleBase(input.uv).a * _BaseColor.a);
#endif

    float3 normalWS = input.normalWS * IS_FRONT_VFACE(isFront, 1.0, -1.0);
#if defined(_GBUFFER_NORMALS_OCT)
    normalWS = normalize(normalWS);
    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
    outNormalWS = half4(packedNormalWS, 0.0);
#else
    normalWS = NormalizeNormalPerPixel(normalWS);
    outNormalWS = half4(normalWS, 0.0);
#endif

#if defined(_WRITE_RENDERING_LAYERS)
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif
