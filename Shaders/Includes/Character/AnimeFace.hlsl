#ifndef ANIME_FACE_INCLUDED
#define ANIME_FACE_INCLUDED

// The face's surface (ANIME_CUSTOM_SURFACE): its form lit from the face map instead of its normals, so the shadow
// sweeps across the face in one clean line as the light goes round the head. The map holds each texel's normal flattened
// onto the head's horizontal plane (smoothed, in the model's front-right frame) and its face weight; the light is
// flattened onto the same plane through the head frame, and the face is lit where the two face each other (Lambert
// round the horizon, offset by the bias and softened by the softness). A light high above or below dims it by Light
// Height. The front hair's shadow (AnimeHairShadow.hlsl) darkens it by Hair Shadow Strength. Where the map's face weight
// is 0, without a map, or without a head frame, the surface shades as plain.
//
// Reads: the face map once, the hair's depth four times.

#include "../Core/AnimeLighting.hlsl"
#include "AnimeHead.hlsl"
#include "AnimeHairShadow.hlsl"

// Below this, a light's horizontal part is too short to say where round the head it is: taken as from the front.
#define ANIME_FACE_SHORTEST 1e-3

// The face's lit weight at a texel, 0 to 1, from its horizontal normal (x right, y front) and the direction toward the
// light, both against the head's frame.
half AnimeFaceLit(half2 normalH, float3 towardLight, float3 right, float3 forward, float3 up)
{
    float2 lightH = float2(dot(towardLight, right), dot(towardLight, forward));
    float length2 = dot(lightH, lightH);
    lightH = length2 > ANIME_FACE_SHORTEST * ANIME_FACE_SHORTEST ? lightH * rsqrt(length2) : float2(0.0, 1.0);
    half facing = half(dot(float2(normalH), lightH)) + _FaceShadowBias;
    half halfWidth = max(half(radians(_FaceShadowSoftness)), 0.004h) * 0.5h;
    half lit = smoothstep(-halfWidth, halfWidth, facing);
    return lit * (1.0h - _FaceLightHeight * half(abs(dot(towardLight, up))));
}

void AnimeCustomSurface(float3 positionWS, float2 uv, inout AnimeSurface surface)
{
    half4 map = SAMPLE_TEXTURE2D(_FaceMap, sampler_LinearClamp, uv);
    half2 normalH = map.rg * 2.0h - 1.0h;
    normalH *= rsqrt(max(dot(normalH, normalH), 1e-4h));

    Light mainLight = GetMainLight();
    float3 towardLight = AnimeDirectorLightDirection(mainLight.direction);
    half lit = AnimeFaceLit(normalH, towardLight, _AnimeHeadRight.xyz, _AnimeHeadForward.xyz, _AnimeHeadUp.xyz);
    surface.formOverride = lit * (1.0h - _HairShadowStrength * AnimeHairShadow(positionWS, towardLight));
    surface.formOverrideWeight = AnimeHeadFrameSet() ? map.b * map.a * _FaceMapStrength : 0.0h;
}

#endif
