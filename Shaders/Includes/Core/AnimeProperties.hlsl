#ifndef ANIME_PROPERTIES_INCLUDED
#define ANIME_PROPERTIES_INCLUDED

// The one UnityPerMaterial block of every pass of every pack shader. Every pass includes this file, never its own
// copy, or the SRP Batcher drops the shader. Do not wrap a property in #if: the layout must be identical in every
// variant and pass. Textures are declared outside the block. A shader opts into feature groups (below) in its
// HLSLINCLUDE; its own extra properties go through ANIME_ENTRY_PROPERTIES, never a second UnityPerMaterial block.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// Cheap material toggles are dynamic branches (one variant, a uniform branch): the keyword reads as a bool when
// the shader declares it with dynamic_branch_local, or folds to a constant when an entry shader declares it static.
#if defined(_ANIME_BAND2_KEYWORD_DECLARED)
    #define ANIME_BAND2_ON (_ANIME_BAND2)
#elif defined(_ANIME_BAND2)
    #define ANIME_BAND2_ON true
#else
    #define ANIME_BAND2_ON false
#endif
#if defined(_ANIME_RIM_KEYWORD_DECLARED)
    #define ANIME_RIM_ON (_ANIME_RIM)
#elif defined(_ANIME_RIM)
    #define ANIME_RIM_ON true
#else
    #define ANIME_RIM_ON false
#endif
#if defined(_ANIME_EMISSION_KEYWORD_DECLARED)
    #define ANIME_EMISSION_ON (_ANIME_EMISSION)
#elif defined(_ANIME_EMISSION)
    #define ANIME_EMISSION_ON true
#else
    #define ANIME_EMISSION_ON false
#endif
#if defined(_ANIME_EMISSION_MAP_KEYWORD_DECLARED)
    #define ANIME_EMISSION_MAP_ON (_ANIME_EMISSION_MAP)
#elif defined(_ANIME_EMISSION_MAP)
    #define ANIME_EMISSION_MAP_ON true
#else
    #define ANIME_EMISSION_MAP_ON false
#endif
#if defined(_ANIME_INSIDE_TONE_KEYWORD_DECLARED)
    #define ANIME_INSIDE_TONE_ON (_ANIME_INSIDE_TONE)
#elif defined(_ANIME_INSIDE_TONE)
    #define ANIME_INSIDE_TONE_ON true
#else
    #define ANIME_INSIDE_TONE_ON false
#endif
#if defined(_ANIME_BODY_SHADOW_KEYWORD_DECLARED)
    #define ANIME_BODY_SHADOW_ON (_ANIME_BODY_SHADOW)
#elif defined(_ANIME_BODY_SHADOW)
    #define ANIME_BODY_SHADOW_ON true
#else
    #define ANIME_BODY_SHADOW_ON false
#endif
#if defined(_ANIME_WORLD_UV_KEYWORD_DECLARED)
    #define ANIME_WORLD_UV_ON (_ANIME_WORLD_UV)
#elif defined(_ANIME_WORLD_UV)
    #define ANIME_WORLD_UV_ON true
#else
    #define ANIME_WORLD_UV_ON false
#endif
#if defined(_ANIME_INSIDE_FADE_KEYWORD_DECLARED)
    #define ANIME_INSIDE_FADE_ON (_ANIME_INSIDE_FADE)
#elif defined(_ANIME_INSIDE_FADE)
    #define ANIME_INSIDE_FADE_ON true
#else
    #define ANIME_INSIDE_FADE_ON false
#endif

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap);
TEXTURE2D(_EmissionMap);
#if defined(ANIME_GROUP_GROUND)
// The ground's two layers' packed detail maps (R shade, G mask, B and A the normal's X and Y), each with its own sampler.
TEXTURE2D(_FieldDetail);
SAMPLER(sampler_FieldDetail);
TEXTURE2D(_CircleDetail);
SAMPLER(sampler_CircleDetail);
// The layers' stone maps (R the stone's value, G its home-tile code), read point-sampled through URP's global
// sampler_PointRepeat (GlobalSamplers.hlsl).
TEXTURE2D(_FieldStones);
TEXTURE2D(_CircleStones);
#endif
#if defined(ANIME_GROUP_HAIR)
// The angel ring's optional mask (R), read through the base map's sampler.
TEXTURE2D(_RingMask);
#endif
#if defined(ANIME_GROUP_FACE)
// The face map (AnimeFaceMapBaker: R and G the horizontal normal in the head's frame, B the face weight, A coverage),
// read through URP's global sampler_LinearClamp.
TEXTURE2D(_FaceMap);
#endif

