#ifndef ANIME_HAIR_INCLUDED
#define ANIME_HAIR_INCLUDED

// The hair's surface (ANIME_CUSTOM_SURFACE): the angel ring, a band of highlight round the head added on the lit side
// only (AnimeSurface.litHighlight). Where on the hair: each point's direction from the head's centre (or its normal, by
// Ring Sphere Blend) against the head's up, tilted forward by Ring Tilt, gives its latitude, and the band sits at Ring
// Height, Ring Width either side, its edges softened by Ring Softness. Ring Follows View slides it toward the view's band
// instead (where the normal meets the view at Ring Height), the prototype's first style. The band is drawn as strokes,
// one per strand round the head (Ring Breakup Count), as drawn highlights are: a clean top edge, thin lines between the
// strands (Ring Strand Gap), and a lower edge that breaks into spikes, each strand's reaching down its own length (Ring
// Breakup: most short, a few long) and narrowing to a point by Ring Breakup Shape (0 square ends, 1 sharp tips). Both
// edges fade into the hair by Ring Softness (a soft streak, as drawn rings are; 0 crisp, anti-aliased to a pixel). Brighter toward the view (Ring Facing); a softer wide lobe (Ring Glow).
// Its colour is Ring Colour, taking the hair's own hue by Ring Hair Tint and the light's by Ring Light Colour, times
// Ring Strength and the Ring Mask, capped at Ring Brightest. Ring Lift turns it from added light (0) into the hair's own
// colour brightened (1: the hair under it times one plus the ring, so it fades into dark hair as a deeper sheen). Without
// a head frame there is no ring.
//
// Reads: the ring mask once.

#include "../Core/AnimeLighting.hlsl"
#include "AnimeHead.hlsl"
#include "../Core/AnimeHash.hlsl"

// How far the outer glow reaches, in ring widths.
#define ANIME_RING_GLOW_WIDTHS 3.0
// Below this the hair's brightness is too low to take a hue from.
#define ANIME_RING_DARKEST 0.05

// At full breakup, how far below the band's middle a strand's spike reaches: from this share of the width to twice it.
#define ANIME_RING_SHORTEST 0.3
// At full breakup, how far a strand's top edge moves, as a share of the width.
#define ANIME_RING_TOP_JITTER 0.15

// Where a point is among the strands round the head: x how far across its strand (0 to 1), y and z its strand's own
// random values (0 to 1). The strands meet behind the head, where the seam can't be seen.
half3 AnimeRingStrand(float3 fromCentre, float3 right, float3 forward)
{
    float around = (atan2(dot(fromCentre, right), dot(fromCentre, forward)) * (0.5 / PI) + 0.5) * _RingBreakupCount;
    float index = floor(around);
    return half3(half(around - index), half(AnimeHash(index, 1.0)), half(AnimeHash(index, 2.0)));
}

// 1 inside a band centred at height, halfWidth either side, its edges softened by softness (of the half width) and never
// sharper than a pixel.
half AnimeRingBand(half coordinate, half height, half halfWidth, half softness)
{
    half soft = max(max(halfWidth * softness, half(fwidth(coordinate))), 1e-4h);
    half distance = abs(coordinate - height);
    return 1.0h - smoothstep(halfWidth - soft, halfWidth, distance);
}

// The stroke a strand draws at a coordinate (its latitude): from the band's top edge (moved a little by the breakup)
// down to its spike's tip (its own length by the breakup); full width less the gap above the band's middle, narrowing
// to the tip below it by the shape.
half AnimeRingStroke(half coordinate, half3 strand)
{
    half side = abs(strand.x * 2.0h - 1.0h);
    half top = _RingHeight + _RingWidth * (1.0h + (strand.y - 0.5h) * 2.0h * _RingBreakup * ANIME_RING_TOP_JITTER);
    half reach = _RingWidth * lerp(1.0h, lerp(ANIME_RING_SHORTEST, 2.0h, strand.z * strand.z), _RingBreakup);
    half tip = _RingHeight - reach;
    half along = saturate((_RingHeight - coordinate) / max(reach, 1e-4h));
    half wide = (1.0h - _RingStrandGap) * lerp(1.0h, 1.0h - along, _RingBreakupShape);
    half edge = max(half(fwidth(side)), 1e-3h);
    half across = 1.0h - smoothstep(wide, wide + edge, side);
    half pixel = max(half(fwidth(coordinate)), 1e-4h);
    half fade = max(_RingWidth * _RingSoftness, pixel);
    half below = smoothstep(tip - pixel, tip + fade, coordinate);
    half above = 1.0h - smoothstep(top - fade, top + pixel, coordinate);
    return below * above * across;
}

// How much ring a point has, 0 to about 1, before colour and strength.
half AnimeRingAmount(float3 positionWS, half3 normalWS, half3 viewWS)
{
    float3 fromCentre = positionWS - _AnimeHeadCentre.xyz;
    float3 up = _AnimeHeadUp.xyz, forward = _AnimeHeadForward.xyz, right = _AnimeHeadRight.xyz;
    half3 direction = normalize(lerp(normalWS, half3(normalize(fromCentre)), _RingSphereBlend));
    half3 axis = half3(normalize(up + forward * _RingTilt));
    half3 strand = AnimeRingStrand(fromCentre, right, forward);

    half fixedOnHead = AnimeRingStroke(dot(direction, axis), strand);
    half towardView = AnimeRingStroke(dot(normalWS, viewWS), strand) * smoothstep(0.0h, 0.3h, dot(direction, half3(up)));
    half ring = lerp(fixedOnHead, towardView, _RingFollowsView);
    half glowWidth = _RingWidth * ANIME_RING_GLOW_WIDTHS;
    ring = max(ring, _RingGlow * AnimeRingBand(dot(direction, axis), _RingHeight, glowWidth, 1.0h));
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
    colour = lerp(colour, colour * surface.albedo, _RingLift);
    surface.litHighlight = min(colour * (amount * _RingStrength), _RingBrightest);
}

#endif
