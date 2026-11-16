#ifndef ANIME_DEPTH_ONLY_PASS_INCLUDED
#define ANIME_DEPTH_ONLY_PASS_INCLUDED

// DepthOnly: the depth prepass and depth copy; a faded body fades here too.

#include "AnimeSurface.hlsl"
#include "AnimeFade.hlsl"

struct AnimeDepthAttributes
{
    float4 positionOS : POSITION;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct AnimeDepthVaryings
{
#if defined(_ALPHATEST_ON)
    float2 uv : TEXCOORD0;
#endif
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

AnimeDepthVaryings AnimeDepthVertex(AnimeDepthAttributes input)
{
    AnimeDepthVaryings output = (AnimeDepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#if defined(_ALPHATEST_ON)
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
#endif
    output.positionCS = AnimeFadeCull(TransformObjectToHClip(input.positionOS.xyz));
    return output;
}

half AnimeDepthFragment(AnimeDepthVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    AnimeFadeClip(input.positionCS);
#if defined(_ALPHATEST_ON)
    AnimeAlphaClip(AnimeSampleBase(input.uv).a * _BaseColor.a);
#endif
    return input.positionCS.z;
}

#endif
