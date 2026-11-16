#ifndef ANIME_DIRECTOR_INCLUDED
#define ANIME_DIRECTOR_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// The light director's global properties. A project sets them (Shader.SetGlobalVector) to relight a scene or a shot;
// never set, every value is zero and means "no change". Neutral is encoded as zero:
//
//   _AnimeDirectorLightDirection  xyz: world direction TOWARD the light (normalised by the sender)
//                                 w:   weight 0..1 of the override (0 = the scene's main light is used)
//   _AnimeDirectorLightColor      rgb: light colour multiplier MINUS 1 (0 = x1, -1 = black, 1 = x2)
//   _AnimeDirectorShadowColor     rgb: shadow tone multiplier MINUS 1
//   _AnimeDirectorAmbient         rgb: ambient colour, w: weight 0..1 of replacing the probe's ambient (0 = probe)
//   _AnimeDirectorScalars         x: cast shadow strength multiplier MINUS 1
//                                 y: rim strength multiplier MINUS 1
//                                 z: line width multiplier MINUS 1
//                                 w: reserved (keep 0)
//
// The override direction steers the tones, rim and extra lights only: cast shadows still come from the real light.

CBUFFER_START(AnimeDirector)
    float4 _AnimeDirectorLightDirection;
    float4 _AnimeDirectorLightColor;
    float4 _AnimeDirectorShadowColor;
    float4 _AnimeDirectorAmbient;
    float4 _AnimeDirectorScalars;
CBUFFER_END

float3 AnimeDirectorLightDirection(float3 lightDirectionWS)
{
    return normalize(lerp(lightDirectionWS, _AnimeDirectorLightDirection.xyz, _AnimeDirectorLightDirection.w));
}

half3 AnimeDirectorLightColorMultiplier()
{
    return max(half3(0, 0, 0), 1.0h + half3(_AnimeDirectorLightColor.rgb));
}

half3 AnimeDirectorShadowColorMultiplier()
{
    return max(half3(0, 0, 0), 1.0h + half3(_AnimeDirectorShadowColor.rgb));
}

half3 AnimeDirectorAmbient(half3 probeAmbient)
{
    return lerp(probeAmbient, half3(_AnimeDirectorAmbient.rgb), half(_AnimeDirectorAmbient.w));
}

half AnimeDirectorShadowStrengthMultiplier()
{
    return max(0.0h, 1.0h + half(_AnimeDirectorScalars.x));
}

half AnimeDirectorRimStrengthMultiplier()
{
    return max(0.0h, 1.0h + half(_AnimeDirectorScalars.y));
}

float AnimeDirectorOutlineWidthMultiplier()
{
    return max(0.0, 1.0 + _AnimeDirectorScalars.z);
}

#endif
