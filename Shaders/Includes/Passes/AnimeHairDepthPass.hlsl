#ifndef ANIME_HAIR_DEPTH_PASS_INCLUDED
#define ANIME_HAIR_DEPTH_PASS_INCLUDED

// AnimeHairDepth: the hair drawn by AnimeHairDepthFeature into _AnimeHairDepth before the opaques, for the face's
// front-hair shadow. It writes one over the eye depth (0 where no hair is: the target's clear), so bilinear reads soften
// toward "no hair" at the fringe's edge; alpha-clipped as the hair is, and faded with it.

#include "../Core/AnimeSurface.hlsl"
#include "../Core/AnimeFade.hlsl"

struct AnimeHairDepthAttributes
{
    float4 positionOS : POSITION;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct AnimeHairDepthVaryings
{
    float2 uv : TEXCOORD0;
    float eyeDepth : TEXCOORD1;
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

AnimeHairDepthVaryings AnimeHairDepthVertex(AnimeHairDepthAttributes input)
{
    AnimeHairDepthVaryings output = (AnimeHairDepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.eyeDepth = -TransformWorldToView(positionWS).z;
    output.positionCS = AnimeFadeCull(TransformWorldToHClip(positionWS));
    return output;
}

half AnimeHairDepthFragment(AnimeHairDepthVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    AnimeFadeClip(input.positionCS);
#if defined(_ALPHATEST_ON)
    AnimeAlphaClip(AnimeSampleBase(input.uv).a * _BaseColor.a);
#endif
    return half(rcp(max(input.eyeDepth, 1e-3)));
}

#endif
