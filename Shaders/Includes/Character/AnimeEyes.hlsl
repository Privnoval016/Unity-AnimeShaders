#ifndef ANIME_EYES_INCLUDED
#define ANIME_EYES_INCLUDED

// How much an eye or brow shows over its fringe (Anime/Eyes' EyesOverHair pass), 0 to 1. The fringe stays over it by
// Hair Over Eyes at least, and grows opaque as the head turns from the camera: to fully opaque at Eye Yaw Limit degrees
// round (left or right) and at Eye Pitch Limit degrees up or down, each against the head frame's forward (Star Rail's
// community recreation uses 0.6, 70 and 45). It fades out softly over Eye Hair Fade before the fringe is Eye Hair
// Distance in front. Eye Thin Hair (texels, 0 off) shows it only where the fringe is thin: the hair's depth sampled on a
// ring round the pixel, full once Eye Thin Hair Need of the ring has none of its fringe over it (part of the eye already
// in view), none under a solid mass. The revealed eye is tinted by Eye Through Tint (white: none).

#include "../Core/AnimeLighting.hlsl"
#include "AnimeHead.hlsl"
#include "AnimeHairDepth.hlsl"

#define ANIME_EYES_RING_TAPS 8
// Hair nearer than the eye by less than this is the eye's own surface.
#define ANIME_EYES_SURFACE 1e-3

// How far toward opaque a turn makes the hair, 0 (facing) to 1 (at the limit): one less the cosine of the view's angle
// from the head's forward in a plane, over one less the cosine of the limit.
half AnimeEyesTurn(float3 toCamera, float3 across, float3 forward, half limitDegrees)
{
    float3 inPlane = toCamera - dot(toCamera, across) * across;
    float facing = max(0.0, dot(inPlane * rsqrt(max(dot(inPlane, inPlane), 1e-6)), forward));
    return half(saturate((1.0 - facing) / max(1.0 - cos(radians(limitDegrees)), 1e-3)));
}

// Whether the fringe is over the eye at a texel: hair in front of it, no farther than the distance.
half AnimeEyesFringeAt(int2 texel, float eyeDepth)
{
    float inFront = eyeDepth - AnimeHairDepthAt(texel);
    return inFront > ANIME_EYES_SURFACE && inFront < _EyeHairDistance ? 1.0h : 0.0h;
}

half AnimeEyesThrough(float3 positionWS, float2 screenUV)
{
    half hairOver = _HairOverEyes;
    UNITY_BRANCH
    if (AnimeHeadFrameSet())
    {
        float3 toCamera = normalize(GetCameraPositionWS() - positionWS);
        hairOver = max(hairOver, AnimeEyesTurn(toCamera, _AnimeHeadUp.xyz, _AnimeHeadForward.xyz, _EyeYawLimit));
        hairOver = max(hairOver, AnimeEyesTurn(toCamera, _AnimeHeadRight.xyz, _AnimeHeadForward.xyz, _EyePitchLimit));
    }
    half amount = 1.0h - hairOver;

    float eyeDepth = LinearEyeDepth(positionWS, GetWorldToViewMatrix());
    float2 centre = screenUV * _AnimeHairDepthSize.xy;
    float inFront = eyeDepth - AnimeHairDepthAt(int2(floor(centre)));
    amount *= 1.0h - half(smoothstep(_EyeHairDistance - _EyeHairFade, _EyeHairDistance, inFront));

    UNITY_BRANCH
    if (_EyeThinHair > 0.5)
    {
        half open = 0.0h;
        [unroll] for (int tap = 0; tap < ANIME_EYES_RING_TAPS; tap++)
        {
            float angle = tap * (TWO_PI / ANIME_EYES_RING_TAPS);
            open += 1.0h - AnimeEyesFringeAt(int2(floor(centre + float2(cos(angle), sin(angle)) * _EyeThinHair)), eyeDepth);
        }
        amount *= saturate(open / (ANIME_EYES_RING_TAPS * max(_EyeThinHairNeed, 0.01h)));
    }
    return amount;
}

#if defined(ANIME_EYES_OVER_HAIR)
// The over-hair pass's surface: the eye tinted as it shows through.
void AnimeCustomSurface(float3 positionWS, float2 uv, inout AnimeSurface surface)
{
    surface.albedo *= _EyeThroughTint.rgb;
}
#endif

#endif
