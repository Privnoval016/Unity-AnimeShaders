#ifndef ANIME_HAIR_SHADOW_INCLUDED
#define ANIME_HAIR_SHADOW_INCLUDED

// The front hair's shadow on the face: the hair's depth (AnimeHairDepthFeature, half resolution, one over the eye depth,
// 0 for none) read at the face's point moved toward the light across the screen by Hair Shadow Reach (metres, so the
// same at any distance; it shrinks to nothing as the light lines up with the view). Where hair there is nearer than the
// face by more than Hair Shadow Bias, the face is in its shadow, softened over Hair Shadow Softness (metres) and faded
// near the screen's edge; hair farther in front than Hair Shadow Range (another's, metres away) casts none. The four nearest texels are each compared and the results blended by position (a bilinear
// comparison), so the edge is smooth across a texel instead of stepping at the half resolution: four reads. With no
// hair drawn, or without the feature, there is no shadow (AnimeHairDepth.hlsl).

#include "../Core/AnimeLighting.hlsl"
#include "AnimeHairDepth.hlsl"

// Within this share of the screen's edge, the shadow fades out (the moved point may fall off screen).
#define ANIME_HAIR_SHADOW_EDGE 0.02
// Over this last share of Hair Shadow Range, the shadow fades out.
#define ANIME_HAIR_SHADOW_RANGE_FADE 0.25

// How far the face at faceDepth is behind the hair at one texel of the hair's depth, 0 to 1.
half AnimeHairShadowTap(int2 texel, float faceDepth)
{
    float inFront = faceDepth - AnimeHairDepthAt(texel);
    float shade = saturate((inFront - _HairShadowBias) / max(_HairShadowSoftness, 1e-4));
    return half(shade * (1.0 - smoothstep(_HairShadowRange * (1.0 - ANIME_HAIR_SHADOW_RANGE_FADE), _HairShadowRange, inFront)));
}

// How far a face point is in the front hair's shadow, 0 to 1, before Hair Shadow Strength.
half AnimeHairShadow(float3 positionWS, float3 towardLight)
{
    float3 forward = GetViewForwardDir();
    float3 across = towardLight - dot(towardLight, forward) * forward;
    float4 movedCS = TransformWorldToHClip(positionWS + across * _HairShadowReach);
    if (movedCS.w <= 0.0)
        return 0.0h;
    float4 screen = ComputeScreenPos(movedCS);
    float2 uv = screen.xy / screen.w;

    float faceDepth = LinearEyeDepth(positionWS, GetWorldToViewMatrix());
    float2 position = uv * _AnimeHairDepthSize.xy - 0.5;
    int2 corner = int2(floor(position));
    half2 blend = half2(position - floor(position));
    half below = lerp(AnimeHairShadowTap(corner, faceDepth), AnimeHairShadowTap(corner + int2(1, 0), faceDepth), blend.x);
    half above = lerp(AnimeHairShadowTap(corner + int2(0, 1), faceDepth), AnimeHairShadowTap(corner + int2(1, 1), faceDepth), blend.x);
    half shade = lerp(below, above, blend.y);

    float2 edge = min(uv, 1.0 - uv);
    return shade * half(saturate(min(edge.x, edge.y) / ANIME_HAIR_SHADOW_EDGE));
}

#endif
