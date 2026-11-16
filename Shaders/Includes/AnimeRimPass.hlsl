#ifndef ANIME_RIM_PASS_INCLUDED
#define ANIME_RIM_PASS_INCLUDED

// The depth-edge rim (AnimeRim pass), drawn by AnimeRimFeature over the camera colour after the opaques, additively,
// on the pixels the body itself left visible. Each pixel reads the scene's depth (URP's camera depth texture: scenery
// and every body) at a point moved outward along its view-space normal, past the body's own line; where that depth is
// farther than the threshold, the pixel sits on an edge against something behind it and takes the rim, strongest where
// the edge faces the light. The offset uses
// the normal's xy unnormalised, so surfaces turned to the camera reach nowhere and take none.

#include "AnimeLighting.hlsl"
#include "AnimeFade.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

struct AnimeRimAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct AnimeRimVaryings
{
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

AnimeRimVaryings AnimeRimVertex(AnimeRimAttributes input)
{
    AnimeRimVaryings output = (AnimeRimVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.positionCS = AnimeFadeCull(TransformWorldToHClip(output.positionWS));
    return output;
}

// The screen UV of a world position, in the convention the camera depth texture is read with.
float2 AnimeRimScreenUV(float3 positionWS)
{
    float4 screen = ComputeScreenPos(TransformWorldToHClip(positionWS));
    return screen.xy / screen.w;
}

// Eye depth of a raw camera depth value, for either projection.
float AnimeRimSceneEyeDepth(float rawDepth)
{
    return IsPerspectiveProjection() ? LinearEyeDepth(rawDepth, _ZBufferParams) : LinearDepthToEyeDepth(rawDepth);
}

// How many pixels the offset reaches: past the line (its own width at this depth), then the rim's width, held on
// screen up to the line's far distance and thinning beyond it like the line, never under one pixel.
float AnimeRimReachPixels(float eyeDepth)
{
    float screenHeight = _ScaledScreenParams.y;
    float distanceScale = 1.0;
    if (IsPerspectiveProjection())
    {
        float held = clamp(eyeDepth, _OutlineDistanceMin, max(_OutlineDistanceMin, _OutlineDistanceMax));
        distanceScale = held / max(eyeDepth, 1e-4);
    }
    float linePixels = _OutlineWidth * 0.001 * screenHeight * distanceScale * AnimeDirectorOutlineWidthMultiplier();
    float rimPixels = max(1.0, _RimDepthWidth * (screenHeight / 1080.0) * distanceScale);
    return linePixels + rimPixels;
}

half4 AnimeRimFragment(AnimeRimVaryings input, FRONT_FACE_TYPE isFront : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // A fading body takes no rim: every dithered hole would read as an edge.
    clip(AnimeFadeLevel() == 0u ? 1.0 : -1.0);
#if defined(_ALPHATEST_ON)
    AnimeAlphaClip(AnimeSampleBase(input.uv).a * _BaseColor.a);
#endif

    float3 normalWS = normalize(input.normalWS) * IS_FRONT_VFACE(isFront, 1.0, -1.0);
    float eyeDepth = -TransformWorldToView(input.positionWS).z;

    float2 here = AnimeRimScreenUV(input.positionWS);
    float2 toward = AnimeRimScreenUV(input.positionWS + normalWS * (0.01 * max(eyeDepth, 0.1))) - here;
    float2 towardPixels = toward * _ScaledScreenParams.xy;
    float lengthPixels = length(towardPixels);
    float facing = length(TransformWorldToViewDir(normalWS, true).xy);
    float2 outward = lengthPixels > 1e-5 ? towardPixels / lengthPixels : float2(0, 0);
    float2 readUV = here + outward * (facing * AnimeRimReachPixels(eyeDepth)) / _ScaledScreenParams.xy;

    float behind = AnimeRimSceneEyeDepth(SampleSceneDepth(saturate(readUV))) - eyeDepth;
    float threshold = _RimDepthThreshold * max(1.0, eyeDepth * 0.1);
    float rise = max(threshold * max(_RimSoftness, 0.01) * 4.0, 1e-3);
    half rim = half(saturate((behind - threshold) / rise));

    Light mainLight = GetMainLight();
    half3 lightDirection = half3(AnimeDirectorLightDirection(mainLight.direction));
    // How much the edge faces the light (zero on the side turned away), not the ramp's lit weight: with a low threshold
    // nearly every surface counts as lit, and the rim would ring the whole body.
    half towardLight = saturate(dot(half3(normalWS), lightDirection));
    rim *= lerp(1.0h, towardLight, _RimLightMask) * _RimDepth;

    return half4(_RimColor.rgb * (rim * _RimStrength * AnimeDirectorRimStrengthMultiplier()), 0.0h);
}

#endif
