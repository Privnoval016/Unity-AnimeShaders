Shader "Anime/Ground"
{
    // Ground: two layers, the field and, inside an optional disc (a plaza, an arena, a ring; radius 0 for none), the
    // circle, each painted from a packed detail map (Tools > Anime Shaders: a Detail Set packs one) in world XZ: its
    // shade picks a colour between the layer's dark and light, or its second pair where the map's mask is off. Large
    // soft patches and bare earth from the pack's noise. Lit with the pack's two tones and cast shadows. No line, no
    // rim, no fade. Group: ground.
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
        [ToggleOff(_RECEIVE_SHADOWS_OFF)] _ReceiveShadows("Receive Shadows", Float) = 1
        _CastShadowStrength("Cast Shadow Strength", Range(0, 1)) = 1
        _CloudShadow("Cloud Shadow (how much the clouds shade it)", Range(0, 1)) = 1
        _ContactShading("Contact Shading (how much ambient occlusion shades it)", Range(0, 1)) = 1
        _ContactThreshold("Contact Threshold (occlusion turns to shade below it)", Range(0, 1)) = 0.6
        [Toggle(_ANIME_EXTRA_LIGHTS)] _ExtraLights("Use Additional Lights", Float) = 0
        _ExtraLightStrength("Additional Light Strength", Range(0, 2)) = 1
        _ExtraLightCap("Additional Light Cap", Range(0, 2)) = 0.6

        [Header(Normal Map and Clipping)]
        [Toggle(_NORMALMAP)] _NormalMapOn("Use Normal Map", Float) = 0
        [Normal] [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Use Alpha Clip", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull (Off = two-sided)", Float) = 2

        [Header(Field)]
        [NoScaleOffset] _FieldDetail("Field Detail (packed)", 2D) = "linearGrey" {}
        _FieldTile("Field Tile Size (m)", Range(0.1, 20)) = 2
        _FieldDark("Field Dark", Color) = (0.3, 0.45, 0.22, 1)
        _FieldLight("Field Light", Color) = (0.56, 0.68, 0.4, 1)
        _FieldSecondDark("Field Second Dark (mask off)", Color) = (0.36, 0.42, 0.22, 1)
        _FieldSecondLight("Field Second Light (mask off)", Color) = (0.6, 0.62, 0.36, 1)
        [IntRange] _FieldSteps("Field Shade Steps (0 smooth)", Range(0, 6)) = 0
        _FieldNormal("Field Normal Strength", Range(0, 2)) = 0.3
        _FieldBreakUp("Field Break Up Tiling", Range(0, 1)) = 1
        [NoScaleOffset] _FieldStones("Field Stone Map (from a detail set with Stones)", 2D) = "black" {}
        _FieldStoneShade("Field Stone Shade (each stone varies by up to this)", Range(0, 0.5)) = 0
        _FieldStoneMix("Field Stone Mix (share of stones in the second pair)", Range(0, 1)) = 0

        [Header(Circle)]
        [NoScaleOffset] _CircleDetail("Circle Detail (packed)", 2D) = "linearGrey" {}
        _CircleTile("Circle Tile Size (m)", Range(0.1, 20)) = 2
        _CircleDark("Circle Dark", Color) = (0.5, 0.48, 0.45, 1)
        _CircleLight("Circle Light", Color) = (0.8, 0.78, 0.73, 1)
        _CircleSecondDark("Circle Second Dark (mask off)", Color) = (0.5, 0.48, 0.45, 1)
        _CircleSecondLight("Circle Second Light (mask off)", Color) = (0.8, 0.78, 0.73, 1)
        [IntRange] _CircleSteps("Circle Shade Steps (0 smooth)", Range(0, 6)) = 0
        _CircleNormal("Circle Normal Strength", Range(0, 2)) = 0.5
        _CircleBreakUp("Circle Break Up Tiling", Range(0, 1)) = 0
        [NoScaleOffset] _CircleStones("Circle Stone Map (from a detail set with Stones)", 2D) = "black" {}
        _CircleStoneShade("Circle Stone Shade (each stone varies by up to this)", Range(0, 0.5)) = 0
        _CircleStoneMix("Circle Stone Mix (share of stones in the second pair)", Range(0, 1)) = 0

        [Header(Patches and Tiling)]
        _PatchScale("Patch Scale (per metre)", Float) = 0.04
        _PatchStrength("Patch Strength", Range(0, 1)) = 0.15
        _EarthColor("Bare Earth Colour", Color) = (0.55, 0.45, 0.32, 1)
        _EarthAmount("Bare Earth Amount", Range(0, 0.5)) = 0.06
        _PatchTint("Field Patch Tint (alpha: strength)", Color) = (0.42, 0.58, 0.36, 0)
        _PatchTintScale("Field Patch Tint Scale (per metre)", Float) = 0.01
        _PatchTintCover("Field Patch Tint Cover", Range(0, 1)) = 0.4
        _PatchTintSoftness("Field Patch Tint Softness", Range(0.01, 0.5)) = 0.15
        _BreakUpScale("Break Up Second Read Scale", Range(0.1, 4)) = 0.43
        _BreakUpAngle("Break Up Second Read Angle", Range(0, 360)) = 37

        [Header(Circle Edge)]
        _GroundCircle("Circle (centre x, centre z, radius, edge wobble)", Vector) = (0, 0, 15, 0.6)
        _EdgeSoftness("Circle Edge Softness (m)", Range(0.01, 5)) = 0.8

        [Header(Distance)]
        _DetailFadeStart("Detail Fade Start (m)", Float) = 25
        _DetailFadeRange("Detail Fade Range (m)", Float) = 40
        [NoScaleOffset] _AnimeNoiseTex("Noise (baked)", 2D) = "gray" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 300

        HLSLINCLUDE
        #define ANIME_GROUP_GROUND
        #define ANIME_CUSTOM_SURFACE
        #define ANIME_NO_FADE
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex AnimeForwardVertex
            #pragma fragment AnimeForwardFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ANIME_EXTRA_LIGHTS

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #pragma dynamic_branch_fragment _ _ANIME_ATMOSPHERE
            #pragma dynamic_branch_fragment _ _ANIME_CLOUD_SHADOWS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #include "Includes/World/AnimeGround.hlsl"
            #include "Includes/Passes/AnimeForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex AnimeShadowVertex
            #pragma fragment AnimeShadowFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Includes/Passes/AnimeShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex AnimeDepthVertex
            #pragma fragment AnimeDepthFragment

            #pragma shader_feature_local _ALPHATEST_ON

            #include "Includes/Passes/AnimeDepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex AnimeDepthNormalsVertex
            #pragma fragment AnimeDepthNormalsFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #include "Includes/Passes/AnimeDepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
    CustomEditor "AnimeShaders.EditorTools.AnimeShaderGUI"
}
