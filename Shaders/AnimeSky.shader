Shader "Anime/Sky"
{
    // The skybox: a gradient (zenith, a horizon band, below the horizon), the cloud field (the same clouds whose
    // shadows the ground takes, drawn where the view ray meets their plane), stars at night, the sun's halo and disc
    // where the main light points (or the light director's override), the haze at the sky's distance so far ground and
    // sky meet, and up to three ridges of hills on the horizon. Everything but the disc stays under the bloom ceiling.
    // A sky preset sets every value here and publishes the clouds and haze as the world's globals.
    Properties
    {
        [Header(Gradient)]
        _ZenithColor("Zenith", Color) = (0.27, 0.52, 0.9, 1)
        _HorizonColor("Horizon (also the haze colour)", Color) = (0.78, 0.87, 0.96, 1)
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
        _CloudShadeOffset("Cloud Shade Offset (m)", Float) = 40
        _CloudHorizonFade("Cloud Horizon Fade", Range(0.01, 0.5)) = 0.12

        [Header(Ridges)]
        _RidgeColor0("Ridge 1 Colour", Color) = (0, 0, 0, 0)
        _RidgeShape0("Ridge 1 (height, roughness, hills, seed)", Vector) = (0.05, 0.03, 8, 0)
        _RidgeColor1("Ridge 2 Colour", Color) = (0, 0, 0, 0)
        _RidgeShape1("Ridge 2 (height, roughness, hills, seed)", Vector) = (0.04, 0.02, 12, 1)
        _RidgeColor2("Ridge 3 Colour", Color) = (0, 0, 0, 0)
        _RidgeShape2("Ridge 3 (height, roughness, hills, seed)", Vector) = (0.03, 0.015, 16, 2)
        _RidgeDistance("Ridge Distances (m: 1, 2, 3)", Vector) = (1200, 700, 400, 0)
        _RidgeLitColor("Ridge Sunlit Edge (alpha: strength)", Color) = (1, 1, 1, 0.3)

        [Header(Night)]
        [Toggle(_ANIME_SKY_STARS)] _Stars("Use Stars", Float) = 0
        _StarColor("Star Colour", Color) = (0.9, 0.92, 1, 1)
        _StarDensity("Star Density", Range(0, 1)) = 0.2
        _StarSize("Star Size", Range(0, 1)) = 0.15
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
            #pragma dynamic_branch_fragment _ _ANIME_ATMOSPHERE

            #include "Includes/World/AnimeSky.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
