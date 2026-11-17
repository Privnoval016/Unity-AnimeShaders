#ifndef ANIME_DIRECTOR_INCLUDED
#define ANIME_DIRECTOR_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// The light director's global properties, in two channels: the characters' (the default) and the world's (an entry
// shader that defines ANIME_DIRECTOR_WORLD in its HLSLINCLUDE, before any include). A project sets them
// (AnimeLightDirector) to relight a scene or a shot; never set, every value is zero and means "no change":
//
//   _AnimeDirectorLightDirection  characters only. xyz: world direction TOWARD the light (normalised by the sender),
//                                 w: weight 0..1 of the override (0 = the scene's main light)
//   _AnimeDirector[World]LightColor   rgb: light colour multiplier MINUS 1 (0 = x1, -1 = black, 1 = x2)
//   _AnimeDirector[World]ShadowColor  rgb: shadow tone multiplier MINUS 1 (after the material's tint and the sky's shade)
//   _AnimeDirector[World]Ambient      rgb: ambient colour, w: weight 0..1 of replacing the probe's ambient
//   _AnimeDirector[World]Scalars      x: cast shadow strength multiplier MINUS 1; y: rim and z: line width multipliers
//                                     MINUS 1 (characters only); w: reserved (keep 0)
//
// The world has no direction override: the sun's angle is the sky's, so the haze, the sky and the cloud shadows read the
// real light. Cast shadows always come from the real light.

CBUFFER_START(AnimeDirector)
    float4 _AnimeDirectorLightDirection;
    float4 _AnimeDirectorLightColor;
    float4 _AnimeDirectorShadowColor;
    float4 _AnimeDirectorAmbient;
    float4 _AnimeDirectorScalars;
    float4 _AnimeDirectorWorldLightColor;
    float4 _AnimeDirectorWorldShadowColor;
    float4 _AnimeDirectorWorldAmbient;
    float4 _AnimeDirectorWorldScalars;
CBUFFER_END

#if defined(ANIME_DIRECTOR_WORLD)
    #define ANIME_DIRECTOR_LIGHT_COLOR _AnimeDirectorWorldLightColor
    #define ANIME_DIRECTOR_SHADOW_COLOR _AnimeDirectorWorldShadowColor
    #define ANIME_DIRECTOR_AMBIENT _AnimeDirectorWorldAmbient
    #define ANIME_DIRECTOR_SCALARS _AnimeDirectorWorldScalars
#else
    #define ANIME_DIRECTOR_LIGHT_COLOR _AnimeDirectorLightColor
    #define ANIME_DIRECTOR_SHADOW_COLOR _AnimeDirectorShadowColor
    #define ANIME_DIRECTOR_AMBIENT _AnimeDirectorAmbient
    #define ANIME_DIRECTOR_SCALARS _AnimeDirectorScalars
#endif

// The direction toward the light this surface is lit from: the characters' override blended in, the world's real light.
float3 AnimeDirectorLightDirection(float3 lightDirectionWS)
{
#if defined(ANIME_DIRECTOR_WORLD)
    return normalize(lightDirectionWS);
#else
    return normalize(lerp(lightDirectionWS, _AnimeDirectorLightDirection.xyz, _AnimeDirectorLightDirection.w));
#endif
}

half3 AnimeDirectorLightColorMultiplier()
{
    return max(half3(0, 0, 0), 1.0h + half3(ANIME_DIRECTOR_LIGHT_COLOR.rgb));
}

half3 AnimeDirectorShadowColorMultiplier()
{
    return max(half3(0, 0, 0), 1.0h + half3(ANIME_DIRECTOR_SHADOW_COLOR.rgb));
}

half3 AnimeDirectorAmbient(half3 probeAmbient)
{
    return lerp(probeAmbient, half3(ANIME_DIRECTOR_AMBIENT.rgb), half(ANIME_DIRECTOR_AMBIENT.w));
}

half AnimeDirectorShadowStrengthMultiplier()
{
    return max(0.0h, 1.0h + half(ANIME_DIRECTOR_SCALARS.x));
}

half AnimeDirectorRimStrengthMultiplier()
{
    return max(0.0h, 1.0h + half(ANIME_DIRECTOR_SCALARS.y));
}

float AnimeDirectorOutlineWidthMultiplier()
{
    return max(0.0, 1.0 + ANIME_DIRECTOR_SCALARS.z);
}

#endif
