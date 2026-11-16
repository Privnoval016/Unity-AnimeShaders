#ifndef ANIME_WORLD_INCLUDED
#define ANIME_WORLD_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// The world's global properties, published by SkyApply from a sky preset. Never set, every value is zero: no clouds,
// no haze.
//
//   _AnimeCloudShape   x: 1 / a typical cloud's size (per metre)   y: cover 0..1   z: edge softness   w: altitude (m)
//   _AnimeCloudMotion  xy: drift across x and z (m/s)   z: simplicity (a mip bias on the shape)   w: shadow strength
//   _AnimeCloudShadowClear  x: no cloud shadow within this many metres of the camera   y: the metres they fade in over
//   _AnimeHazeColor    rgb: the haze (the horizon colour), linear   a: its most opacity
//   _AnimeHazeGlow     rgb: the haze toward the sun, linear   a: the glow's strength
//   _AnimeHazeShape    x: start (m)   y: density per metre   z: base height (m)   w: falloff per metre above it
//   _AnimeHazeSky      x: the sky's distance (m), where its haze is read   y: the glow's size (radians)
//
// _AnimeCloudNoise is the pack's baked noise (its G channel shapes the clouds).

CBUFFER_START(AnimeWorld)
    float4 _AnimeCloudShape;
    float4 _AnimeCloudMotion;
    float4 _AnimeCloudShadowClear;
    float4 _AnimeHazeColor;
    float4 _AnimeHazeGlow;
    float4 _AnimeHazeShape;
    float4 _AnimeHazeSky;
CBUFFER_END

TEXTURE2D(_AnimeCloudNoise);
SAMPLER(sampler_AnimeCloudNoise);

#if defined(_ANIME_ATMOSPHERE_KEYWORD_DECLARED)
    #define ANIME_ATMOSPHERE_ON (_ANIME_ATMOSPHERE)
#elif defined(_ANIME_ATMOSPHERE)
    #define ANIME_ATMOSPHERE_ON true
#else
    #define ANIME_ATMOSPHERE_ON false
#endif

#if defined(_ANIME_CLOUD_SHADOWS_KEYWORD_DECLARED)
    #define ANIME_CLOUD_SHADOWS_ON (_ANIME_CLOUD_SHADOWS)
#elif defined(_ANIME_CLOUD_SHADOWS)
    #define ANIME_CLOUD_SHADOWS_ON true
#else
    #define ANIME_CLOUD_SHADOWS_ON false
#endif

#endif
