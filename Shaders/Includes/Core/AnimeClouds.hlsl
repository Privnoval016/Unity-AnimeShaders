#ifndef ANIME_CLOUDS_INCLUDED
#define ANIME_CLOUDS_INCLUDED

// The cloud field: one layer of clouds on a world plane at the published altitude, drifting with game time. The sky
// draws it where the view ray meets the plane; a lit surface reads it where the ray from it toward the sun meets the
// plane, so a cloud's shadow lies under the cloud. Sampled in uniform flow (its edge is anti-aliased by its own
// derivative).

#include "AnimeWorld.hlsl"

// The lowest the sun's height is taken as when projecting shadows, so a sun on the horizon throws them far, not to infinity.
#define ANIME_CLOUD_MIN_SUN_HEIGHT 0.05
// The lowest a view ray's height is taken as when meeting the plane, for rays at the horizon.
#define ANIME_CLOUD_MIN_VIEW_HEIGHT 0.01

// How much cloud there is at a point of the cloud plane (world x and z), 0 to 1.
half AnimeCloudDensity(float2 planeXZ)
{
    float2 uv = (planeXZ - _AnimeCloudMotion.xy * _Time.y) * _AnimeCloudShape.x;
    half shape = SAMPLE_TEXTURE2D_BIAS(_AnimeCloudNoise, sampler_AnimeCloudNoise, uv, _AnimeCloudMotion.z).g;
    half threshold = 1.0h - half(_AnimeCloudShape.y);
    half width = max(max(half(fwidth(shape)), half(_AnimeCloudShape.z)), 1e-3h);
    half density = smoothstep(threshold - width, threshold + width, shape);
    return _AnimeCloudShape.y > 0.0 ? density : 0.0h;
}

// Where a view ray from <cameraPosition> in <direction> meets the cloud plane (rays at or below the horizon held just above it).
float2 AnimeCloudPlaneAlongView(float3 cameraPosition, float3 direction)
{
    float rise = max(direction.y, ANIME_CLOUD_MIN_VIEW_HEIGHT);
    return cameraPosition.xz + direction.xz * ((_AnimeCloudShape.w - cameraPosition.y) / rise);
}

// Where the ray from <positionWS> toward the sun meets the cloud plane.
float2 AnimeCloudPlaneTowardSun(float3 positionWS, float3 towardSun)
{
    float rise = max(towardSun.y, ANIME_CLOUD_MIN_SUN_HEIGHT);
    return positionWS.xz + towardSun.xz * ((_AnimeCloudShape.w - positionWS.y) / rise);
}

// How much a cloud shades <positionWS> from the sun, 0 to the published shadow strength (none below the plane's height or with the sun down).
half AnimeCloudShadow(float3 positionWS, float3 towardSun)
{
    half density = AnimeCloudDensity(AnimeCloudPlaneTowardSun(positionWS, towardSun));
    half above = positionWS.y < _AnimeCloudShape.w && towardSun.y > 0.0 ? 1.0h : 0.0h;
    return density * half(_AnimeCloudMotion.w) * above;
}

#endif
