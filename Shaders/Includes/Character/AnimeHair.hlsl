#ifndef ANIME_HAIR_INCLUDED
#define ANIME_HAIR_INCLUDED

// The hair's surface (ANIME_CUSTOM_SURFACE): the angel ring, a band of highlight round the head added on the lit side
// only (AnimeSurface.litHighlight). Where on the hair: each point's direction from the head's centre (or its normal, by
// Ring Sphere Blend) against the head's up, tilted forward by Ring Tilt, gives its latitude, and the band sits at Ring
// Height, Ring Width either side, its edges softened by Ring Softness. Ring Follows View slides it toward the view's band
// instead (where the normal meets the view at Ring Height), the prototype's first style. Its lower edge reaches up and
// down strand by strand round the head (Ring Breakup, over Ring Breakup Count strands, soft waves to sawtooth teeth by
// Ring Breakup Shape), as drawn highlights do. Brighter toward the view (Ring Facing); a softer wide lobe (Ring Glow).
// Its colour is Ring Colour, taking the hair's own hue by Ring Hair Tint and the light's by Ring Light Colour, times
// Ring Strength and the Ring Mask, capped at Ring Brightest. Without a head frame there is no ring.
//
// Reads: the ring mask once.

#include "../Core/AnimeLighting.hlsl"
#include "AnimeHead.hlsl"

// How far the outer glow reaches, in ring widths.
#define ANIME_RING_GLOW_WIDTHS 3.0
// Below this the hair's brightness is too low to take a hue from.
#define ANIME_RING_DARKEST 0.05

// 1 inside a band centred at height, half as wide as width either side, its edges softened by softness (0 to 1 of the
// width); the lower edge moved by shift.
half AnimeRingBand(half coordinate, half height, half width, half softness, half shift)
{
    half soft = max(width * softness, 1e-3h);
    half lower = height - width + shift;
    half upper = height + width;
    return smoothstep(lower, lower + soft, coordinate) * (1.0h - smoothstep(upper - soft, upper, coordinate));
}

// The breakup's pattern round the head, 0 to 1: soft waves at shape 0, sawtooth teeth at 1.
half AnimeRingStrands(float3 fromCentre, float3 right, float3 forward)
{
    float around = atan2(dot(fromCentre, right), dot(fromCentre, forward)) * (0.5 / PI) + 0.5;
    half strand = half(frac(around * _RingBreakupCount));
    half wave = 0.5h - 0.5h * cos(strand * TWO_PI);
    return lerp(wave, strand, _RingBreakupShape);
}

// How much ring a point has, 0 to about 1, before colour and strength.
half AnimeRingAmount(float3 positionWS, half3 normalWS, half3 viewWS)
{
    float3 fromCentre = positionWS - _AnimeHeadCentre.xyz;
    float3 up = _AnimeHeadUp.xyz, forward = _AnimeHeadForward.xyz, right = _AnimeHeadRight.xyz;
    half3 direction = normalize(lerp(normalWS, half3(normalize(fromCentre)), _RingSphereBlend));
    half3 axis = half3(normalize(up + forward * _RingTilt));
    half shift = _RingBreakup * _RingWidth * 2.0h * (AnimeRingStrands(fromCentre, right, forward) - 0.5h);

    half fixedOnHead = AnimeRingBand(dot(direction, axis), _RingHeight, _RingWidth, _RingSoftness, shift);
    half towardView = AnimeRingBand(dot(normalWS, viewWS), _RingHeight, _RingWidth, _RingSoftness, shift)
        * smoothstep(0.0h, 0.3h, dot(direction, half3(up)));
    half ring = lerp(fixedOnHead, towardView, _RingFollowsView);
    half glowWidth = _RingWidth * ANIME_RING_GLOW_WIDTHS;
    ring = max(ring, _RingGlow * AnimeRingBand(dot(direction, axis), _RingHeight, glowWidth, 1.0h, 0.0h));
    return ring * lerp(1.0h, saturate(dot(normalWS, viewWS) * 1.5h), _RingFacing);
}

void AnimeCustomSurface(float3 positionWS, float2 uv, inout AnimeSurface surface)
{
    if (!AnimeHeadFrameSet())
        return;
    half3 viewWS = half3(GetWorldSpaceNormalizeViewDir(positionWS));
    half amount = AnimeRingAmount(positionWS, surface.normalWS, viewWS);
    amount *= SAMPLE_TEXTURE2D(_RingMask, sampler_BaseMap, uv).r;
    half3 hue = surface.albedo / max(AnimeLuma(surface.albedo), ANIME_RING_DARKEST);
    half3 colour = _RingColor.rgb * lerp(half3(1, 1, 1), hue, _RingHairTint);
    colour *= lerp(half3(1, 1, 1), GetMainLight().color, _RingLightColor);
    surface.litHighlight = min(colour * (amount * _RingStrength), _RingBrightest);
}

#endif
