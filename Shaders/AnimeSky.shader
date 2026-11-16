Shader "Anime/Sky"
{
    // The skybox: a gradient (zenith, a horizon band, below the horizon), the sun disc and halo where the main light
    // points (the light's own direction, or the light director's override), cel clouds drifting with game time (frozen
    // with hitstop, like the world), stars at night. Everything but the disc stays under the bloom ceiling, so the sky
    // never blooms over the bodies and effects in front of it. Three noise samples a pixel at most, from the pack's baked
    // noise. Set it as the scene's skybox (a sky preset does).
    Properties
    {
        [Header(Gradient)]
        _ZenithColor("Zenith", Color) = (0.27, 0.52, 0.9, 1)
        _HorizonColor("Horizon (also the fog colour)", Color) = (0.78, 0.87, 0.96, 1)
        _BelowColor("Below The Horizon", Color) = (0.62, 0.68, 0.74, 1)
        _HorizonHeight("Horizon Band Height", Range(0.01, 1)) = 0.3

        [Header(Sun)]
        [HDR] _SunColor("Sun Colour", Color) = (1, 0.96, 0.88, 1)
        _SunSize("Sun Disc Size (radians)", Range(0, 0.2)) = 0.04
        _SunStrength("Sun Disc Strength (may bloom)", Range(0, 8)) = 2
        _SunHaloSize("Sun Halo Size (radians)", Range(0, 1)) = 0.25
        _SunHaloStrength("Sun Halo Strength", Range(0, 1)) = 0.3

        [Header(Clouds)]
        _CloudLitColor("Cloud Lit Colour", Color) = (1, 1, 1, 1)
        _CloudShadeColor("Cloud Shade Colour", Color) = (0.8, 0.84, 0.93, 1)
        _CloudCover("Cloud Cover", Range(0, 1)) = 0.45
        _CloudSoftness("Cloud Edge Softness", Range(0.001, 0.5)) = 0.05
        _CloudScale("Cloud Scale", Float) = 0.35
        _CloudSpeed("Cloud Drift Speed", Float) = 0.004
        _CloudWind("Cloud Wind (x, z)", Vector) = (1, 0.3, 0, 0)

        [Header(Night)]
        [Toggle(_ANIME_SKY_STARS)] _Stars("Use Stars", Float) = 0
        _StarColor("Star Colour", Color) = (0.9, 0.92, 1, 1)
        _StarDensity("Star Density", Range(0, 1)) = 0.2
        _StarScale("Star Scale", Float) = 4

        [Header(Bloom)]
        _BloomCeiling("Brightest Sky (keep under the bloom threshold)", Range(0, 1)) = 0.95

        [NoScaleOffset] _AnimeNoiseTex("Noise (baked)", 2D) = "gray" {}
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            Name "Sky"

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex AnimeSkyVertex
            #pragma fragment AnimeSkyFragment
            #pragma dynamic_branch_local_fragment _ _ANIME_SKY_STARS

            #include "Includes/World/AnimeSky.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
