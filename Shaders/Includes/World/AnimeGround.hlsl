#ifndef ANIME_GROUND_INCLUDED
#define ANIME_GROUND_INCLUDED

// The ground's surface colour (ANIME_CUSTOM_ALBEDO): grass in large soft patches with bare earth, and stone flagstones
// with grout, inside an optional disc (a plaza, an arena, a ring; radius 0 for none), all from the pack's baked noise
// in world XZ: three samples a pixel. Grout is widened by its own screen derivative and the fine detail fades with
// distance, so a low camera never sees it shimmer.

#include "../Core/AnimeProperties.hlsl"

#if !defined(ANIME_GROUP_GROUND)
    #error The ground needs ANIME_GROUP_GROUND in the shader's HLSLINCLUDE.
#endif

TEXTURE2D(_AnimeNoiseTex);
SAMPLER(sampler_AnimeNoiseTex);

// The noise texture holds this many flagstone cells across one tile.
#define ANIME_NOISE_STONE_CELLS 8.0

half4 AnimeGroundNoise(float2 position)
{
    return SAMPLE_TEXTURE2D(_AnimeNoiseTex, sampler_AnimeNoiseTex, position);
}

half3 AnimeCustomAlbedo(float3 positionWS, float2 uv, half3 baseAlbedo)
{
    float2 p = positionWS.xz;
    half4 patch = AnimeGroundNoise(p * _GrassPatchScale);
    half4 grain = AnimeGroundNoise(p * _GrainScale);
    half4 stone = AnimeGroundNoise(p / (max(_StoneSize, 0.01) * ANIME_NOISE_STONE_CELLS));

    half detail = 1.0h - half(saturate((distance(positionWS, _WorldSpaceCameraPos) - _DetailFadeStart) / max(_DetailFadeRange, 0.01)));

    half3 grass = lerp(_GrassColorA.rgb, _GrassColorB.rgb, smoothstep(0.3h, 0.7h, patch.r));
    half earth = smoothstep(1.0h - _EarthAmount, 1.0h - _EarthAmount + 0.05h, patch.g);
    grass = lerp(grass, _EarthColor.rgb, earth);
    grass *= 1.0h + (grain.g - 0.5h) * _GrainStrength * detail;

    half3 flagstone = lerp(_StoneColorA.rgb, _StoneColorB.rgb, stone.a);
    flagstone *= 1.0h + (grain.r - 0.5h) * _GrainStrength * 0.5h * detail;
    float edge = stone.b;
    float soften = max(fwidth(edge), 1e-4);
    half grout = half(1.0 - smoothstep(_GroutWidth - soften, _GroutWidth + soften, edge)) * detail;
    flagstone = lerp(flagstone, _GroutColor.rgb, grout);

    float fromCentre = distance(p, _GroundCircle.xy) + (patch.g - 0.5) * 2.0 * _GroundCircle.w;
    half inside = _GroundCircle.z > 0.0
        ? half(1.0 - smoothstep(_GroundCircle.z - _EdgeSoftness, _GroundCircle.z + _EdgeSoftness, fromCentre))
        : 0.0h;

    return lerp(grass, flagstone, inside) * baseAlbedo;
}

#endif
