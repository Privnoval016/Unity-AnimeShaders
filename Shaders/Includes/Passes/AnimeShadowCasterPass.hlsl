#ifndef ANIME_SHADOW_CASTER_PASS_INCLUDED
#define ANIME_SHADOW_CASTER_PASS_INCLUDED

// ShadowCaster: the body's shadow, faded with the body by the same dither (screen space of the shadow map).

#include "../Core/AnimeSurface.hlsl"
#include "../Core/AnimeFade.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

float3 _LightDirection;
float3 _LightPosition;

struct AnimeShadowAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct AnimeShadowVaryings
{
#if defined(_ALPHATEST_ON)
    float2 uv : TEXCOORD0;
#endif
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

float4 AnimeShadowPositionHClip(AnimeShadowAttributes input)
{
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
    return ApplyShadowClamping(positionCS);
}

AnimeShadowVaryings AnimeShadowVertex(AnimeShadowAttributes input)
{
    AnimeShadowVaryings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
#if defined(_ALPHATEST_ON)
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
#endif
    output.positionCS = AnimeFadeCull(AnimeShadowPositionHClip(input));
    return output;
}

half4 AnimeShadowFragment(AnimeShadowVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    AnimeFadeClip(input.positionCS);
#if defined(_ALPHATEST_ON)
    AnimeAlphaClip(AnimeSampleBase(input.uv).a * _BaseColor.a);
#endif
    return 0;
}

#endif