CBUFFER_START(UnityPerMaterial)
    // The core every lit pack shader has.
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _ShadowTint;
    half _Cutoff;
    half _BumpScale;
    half _ShadowThreshold;
    half _ShadowSoftness;
    half _CastShadowStrength;
    half _CloudShadow;
    half _ContactShading;
    half _ContactThreshold;
    half _LightInfluence;
    half _AmbientStrength;
    half _ExtraLightStrength;
    half _ExtraLightCap;

    // Feature groups, opted into by an entry shader's HLSLINCLUDE (#define ANIME_GROUP_...), the same in every pass:
    // the SRP Batcher needs each variable here declared in the shader's Properties, so a shader carries only its own.
#if defined(ANIME_GROUP_BAND)
    half4 _Band2Tint;
    half _Band2Offset;
    half _Band2Softness;
#endif
#if defined(ANIME_GROUP_BODY)
    half _BodyShadowReach;
#endif
#if defined(ANIME_GROUP_EMISSION)
    half4 _EmissionColor;
#endif
#if defined(ANIME_GROUP_INSIDE)
    half4 _InsideTint;
    half _InsideDarken;
    half _InsideSaturation;
    half _InsideFadeAxis;
    half _InsideFadeStart;
    half _InsideFadeEnd;
#endif
#if defined(ANIME_GROUP_RIM)
    half4 _RimColor;
    half _RimWidth;
    half _RimSoftness;
    half _RimStrength;
    half _RimLightMask;
    half _RimDepth;
    float _RimDepthWidth;
    float _RimDepthThreshold;
#endif
#if defined(ANIME_GROUP_GROUND)
    half4 _FieldDark;
    half4 _FieldLight;
    half4 _FieldSecondDark;
    half4 _FieldSecondLight;
    half4 _CircleDark;
    half4 _CircleLight;
    half4 _CircleSecondDark;
    half4 _CircleSecondLight;
    half4 _EarthColor;
    half4 _PatchTint;
    float4 _GroundCircle;
    float _FieldTile;
    float _CircleTile;
    half _FieldSteps;
    half _CircleSteps;
    half _FieldNormal;
    half _CircleNormal;
    half _FieldBreakUp;
    half _CircleBreakUp;
    half _FieldStoneShade;
    half _CircleStoneShade;
    half _FieldStoneMix;
    half _CircleStoneMix;
    half _FieldStoneJoint;
    half _CircleStoneJoint;
    float _BreakUpScale;
    float _BreakUpAngle;
    float _PatchScale;
    half _PatchStrength;
    half _EarthAmount;
    float _PatchTintScale;
    half _PatchTintCover;
    half _PatchTintSoftness;
    float _EdgeSoftness;
    float _DetailFadeStart;
    float _DetailFadeRange;
#endif
#if defined(ANIME_GROUP_FACE)
    half _FaceMapStrength;
    half _FaceShadowSoftness;
    half _FaceShadowBias;
    half _FaceLightHeight;
    half _HairShadowStrength;
    float _HairShadowReach;
    float _HairShadowBias;
    float _HairShadowSoftness;
    float _HairShadowRange;
#endif
#if defined(ANIME_GROUP_HAIR)
    half4 _RingColor;
    half _RingStrength;
    half _RingHairTint;
    half _RingLightColor;
    half _RingHeight;
    half _RingWidth;
    half _RingSoftness;
    half _RingTilt;
    half _RingSphereBlend;
    half _RingFacing;
    half _RingFollowsView;
    half _RingGlow;
    half _RingBreakup;
    half _RingBreakupCount;
    half _RingBreakupShape;
    half _RingStrandGap;
    half _RingLift;
    half _RingBrightest;
#endif
#if defined(ANIME_GROUP_EYES)
    half4 _EyeThroughTint;
    half _HairOverEyes;
    float _EyeHairDistance;
    float _EyeHairFade;
    half _EyeYawLimit;
    half _EyePitchLimit;
    float _EyeThinHair;
    half _EyeThinHairNeed;
    float _EyeStencilBit;
#endif
#if defined(ANIME_GROUP_LINE)
    half4 _OutlineColor;
    float _OutlineWidth;
    float _OutlineDistanceMin;
    float _OutlineDistanceMax;
    float _OutlineZOffset;
    float _OutlineVertexZ;
    half _OutlineWidthChannel;
    half _OutlineZChannel;
    half _OutlineBaseTint;
#endif

    // An entry shader adds its own material properties by defining ANIME_ENTRY_PROPERTIES (declarations ending in
    // semicolons) in its SubShader's HLSLINCLUDE, so every one of its passes declares the same block. Its own
    // textures it declares itself (TEXTURE2D and SAMPLER, outside any block) after including the core.
#if defined(ANIME_ENTRY_PROPERTIES)
    ANIME_ENTRY_PROPERTIES
#endif
CBUFFER_END

#endif
