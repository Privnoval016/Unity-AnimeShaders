Shader "Anime/Eyes"
{
    // Eyes and brows: Anime/Character's tones, cast shadows, fresnel rim and emission (no line or depth rim), drawn before the hair (Queue
    // Geometry+10; Anime/Hair is Geometry+20) so they can show through their own fringe. EyeMark (SRPDefaultUnlit) marks
    // the stencil bit (Eye Stencil Bit) where they show and their fringe lies within Eye Hair Distance in front of them;
    // AnimeHairDepthFeature then draws EyesOverHair (LightMode AnimeEyesOverHair) on the marked pixels, blended over the
    // hair by Hair Over Eyes (0 fully through, 1 hidden). Without the feature they are hidden by hair as any opaque.
    // Variants: as Anime/Character's forward pass, twice (the blended pass has the same keywords).
    Properties
    {
        [AnimeSection(Surface)]
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] [AnimeOwn] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [AnimeSection(Tones)]
        _ShadowTint("Shadow Tint", Color) = (0.62, 0.6, 0.72, 1)
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.5
        _ShadowSoftness("Shadow Softness", Range(0, 1)) = 0.08
        _LightInfluence("Light Colour Influence", Range(0, 1)) = 1
        _AmbientStrength("Ambient Strength", Range(0, 1)) = 0.25

        [AnimeSection(Through Hair)]
        _HairOverEyes("Hair Over Eyes", Range(0, 1)) = 0.6
        _EyeYawLimit("Eye Yaw Limit", Range(1, 90)) = 70
        _EyePitchLimit("Eye Pitch Limit", Range(1, 90)) = 45
        _EyeHairDistance("Eye Hair Distance", Range(0.005, 0.5)) = 0.15
        _EyeHairFade("Eye Hair Fade", Range(0, 0.2)) = 0.03
        _EyeThinHair("Eye Thin Hair", Range(0, 16)) = 0
        _EyeThinHairNeed("Eye Thin Hair Need", Range(0.05, 1)) = 0.5
        _EyeThroughTint("Eye Through Tint", Color) = (1, 1, 1, 1)
        [Enum(Bit0, 1, Bit1, 2, Bit2, 4, Bit3, 8, Bit4, 16, Bit5, 32, Bit6, 64)] _EyeStencilBit("Eye Stencil Bit", Float) = 16

        [AnimeSection(Shadows and Lights)]
        [ToggleOff(_RECEIVE_SHADOWS_OFF)] _ReceiveShadows("Receive Shadows", Float) = 1
        _CastShadowStrength("Cast Shadow Strength", Range(0, 1)) = 1
        _CloudShadow("Cloud Shadow", Range(0, 1)) = 1
        _ContactShading("Contact Shading", Range(0, 1)) = 0.3
        _ContactThreshold("Contact Threshold", Range(0, 1)) = 0.6
        [Toggle(_ANIME_BODY_SHADOW)] _BodyShadow("Shadow The Whole Body", Float) = 0
        [AnimeShowWith(_BodyShadow)] _BodyShadowReach("Body Shadow Reach (m toward the light)", Range(0, 5)) = 1.5

        [Toggle(_ANIME_EXTRA_LIGHTS)] _ExtraLights("Use Additional Lights", Float) = 0
        [AnimeShowWith(_ExtraLights)] _ExtraLightStrength("Additional Light Strength", Range(0, 2)) = 1
        [AnimeShowWith(_ExtraLights)] _ExtraLightCap("Additional Light Cap", Range(0, 2)) = 0.6

        [AnimeSection(Colour)]
        _ColourBrightness("Brightness", Range(0, 2)) = 1
        _ColourSaturation("Saturation", Range(0, 2)) = 1

        [AnimeSection(Rim)]
        [Toggle(_ANIME_RIM)] _Rim("Use Fresnel Rim", Float) = 0
        [HDR] _RimColor("Rim Colour", Color) = (1, 1, 1, 1)
        [AnimeShowWith(_Rim)] _RimWidth("Fresnel Rim Width", Range(0, 1)) = 0.3
        _RimSoftness("Rim Softness", Range(0, 1)) = 0.15
        _RimStrength("Rim Strength", Range(0, 2)) = 0.5
        _RimLightMask("Rim Lit Side Only", Range(0, 1)) = 0.5
        [HideInInspector] _RimDepth("Use Depth Edge Rim", Float) = 0
        [HideInInspector] _RimDepthWidth("Depth Rim Width (px at 1080p)", Range(0, 12)) = 3
        [HideInInspector] _RimDepthThreshold("Depth Rim Threshold (m)", Range(0.01, 2)) = 0.15

        [AnimeSection(Emission)]
        [Toggle(_ANIME_EMISSION)] [AnimeSectionSwitch] _Emission("Use Emission", Float) = 0
        [HDR] [AnimeShowWith(_Emission)] _EmissionColor("Emission Colour", Color) = (0, 0, 0, 1)
        [Toggle(_ANIME_EMISSION_MAP)] [AnimeShowWith(_Emission)] _EmissionMapOn("Use Emission Map", Float) = 0
        [NoScaleOffset] [AnimeShowWith(_EmissionMapOn)] _EmissionMap("Emission Map", 2D) = "white" {}

        [AnimeSection(Normal Map and Clipping)]
        [Toggle(_NORMALMAP)] _NormalMapOn("Use Normal Map", Float) = 0
        [Normal] [NoScaleOffset] [AnimeShowWith(_NormalMapOn)] _BumpMap("Normal Map", 2D) = "bump" {}
        [AnimeShowWith(_NormalMapOn)] [AnimeOwn] _BumpScale("Normal Scale", Float) = 1

        [Toggle(_ALPHATEST_ON)] _AlphaClip("Use Alpha Clip", Float) = 0
        [AnimeShowWith(_AlphaClip)] [AnimeOwn] _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5

        [Enum(UnityEngine.Rendering.CullMode)] [AnimeOwn] _Cull("Cull (Off = two-sided)", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry+10"
            "IgnoreProjector" = "True"
            "AnimePreview" = "Ball"
        }
        LOD 300

        HLSLINCLUDE
        #define ANIME_GROUP_BODY
        #define ANIME_GROUP_EMISSION
        #define ANIME_GROUP_RIM
        #define ANIME_GROUP_COLOUR
        #define ANIME_GROUP_EYES
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
            #pragma dynamic_branch_local_fragment _ _ANIME_RIM
            #pragma dynamic_branch_local_fragment _ _ANIME_EMISSION
            #pragma dynamic_branch_local_fragment _ _ANIME_EMISSION_MAP
            #pragma dynamic_branch_local_fragment _ _ANIME_BODY_SHADOW

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

            #include "Includes/Passes/AnimeForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "EyeMark"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            ZWrite Off
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]
            Stencil
            {
                Ref [_EyeStencilBit]
                WriteMask [_EyeStencilBit]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex AnimeEyeMarkVertex
            #pragma fragment AnimeEyeMarkFragment

            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #include "Includes/Passes/AnimeEyeMarkPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "EyesOverHair"
            Tags { "LightMode" = "AnimeEyesOverHair" }

            Stencil
            {
                Ref [_EyeStencilBit]
                ReadMask [_EyeStencilBit]
                Comp Equal
            }
            Blend SrcAlpha OneMinusSrcAlpha, Zero One

            ZWrite Off
            ZTest Always
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex AnimeForwardVertex
            #pragma fragment AnimeForwardFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ANIME_EXTRA_LIGHTS
            #pragma dynamic_branch_local_fragment _ _ANIME_RIM
            #pragma dynamic_branch_local_fragment _ _ANIME_EMISSION
            #pragma dynamic_branch_local_fragment _ _ANIME_EMISSION_MAP
            #pragma dynamic_branch_local_fragment _ _ANIME_BODY_SHADOW

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

            #define ANIME_FORWARD_ALPHA AnimeEyesThrough(input.positionWS, inputData.normalizedScreenSpaceUV)
            #define ANIME_EYES_OVER_HAIR
            #define ANIME_CUSTOM_SURFACE
            #include "Includes/Character/AnimeEyes.hlsl"
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
