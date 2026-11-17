#ifndef ANIME_EYE_MARK_PASS_INCLUDED
#define ANIME_EYE_MARK_PASS_INCLUDED

// EyeMark (Anime/Eyes, drawn with the opaques right after the eyes' own pass): marks the stencil bit where an eye or brow
// shows and its own fringe lies just in front of it: the hair's depth there nearer than the eye by no more than Eye Hair
// Distance, so another character's hair metres away never counts. Writes no colour or depth; AnimeHairDepthFeature then
// draws the eyes over the hair on the marked pixels. Without the feature nothing is marked.

#include "../Core/AnimeSurface.hlsl"
#include "../Core/AnimeFade.hlsl"
#include "../Character/AnimeHairDepth.hlsl"

// Hair nearer than the eye by less than this is the eye's own surface, not hair in front of it.
#define ANIME_EYE_MARK_BIAS 1e-3

struct AnimeEyeMarkAttributes
{
    float4 positionOS : POSITION;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct AnimeEyeMarkVaryings
{
    float2 uv : TEXCOORD0;
    float eyeDepth : TEXCOORD1;
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

AnimeEyeMarkVaryings AnimeEyeMarkVertex(AnimeEyeMarkAttributes input)
{
    AnimeEyeMarkVaryings output = (AnimeEyeMarkVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.eyeDepth = -TransformWorldToView(positionWS).z;
    output.positionCS = AnimeFadeCull(TransformWorldToHClip(positionWS));
    return output;
}

half4 AnimeEyeMarkFragment(AnimeEyeMarkVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    AnimeFadeClip(input.positionCS);
#if defined(_ALPHATEST_ON)
    AnimeAlphaClip(AnimeSampleBase(input.uv).a * _BaseColor.a);
#endif
    float hairDepth = AnimeHairDepthAt(AnimeHairDepthTexel(GetNormalizedScreenSpaceUV(input.positionCS)));
    float inFront = input.eyeDepth - hairDepth;
    clip(inFront > ANIME_EYE_MARK_BIAS && inFront < _EyeHairDistance ? 1.0 : -1.0);
    return half4(0, 0, 0, 0);
}

#endif
