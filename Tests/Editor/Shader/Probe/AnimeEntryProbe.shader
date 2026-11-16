Shader "Hidden/AnimeShaders/Tests/EntryProbe"
{
    // A thin entry shader for the tests only: the core's passes with one property of its own added through
    // ANIME_ENTRY_PROPERTIES, as a face, hair or project shader would.
    // Properties: the core's whole list (the SRP Batcher needs every per-material variable declared), then its own.
    Properties
    {
        [Header(Surface)]
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Tones)]
        _ShadowTint("Shadow Tint", Color) = (0.62, 0.6, 0.72, 1)
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.5
        _ShadowSoftness("Shadow Softness", Range(0, 1)) = 0.08
        _LightInfluence("Light Colour Influence", Range(0, 1)) = 1
        _AmbientStrength("Ambient Strength", Range(0, 1)) = 0.25

        [Header(Second Shadow Band)]
        [Toggle(_ANIME_BAND2)] _Band2("Use Second Band", Float) = 0
        _Band2Tint("Second Band Tint", Color) = (0.82, 0.8, 0.9, 1)
        _Band2Offset("Second Band Offset", Range(0, 0.5)) = 0.2
        _Band2Softness("Second Band Softness", Range(0, 1)) = 0.06

        [Header(Shadows and Lights)]
        [ToggleOff(_RECEIVE_SHADOWS_OFF)] _ReceiveShadows("Receive Shadows", Float) = 1
        _CastShadowStrength("Cast Shadow Strength", Range(0, 1)) = 1
        [Toggle(_ANIME_BODY_SHADOW)] _BodyShadow("Shadow The Whole Body", Float) = 0
        _BodyShadowReach("Body Shadow Reach (m toward the light)", Range(0, 5)) = 1.5

        [Toggle(_ANIME_EXTRA_LIGHTS)] _ExtraLights("Use Additional Lights", Float) = 0
        _ExtraLightStrength("Additional Light Strength", Range(0, 2)) = 1
        _ExtraLightCap("Additional Light Cap", Range(0, 2)) = 0.6

        [Header(Rim)]
        [Toggle(_ANIME_RIM)] _Rim("Use Fresnel Rim", Float) = 0
        [HDR] _RimColor("Rim Colour", Color) = (1, 1, 1, 1)
        _RimWidth("Fresnel Rim Width", Range(0, 1)) = 0.3
        _RimSoftness("Rim Softness", Range(0, 1)) = 0.15
        _RimStrength("Rim Strength", Range(0, 2)) = 0.5
        _RimLightMask("Rim Lit Side Only", Range(0, 1)) = 0.5
        [ToggleUI] _RimDepth("Use Depth Edge Rim", Float) = 0
        _RimDepthWidth("Depth Rim Width (px at 1080p)", Range(0, 12)) = 3
        _RimDepthThreshold("Depth Rim Threshold (m)", Range(0.01, 2)) = 0.15

        [Header(Emission)]
        [Toggle(_ANIME_EMISSION)] _Emission("Use Emission", Float) = 0
        [HDR] _EmissionColor("Emission Colour", Color) = (0, 0, 0, 1)
        [Toggle(_ANIME_EMISSION_MAP)] _EmissionMapOn("Use Emission Map", Float) = 0
        [NoScaleOffset] _EmissionMap("Emission Map", 2D) = "white" {}

        [Header(Normal Map and Clipping)]
        [Toggle(_NORMALMAP)] _NormalMapOn("Use Normal Map", Float) = 0
        [Normal] [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1

        [Toggle(_ALPHATEST_ON)] _AlphaClip("Use Alpha Clip", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5

        [Header(Two Sided)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull (Off = two-sided)", Float) = 2
        [Toggle(_ANIME_INSIDE_TONE)] _InsideTone("Use Inside Tone", Float) = 0
        _InsideTint("Inside Tint", Color) = (0.85, 0.85, 0.95, 1)
        _InsideDarken("Inside Darken", Range(0, 1)) = 0.35
        _InsideSaturation("Inside Saturation", Range(0, 2)) = 0.9
        [Toggle(_ANIME_INSIDE_FADE)] _InsideFade("Fade Inside Tone Along UV", Float) = 0
        [Enum(U, 0, V, 1)] _InsideFadeAxis("Inside Fade Axis", Float) = 1
        _InsideFadeStart("Inside Fade Start (UV)", Float) = 0
        _InsideFadeEnd("Inside Fade End (UV)", Float) = 1

        [Header(Line)]
        _OutlineColor("Line Colour", Color) = (0.1, 0.08, 0.1, 1)
        _OutlineWidth("Line Width (per mille of screen height)", Range(0, 10)) = 1.5
        _OutlineDistanceMin("Line Near Distance", Float) = 1
        _OutlineDistanceMax("Line Far Distance", Float) = 12
        _OutlineZOffset("Line Z Offset", Float) = 0.01
        [Toggle(_ANIME_OUTLINE_BAKED)] _OutlineBakedNormals("Line From Baked Normals", Float) = 0
        [Toggle(_ANIME_OUTLINE_BASETINT)] _OutlineBaseTintOn("Tint Line By Base Map", Float) = 0
        _OutlineBaseTint("Line Base Tint", Range(0, 1)) = 0.5
        [Toggle(_ANIME_OUTLINE_VCOLOR)] _OutlineVertexColor("Line From Vertex Colour", Float) = 0
        [Enum(R, 0, G, 1, B, 2, A, 3)] _OutlineWidthChannel("Line Width Channel (vertex colour)", Float) = 3
        [Enum(R, 0, G, 1, B, 2, A, 3)] _OutlineZChannel("Line Z Channel (vertex colour)", Float) = 2
        _OutlineVertexZ("Line Z Offset From Vertex Colour", Float) = 0.02

        _ProbeExtra("Probe Extra", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #define ANIME_GROUP_BAND
        #define ANIME_GROUP_BODY
        #define ANIME_GROUP_EMISSION
        #define ANIME_GROUP_INSIDE
        #define ANIME_GROUP_RIM
        #define ANIME_GROUP_LINE
        #define ANIME_ENTRY_PROPERTIES half _ProbeExtra;
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex AnimeForwardVertex
            #pragma fragment AnimeForwardFragment
            #include "../../../../Shaders/Includes/Passes/AnimeForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex AnimeOutlineVertex
            #pragma fragment AnimeOutlineFragment
            #include "../../../../Shaders/Includes/Passes/AnimeOutline.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex AnimeShadowVertex
            #pragma fragment AnimeShadowFragment
            #include "../../../../Shaders/Includes/Passes/AnimeShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
