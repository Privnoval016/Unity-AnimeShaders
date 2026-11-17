#ifndef ANIME_SKY_INCLUDED
#define ANIME_SKY_INCLUDED

// Anime/Sky's pass: a skybox drawn on a sphere whose object-space position is the view direction. The gradient, the
// cloud field where the view ray meets its plane (the same clouds whose shadows the ground takes), stars at night, the
// sun's halo and disc, the haze at the sky's distance (so far ground and sky meet), and up to three ridges of hills on
// the horizon, each hazed at its own distance, farthest first. Every noise read is in uniform flow.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "../Core/AnimeClouds.hlsl"
#include "../Core/AnimeHash.hlsl"
#include "../Core/AnimeFog.hlsl"

#if defined(_ANIME_SKY_STARS_KEYWORD_DECLARED)
    #define ANIME_SKY_STARS_ON (_ANIME_SKY_STARS)
#elif defined(_ANIME_SKY_STARS)
    #define ANIME_SKY_STARS_ON true
#else
    #define ANIME_SKY_STARS_ON false
#endif

// How sharply the cloud's shade band turns, around the half-covered point toward the sun.
#define ANIME_SKY_SHADE_EDGE 0.5h
// How far below the horizon the below colour is fully reached (0 to 1 of the way down).
#define ANIME_SKY_BELOW_FADE 0.1
// The ridges' noise octaves fall off by this much each.
#define ANIME_SKY_RIDGE_PERSISTENCE 0.5
// How far below a ridge's line its sunlit edge reaches (in the same units as its height).
#define ANIME_SKY_RIDGE_EDGE 0.01


CBUFFER_START(UnityPerMaterial)
    half4 _ZenithColor;
    half4 _HorizonColor;
    half4 _BelowColor;
    half4 _SunColor;
    half4 _CloudLitColor;
    half4 _CloudShadeColor;
    half4 _StarColor;
    half4 _RidgeColor0;
    half4 _RidgeColor1;
    half4 _RidgeColor2;
    float4 _RidgeShape0;
    float4 _RidgeShape1;
    float4 _RidgeShape2;
    float4 _RidgeDistance;
    half4 _RidgeLitColor;
    float4 _RidgeMist;
    half _HorizonHeight;
    half _SunSize;
    half _SunStrength;
    half _SunHaloSize;
    half _SunHaloStrength;
    float _CloudShadeOffset;
    half _CloudHorizonFade;
    half _StarDensity;
    half _StarSize;
    float _StarScale;
    half _BloomCeiling;
CBUFFER_END

struct AnimeSkyAttributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct AnimeSkyVaryings
{
    float3 direction : TEXCOORD0;
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
};

AnimeSkyVaryings AnimeSkyVertex(AnimeSkyAttributes input)
{
    AnimeSkyVaryings output = (AnimeSkyVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.direction = input.positionOS.xyz;
    return output;
}

// The gradient alone: horizon to zenith above, horizon to the below colour under the horizon.
half3 AnimeSkyGradient(float height)
{
    if (height >= 0.0)
        return lerp(_HorizonColor.rgb, _ZenithColor.rgb, half(smoothstep(0.0, _HorizonHeight, height)));
    return lerp(_HorizonColor.rgb, _BelowColor.rgb, half(smoothstep(0.0, ANIME_SKY_BELOW_FADE, -height)));
}


// The dimmest a star shows, as a share of the brightest.
#define ANIME_SKY_STAR_DIMMEST 0.3
// Spreads a cell's coordinates into one hash input, so neighbouring cells never share a value.
#define ANIME_SKY_STAR_ROW 157.0

// A field of stars over <p>: each cell may hold one star (kept by density), anywhere in it, with its own brightness and
// a radius of <size> cells; the cells round it are checked too, so stars near an edge are whole and nothing lines up.
// Edges are softened over a pixel.
half AnimeSkyStars(float2 p, half density, half size)
{
    float2 cell = floor(p);
    float pixel = max(length(fwidth(p)), 1e-4);
    half brightest = 0.0h;
    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 neighbour = cell + float2(x, y);
            float key = neighbour.x + neighbour.y * ANIME_SKY_STAR_ROW;
            float2 centre = neighbour + float2(AnimeHash(key, 2.0), AnimeHash(key, 3.0));
            float distance = length(p - centre);
            half shown = AnimeHash(key, 1.0) < density ? 1.0h : 0.0h;
            half glow = half(smoothstep(size + pixel, max(size - pixel, 0.0), distance));
            half light = half(lerp(ANIME_SKY_STAR_DIMMEST, 1.0, AnimeHash(key, 4.0)));
            brightest = max(brightest, glow * light * shown);
        }
    }
    return brightest;
}

// Smooth value noise along a ring of <period> cells (it wraps), 0 to 1.
float AnimeSkyRingNoise(float position, float period, float seed)
{
    float cell = floor(position);
    float t = position - cell;
    t = t * t * (3.0 - 2.0 * t);
    float a = AnimeHash(fmod(cell, period), seed);
    float b = AnimeHash(fmod(cell + 1.0, period), seed);
    return lerp(a, b, t);
}

