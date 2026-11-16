Shader "Hidden/AnimeShaders/Tests/SurfaceProbe"
{
    // A thin entry shader for the tests only: the core's forward pass through ANIME_CUSTOM_SURFACE, its hook tinting
    // the colour and tilting the normal toward world +X, as a ground, face or project shader would.
    Properties
    {
        [Header(Surface)]
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Tones)]
        _ShadowTint("Shadow Tint", Color) = (0.7, 0.72, 0.82, 1)
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.45
        _ShadowSoftness("Shadow Softness", Range(0, 1)) = 0.08
        _LightInfluence("Light Colour Influence", Range(0, 1)) = 1
        _AmbientStrength("Ambient Strength", Range(0, 1)) = 0.25

        [Header(Shadows and Lights)]
        _CastShadowStrength("Cast Shadow Strength", Range(0, 1)) = 1
        _ExtraLightStrength("Additional Light Strength", Range(0, 2)) = 1
        _ExtraLightCap("Additional Light Cap", Range(0, 2)) = 0.6

        [Header(Normal Map and Clipping)]
        [Normal] [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5

        [Header(Probe)]
        _ProbeTint("Probe Tint", Color) = (1, 1, 1, 1)
        _ProbeTilt("Probe Tilt Toward +X", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #define ANIME_NO_FADE
        #define ANIME_CUSTOM_SURFACE
        #define ANIME_ENTRY_PROPERTIES half4 _ProbeTint; half _ProbeTilt;
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex AnimeForwardVertex
            #pragma fragment AnimeForwardFragment
            #include "../../../../Shaders/Includes/Core/AnimeSurface.hlsl"

            void AnimeCustomSurface(float3 positionWS, float2 uv, inout AnimeSurface surface)
            {
                surface.albedo *= _ProbeTint.rgb;
                surface.normalWS = normalize(surface.normalWS + half3(_ProbeTilt, 0.0h, 0.0h));
            }

            #include "../../../../Shaders/Includes/Passes/AnimeForwardPass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
