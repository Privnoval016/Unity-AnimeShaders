#ifndef ANIME_SKY_INCLUDED
#define ANIME_SKY_INCLUDED

// Anime/Sky's pass: a skybox drawn on a sphere whose object-space position is the view direction.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "../Core/AnimeDirector.hlsl"

#if defined(_ANIME_SKY_STARS_KEYWORD_DECLARED)
    #define ANIME_SKY_STARS_ON (_ANIME_SKY_STARS)
#elif defined(_ANIME_SKY_STARS)
    #define ANIME_SKY_STARS_ON true
#else
    #define ANIME_SKY_STARS_ON false
#endif

TEXTURE2D(_AnimeNoiseTex);
SAMPLER(sampler_AnimeNoiseTex);

CBUFFER_START(UnityPerMaterial)
    half4 _ZenithColor;
    half4 _HorizonColor;
    half4 _BelowColor;
    half4 _SunColor;
    half4 _CloudLitColor;
    half4 _CloudShadeColor;
    half4 _StarColor;
    float4 _CloudWind;
    half _HorizonHeight;
    half _SunSize;
    half _SunStrength;
    half _SunHaloSize;
    half _SunHaloStrength;
    half _CloudCover;
    half _CloudSoftness;
    float _CloudScale;
    float _CloudSpeed;
    half _StarDensity;
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

half4 AnimeSkyNoise(float2 position)
{
    return SAMPLE_TEXTURE2D(_AnimeNoiseTex, sampler_AnimeNoiseTex, position);
}

// The gradient alone: horizon to zenith above, horizon to the below colour under the horizon.
half3 AnimeSkyGradient(float height)
{
    if (height >= 0.0)
        return lerp(_HorizonColor.rgb, _ZenithColor.rgb, half(smoothstep(0.0, _HorizonHeight, height)));
    return lerp(_HorizonColor.rgb, _BelowColor.rgb, half(smoothstep(0.0, 0.1, -height)));
}

half4 AnimeSkyFragment(AnimeSkyVaryings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float3 direction = normalize(input.direction);
    float height = direction.y;
    float3 sun = AnimeDirectorLightDirection(_MainLightPosition.xyz);
    half3 sky = AnimeSkyGradient(height);

    half cloud = 0.0h;
    if (height > 0.0)
    {
        float2 drift = frac(_Time.y * _CloudSpeed * _CloudWind.xy);
        float2 cloudUV = direction.xz / (height + 0.15) * _CloudScale + drift;
        half shape = AnimeSkyNoise(cloudUV).g;
        half towardSun = AnimeSkyNoise(cloudUV + sun.xz * 0.04 * _CloudScale).g;
        half threshold = 1.0h - _CloudCover;
        cloud = smoothstep(threshold - _CloudSoftness, threshold + _CloudSoftness, shape);
        cloud *= half(smoothstep(0.0, 0.12, height));
        half shaded = smoothstep(0.45h, 0.55h, 0.5h + (towardSun - shape) * 6.0h);
        sky = lerp(sky, lerp(_CloudLitColor.rgb, _CloudShadeColor.rgb, shaded), cloud);
    }

    if (ANIME_SKY_STARS_ON && height > 0.0)
    {
        half4 field = AnimeSkyNoise(direction.xz / (height + 1.0) * _StarScale);
        half star = smoothstep(0.8h, 0.95h, field.b) * step(1.0h - _StarDensity, field.a);
        sky += _StarColor.rgb * (star * (1.0h - cloud));
    }

    float away = 1.0 - dot(direction, sun);
    half halo = half(exp(-away / max(_SunHaloSize * _SunHaloSize * 0.5, 1e-5))) * _SunHaloStrength;
    sky = min(sky + _SunColor.rgb * halo * (1.0h - cloud * 0.6h), _BloomCeiling);

    float discEdge = _SunSize * _SunSize * 0.5;
    half disc = half(1.0 - smoothstep(discEdge * 0.8, discEdge, away)) * (1.0h - cloud * 0.9h);
    sky += _SunColor.rgb * (_SunStrength * disc);

    return half4(sky, 1.0h);
}

#endif