// A ridge's line at <around> (0 to 1 round the horizon): its height in view-direction units.
float AnimeSkyRidgeLine(float around, float4 shape)
{
    float hills = shape.z;
    float sum = 0.0, amplitude = 1.0, total = 0.0;
    for (int octave = 0; octave < 3; octave++)
    {
        float period = hills * exp2(float(octave));
        sum += amplitude * AnimeSkyRingNoise(around * period, period, shape.w + octave);
        total += amplitude;
        amplitude *= ANIME_SKY_RIDGE_PERSISTENCE;
    }
    return shape.x + shape.y * (sum / total * 2.0 - 1.0);
}

// Lays one ridge layer over <sky>: its colour (lit softly along its line where it faces the sun, shaded by the clouds'
// shadows where it stands) hazed at its distance,
// dissolving toward its foot into the haze at the sky's distance (the mist), so the far ground meets it softly.
half3 AnimeSkyRidge(half3 sky, half4 colour, float4 shape, float distance, float3 direction, float around, float3 sun)
{
    float below = AnimeSkyRidgeLine(around, shape) - direction.y;
    half cover = half(saturate(below / max(fwidth(below), 1e-5) + 0.5)) * colour.a;
    float2 flatSun = sun.xz / max(length(sun.xz), 1e-4);
    float2 flatView = direction.xz / max(length(direction.xz), 1e-4);
    half facing = half(saturate(dot(flatSun, flatView)));
    half edge = half(1.0 - saturate(below / ANIME_SKY_RIDGE_EDGE));
    half3 ridge = lerp(colour.rgb, _RidgeLitColor.rgb, facing * edge * _RidgeLitColor.a);
#if defined(_ANIME_CLOUD_SHADOWS_KEYWORD_DECLARED) || defined(_ANIME_CLOUD_SHADOWS)
    UNITY_BRANCH
    if (ANIME_CLOUD_SHADOWS_ON)
    {
        float3 slope = _WorldSpaceCameraPos + direction * distance;
        ridge *= 1.0h - AnimeCloudShadow(slope, sun);
    }
#endif
    half3 mist = _HorizonColor.rgb;
    if (ANIME_ATMOSPHERE_ON)
    {
        mist = AnimeHazeAlong(ridge, _WorldSpaceCameraPos, direction, _AnimeHazeSky.x);
        ridge = AnimeHazeAlong(ridge, _WorldSpaceCameraPos, direction, distance);
    }
    half foot = half(1.0 - smoothstep(0.0, max(_RidgeMist.x, 1e-4), direction.y)) * half(_RidgeMist.y);
    ridge = lerp(ridge, mist, foot);
    return lerp(sky, ridge, cover);
}

half4 AnimeSkyFragment(AnimeSkyVaryings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float3 direction = normalize(input.direction);
    float height = direction.y;
    float3 sun = normalize(_MainLightPosition.xyz);
    half3 sky = AnimeSkyGradient(height);

    float2 plane = AnimeCloudPlaneAlongView(_WorldSpaceCameraPos, direction);
    float2 flatSun = sun.xz / max(length(sun.xz), 1e-4);
    half cloud = AnimeCloudDensity(plane);
    half towardSun = AnimeCloudDensity(plane + flatSun * _CloudShadeOffset);
    half shadeWidth = max(half(fwidth(towardSun)), 1e-3h);
    half shaded = smoothstep(ANIME_SKY_SHADE_EDGE - shadeWidth, ANIME_SKY_SHADE_EDGE + shadeWidth, towardSun);
    half belowClouds = _WorldSpaceCameraPos.y < _AnimeCloudShape.w ? 1.0h : 0.0h;
    cloud *= half(smoothstep(0.0, _CloudHorizonFade, height)) * belowClouds;
    sky = lerp(sky, lerp(_CloudLitColor.rgb, _CloudShadeColor.rgb, shaded), cloud);

    if (ANIME_SKY_STARS_ON)
    {
        half star = AnimeSkyStars(direction.xz / (abs(height) + 1.0) * _StarScale, _StarDensity, _StarSize);
        sky += _StarColor.rgb * (star * (1.0h - cloud) * step(0.0, height));
    }

    float away = 1.0 - dot(direction, sun);
    half halo = half(exp(-away / max(_SunHaloSize * _SunHaloSize * 0.5, 1e-5))) * _SunHaloStrength;
    sky = min(sky + _SunColor.rgb * halo * (1.0h - cloud * 0.6h), _BloomCeiling);
    if (ANIME_ATMOSPHERE_ON)
        sky = AnimeHazeAlong(sky, _WorldSpaceCameraPos, direction, _AnimeHazeSky.x);

    float discEdge = _SunSize * _SunSize * 0.5;
    half disc = half(1.0 - smoothstep(discEdge * 0.8, discEdge, away)) * (1.0h - cloud * 0.9h);
    sky += _SunColor.rgb * (_SunStrength * disc);

    float around = atan2(direction.x, direction.z) / TWO_PI + 0.5;
    UNITY_BRANCH
    if (_RidgeColor0.a > 0.0h)
        sky = AnimeSkyRidge(sky, _RidgeColor0, _RidgeShape0, _RidgeDistance.x, direction, around, sun);
    UNITY_BRANCH
    if (_RidgeColor1.a > 0.0h)
        sky = AnimeSkyRidge(sky, _RidgeColor1, _RidgeShape1, _RidgeDistance.y, direction, around, sun);
    UNITY_BRANCH
    if (_RidgeColor2.a > 0.0h)
        sky = AnimeSkyRidge(sky, _RidgeColor2, _RidgeShape2, _RidgeDistance.z, direction, around, sun);

    return half4(sky, 1.0h);
}

#endif
