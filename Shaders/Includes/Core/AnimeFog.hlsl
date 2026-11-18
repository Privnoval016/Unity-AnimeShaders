#ifndef ANIME_FOG_INCLUDED
#define ANIME_FOG_INCLUDED

// The haze: distance and height fog in the published haze colour, glowing toward the sun, as bright as the light
// director's sky says. With the atmosphere keyword
// off it is URP's own fog (MixFog), as before. Exact on orthographic cameras (distance from the camera's plane), and on
// transparents, since it is applied per pixel by the shader that draws it.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "AnimeWorld.hlsl"
#include "AnimeDirector.hlsl"

// The largest exponent the height term takes, so a long ray up or down never overflows.
#define ANIME_HAZE_MAX_EXPONENT 60.0

// <colour> seen through the haze along a ray from <origin> in the unit direction <ray> for <distance> metres.
half3 AnimeHazeAlong(half3 colour, float3 origin, float3 ray, float distance)
{
    float start = _AnimeHazeShape.x;
    float travelled = max(distance - start, 0.0);
    float falloff = max(_AnimeHazeShape.w, 1e-5);
    float startHeight = origin.y + ray.y * min(start, distance) - _AnimeHazeShape.z;
    float rise = clamp(falloff * ray.y * travelled, -ANIME_HAZE_MAX_EXPONENT, ANIME_HAZE_MAX_EXPONENT);
    float heightTerm = abs(rise) > 1e-4 ? (1.0 - exp(-rise)) / rise : 1.0 - 0.5 * rise;
    float amount = _AnimeHazeShape.y * travelled * exp(-falloff * max(startHeight, 0.0)) * heightTerm;
    half cover = min(half(1.0 - exp(-max(amount, 0.0))), half(_AnimeHazeColor.a));

    float3 sun = normalize(_MainLightPosition.xyz);
    float away = 1.0 - dot(ray, sun);
    half glow = half(exp(-away / max(_AnimeHazeSky.y * _AnimeHazeSky.y * 0.5, 1e-4))) * half(_AnimeHazeGlow.a);
    half3 haze = lerp(half3(_AnimeHazeColor.rgb), half3(_AnimeHazeGlow.rgb), glow) * AnimeDirectorSkyMultiplier();
    return lerp(colour, haze, cover);
}

// <colour> of a surface at <positionWS> seen from the camera through the haze.
half3 AnimeHaze(half3 colour, float3 positionWS)
{
    float3 toPixel = positionWS - _WorldSpaceCameraPos;
    float3 forward = -UNITY_MATRIX_V[2].xyz;
    bool orthographic = unity_OrthoParams.w > 0.5;
    float distance = orthographic ? max(dot(toPixel, forward), 0.0) : length(toPixel);
    float3 ray = orthographic ? forward : toPixel / max(distance, 1e-4);
    return AnimeHazeAlong(colour, positionWS - ray * distance, ray, distance);
}

// The pack's fog: the haze with the atmosphere on, URP's own fog (<fogCoord>) with it off.
half3 AnimeApplyFog(half3 colour, float3 positionWS, float fogCoord)
{
#if defined(_ANIME_ATMOSPHERE_KEYWORD_DECLARED) || defined(_ANIME_ATMOSPHERE)
    half3 fogged;
    UNITY_BRANCH
    if (ANIME_ATMOSPHERE_ON)
        fogged = AnimeHaze(colour, positionWS);
    else
        fogged = MixFog(colour, fogCoord);
    return fogged;
#else
    return MixFog(colour, fogCoord);
#endif
}

#endif
