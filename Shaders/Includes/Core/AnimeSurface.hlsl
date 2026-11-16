#ifndef ANIME_SURFACE_INCLUDED
#define ANIME_SURFACE_INCLUDED

// The surface an anime shader shades: what the material gives the lighting. Thin entry shaders fill it their own way
// (a face or hair shader adds its own terms) and hand it to AnimeShade in AnimeLighting.hlsl.

#include "AnimeProperties.hlsl"

struct AnimeSurface
{
    half3 albedo;
    half3 normalWS;
    half3 emission;
    half alpha;
};

half4 AnimeSampleBase(float2 uv)
{
    return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
}

// A linear colour's brightness (Rec. 709 weights).
half AnimeLuma(half3 colour)
{
    return dot(colour, half3(0.2126h, 0.7152h, 0.0722h));
}

void AnimeAlphaClip(half alpha)
{
#if defined(_ALPHATEST_ON)
    clip(alpha - _Cutoff);
#endif
}

#if defined(ANIME_GROUP_INSIDE)
// Procedural tone of a two-sided surface's inside, from the front colour: tint, saturation, darkening, and an
// optional fade along a UV axis (0 at the start edge, full at the end edge).
half3 AnimeApplyInsideTone(half3 albedo, float2 uv)
{
    half3 tinted = albedo * _InsideTint.rgb;
    half luma = AnimeLuma(tinted);
    tinted = lerp(luma.xxx, tinted, _InsideSaturation);
    tinted *= 1.0h - _InsideDarken;

    half strength = 1.0h;
    if (ANIME_INSIDE_FADE_ON)
    {
        float axisValue = lerp(uv.x, uv.y, _InsideFadeAxis);
        float span = _InsideFadeEnd - _InsideFadeStart;
        span = abs(span) < 1e-4 ? 1e-4 : span;
        strength = half(saturate((axisValue - _InsideFadeStart) / span));
    }
    return lerp(albedo, tinted, strength);
}
#endif

#if defined(ANIME_GROUP_EMISSION)
half3 AnimeSampleEmission(float2 uv)
{
    half3 emission = _EmissionColor.rgb;
    if (ANIME_EMISSION_MAP_ON)
        emission *= SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, uv).rgb;
    return emission;
}
#endif

#endif
