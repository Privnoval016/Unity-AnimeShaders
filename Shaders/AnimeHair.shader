Shader "Anime/Hair"
{
    // Hair: Anime/Character's look and passes, plus a HairDepth pass (LightMode AnimeHairDepth) that the Anime Hair
    // Depth renderer feature draws before the opaques, so a face on Anime/Face can take the front hair's shadow, and the
    // angel ring round the head frame (AnimeHeadFrame, the hair listed among its renderers). Group: hair. Variants: as
    // Anime/Character's; the ring adds none; the depth pass varies only by alpha clip. Queue Geometry+20: after the eyes and
    // brows (Anime/Eyes, Geometry+10), so they can mark where they show through it.
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

        [AnimeSection(Angel Ring)]
        _RingStrength("Ring Strength", Range(0, 3)) = 0.8
        _RingLift("Ring Lift", Range(0, 1)) = 1
        [HDR] _RingColor("Ring Colour", Color) = (1, 0.97, 0.92, 1)
        _RingHairTint("Ring Hair Tint", Range(0, 1)) = 0
        _RingLightColor("Ring Light Colour", Range(0, 1)) = 0.5
        _RingHeight("Ring Height", Range(-1, 1)) = 0.5
        _RingWidth("Ring Width (half the band)", Range(0.005, 0.3)) = 0.04
        _RingSoftness("Ring Softness", Range(0, 1)) = 1
        _RingTilt("Ring Tilt (toward the front)", Range(-1, 1)) = 0
        _RingSphereBlend("Ring Sphere Blend", Range(0, 1)) = 1
        _RingFacing("Ring Facing", Range(0, 1)) = 0
        _RingFollowsView("Ring Follows View", Range(0, 1)) = 0
        _RingGlow("Ring Outer Glow", Range(0, 1)) = 0.05
        _RingBreakup("Ring Breakup", Range(0, 1)) = 0
        _RingBreakupCount("Ring Breakup Count", Range(1, 300)) = 140
        _RingBreakupShape("Ring Breakup Shape", Range(0, 1)) = 0
        _RingStrandGap("Ring Strand Gap", Range(0, 0.6)) = 0
        _RingBrightest("Ring Brightest", Range(0, 2)) = 1
        [NoScaleOffset] _RingMask("Ring Mask (R, optional)", 2D) = "white" {}

        [AnimeSection(Second Shadow Band)]
        [Toggle(_ANIME_BAND2)] [AnimeSectionSwitch] _Band2("Use Second Band", Float) = 0
        [AnimeShowWith(_Band2)] _Band2Tint("Second Band Tint", Color) = (0.82, 0.8, 0.9, 1)
        [AnimeShowWith(_Band2)] _Band2Offset("Second Band Offset", Range(0, 0.5)) = 0.2
        [AnimeShowWith(_Band2)] _Band2Softness("Second Band Softness", Range(0, 1)) = 0.06

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
        [ToggleUI] _RimDepth("Use Depth Edge Rim", Float) = 0
        [AnimeShowWith(_RimDepth)] _RimDepthWidth("Depth Rim Width (px at 1080p)", Range(0, 12)) = 3
        [AnimeShowWith(_RimDepth)] _RimDepthThreshold("Depth Rim Threshold (m)", Range(0.01, 2)) = 0.15
        [Enum(Add, 1, Brighten, 2)] [AnimeShowWith(_RimDepth)] _RimDepthBlend("Depth Rim Blend", Float) = 1

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

        [AnimeSection(Two Sided)]
        [Enum(UnityEngine.Rendering.CullMode)] [AnimeOwn] _Cull("Cull (Off = two-sided)", Float) = 2
        [Toggle(_ANIME_INSIDE_TONE)] _InsideTone("Use Inside Tone", Float) = 0
        [AnimeShowWith(_InsideTone)] _InsideTint("Inside Tint", Color) = (0.85, 0.85, 0.95, 1)
        [AnimeShowWith(_InsideTone)] _InsideDarken("Inside Darken", Range(0, 1)) = 0.35
        [AnimeShowWith(_InsideTone)] _InsideSaturation("Inside Saturation", Range(0, 2)) = 0.9
        [Toggle(_ANIME_INSIDE_FADE)] [AnimeShowWith(_InsideTone)] _InsideFade("Fade Inside Tone Along UV", Float) = 0
        [Enum(U, 0, V, 1)] [AnimeShowWith(_InsideFade)] _InsideFadeAxis("Inside Fade Axis", Float) = 1
        [AnimeShowWith(_InsideFade)] _InsideFadeStart("Inside Fade Start (UV)", Float) = 0
        [AnimeShowWith(_InsideFade)] _InsideFadeEnd("Inside Fade End (UV)", Float) = 1

        [AnimeSection(Outline)]
        [AnimeSectionSwitch(Pass, SRPDefaultUnlit)] _OutlineColor("Line Colour", Color) = (0.1, 0.08, 0.1, 1)
        _OutlineWidth("Line Width", Range(0, 10)) = 1.5
        _OutlineDistanceMin("Line Near Distance", Float) = 1
        _OutlineDistanceMax("Line Far Distance", Float) = 12
        [AnimeOwn] _OutlineZOffset("Line Z Offset", Float) = 0.01
        [AnimeOwn] _OutlineOpenEdges("Line On Open Edges", Range(0, 1)) = 0
        [HideInInspector] _OutlineCull("Line Cull (kept in step by Line On Open Edges: Off with it on, else Front)", Float) = 1
        [Toggle(_ANIME_OUTLINE_BAKED)] _OutlineBakedNormals("Line From Baked Normals", Float) = 0
        [Toggle(_ANIME_OUTLINE_BASETINT)] _OutlineBaseTintOn("Tint Line By Base Map", Float) = 0
        [AnimeShowWith(_OutlineBaseTintOn)] _OutlineBaseTint("Line Base Tint", Range(0, 1)) = 0.5
        [Toggle(_ANIME_OUTLINE_VCOLOR)] _OutlineVertexColor("Line From Vertex Colour", Float) = 0
        [Enum(R, 0, G, 1, B, 2, A, 3)] [AnimeShowWith(_OutlineVertexColor)] [AnimeOwn] _OutlineWidthChannel("Line Width Channel (vertex colour)", Float) = 3
        [Enum(R, 0, G, 1, B, 2, A, 3)] [AnimeShowWith(_OutlineVertexColor)] [AnimeOwn] _OutlineZChannel("Line Z Channel (vertex colour)", Float) = 2
        [AnimeShowWith(_OutlineVertexColor)] [AnimeOwn] _OutlineVertexZ("Line Z Offset From Vertex Colour", Float) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry+20"
            "IgnoreProjector" = "True"
            "AnimePreview" = "Ball"
        }
        LOD 300

        HLSLINCLUDE
        #define ANIME_GROUP_BAND
        #define ANIME_GROUP_BODY
        #define ANIME_GROUP_EMISSION
        #define ANIME_GROUP_INSIDE
        #define ANIME_GROUP_RIM
        #define ANIME_GROUP_COLOUR
        #define ANIME_GROUP_LINE
        #define ANIME_GROUP_HAIR
        #define ANIME_CUSTOM_SURFACE
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
            #pragma dynamic_branch_local_fragment _ _ANIME_BAND2
            #pragma dynamic_branch_local_fragment _ _ANIME_RIM
            #pragma dynamic_branch_local_fragment _ _ANIME_EMISSION
            #pragma dynamic_branch_local_fragment _ _ANIME_EMISSION_MAP
            #pragma dynamic_branch_local_fragment _ _ANIME_INSIDE_TONE
            #pragma dynamic_branch_local_fragment _ _ANIME_INSIDE_FADE
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

            #include "Includes/Character/AnimeHair.hlsl"
            #include "Includes/Passes/AnimeForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            ZWrite On
            Cull [_OutlineCull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex AnimeOutlineVertex
            #pragma fragment AnimeOutlineFragment

            #pragma shader_feature_local_vertex _ANIME_OUTLINE_BAKED
            #pragma shader_feature_local_vertex _ANIME_OUTLINE_VCOLOR
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ANIME_OUTLINE_BASETINT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #pragma dynamic_branch_fragment _ _ANIME_ATMOSPHERE

            #include "Includes/Passes/AnimeOutline.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Rim"
            Tags { "LightMode" = "AnimeRim" }

            Blend [_RimDepthBlend] One
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex AnimeRimVertex
            #pragma fragment AnimeRimFragment

            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #include "Includes/Passes/AnimeRimPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "HairDepth"
            Tags { "LightMode" = "AnimeHairDepth" }

            ZWrite On
            ZTest LEqual
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex AnimeHairDepthVertex
            #pragma fragment AnimeHairDepthFragment

            #pragma shader_feature_local _ALPHATEST_ON

            #include "Includes/Passes/AnimeHairDepthPass.hlsl"
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
