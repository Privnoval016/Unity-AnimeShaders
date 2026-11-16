#ifndef ANIME_FORWARD_PASS_INCLUDED
#define ANIME_FORWARD_PASS_INCLUDED

// The lit forward pass of every lit pack shader: vertex and fragment. An entry shader may define ANIME_CUSTOM_ALBEDO
// and, before this include, half3 AnimeCustomAlbedo(float3 positionWS, float2 uv, half3 baseAlbedo) to make its own
// surface colour (the ground does). Characters ignore lightmaps, reflection probes,
// decals and vertex lights; the ambient comes from the probe's average (or the director's tint).

#include "../Core/AnimeLighting.hlsl"
#include "../Core/AnimeFade.hlsl"

#if defined(_NORMALMAP)
    #define ANIME_REQUIRES_TANGENT_INTERPOLATOR
#endif

struct AnimeForwardAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct AnimeForwardVaryings
{
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
#if defined(ANIME_REQUIRES_TANGENT_INTERPOLATOR)
    half4 tangentWS : TEXCOORD3;
#endif
    half fogFactor : TEXCOORD5;
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord : TEXCOORD6;
#endif
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

AnimeForwardVaryings AnimeForwardVertex(AnimeForwardAttributes input)
{
    AnimeForwardVaryings output = (AnimeForwardVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    if (ANIME_WORLD_UV_ON)
        output.uv = vertexInput.positionWS.xz * _BaseMap_ST.xy + _BaseMap_ST.zw;
    output.positionWS = vertexInput.positionWS;
    output.normalWS = normalInput.normalWS;
#if defined(ANIME_REQUIRES_TANGENT_INTERPOLATOR)
    output.tangentWS = half4(normalInput.tangentWS.xyz, input.tangentOS.w * GetOddNegativeScale());
#endif
#if !defined(_FOG_FRAGMENT)
    output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif
    output.positionCS = AnimeFadeCull(vertexInput.positionCS);
    return output;
}

half3 AnimeSurfaceNormal(AnimeForwardVaryings input, half faceSign, float2 uv)
{
    half3 normalWS = half3(input.normalWS);
#if defined(_NORMALMAP)
    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BaseMap, uv), _BumpScale);
    float3 bitangent = input.tangentWS.w * cross(input.normalWS.xyz, input.tangentWS.xyz);
    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
    normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
#endif
    return NormalizeNormalPerPixel(normalWS) * faceSign;
}

void AnimeInitializeInputData(AnimeForwardVaryings input, half3 normalWS, out InputData inputData)
{
    inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.positionCS = input.positionCS;
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
}

void AnimeForwardFragment(
    AnimeForwardVaryings input,
    FRONT_FACE_TYPE isFront : FRONT_FACE_SEMANTIC,
    out half4 outColor : SV_Target0
#if defined(_WRITE_RENDERING_LAYERS)
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    AnimeFadeClip(input.positionCS);

    half4 baseSample = AnimeSampleBase(input.uv);
    half alpha = baseSample.a * _BaseColor.a;
    AnimeAlphaClip(alpha);

    half faceSign = IS_FRONT_VFACE(isFront, 1.0h, -1.0h);

    AnimeSurface surface;
    surface.albedo = baseSample.rgb * _BaseColor.rgb;
#if defined(ANIME_CUSTOM_ALBEDO)
    surface.albedo = AnimeCustomAlbedo(input.positionWS, input.uv, surface.albedo);
#endif
    surface.alpha = alpha;
#if defined(ANIME_GROUP_INSIDE)
    [branch] if (faceSign < 0.0h && ANIME_INSIDE_TONE_ON)
        surface.albedo = AnimeApplyInsideTone(surface.albedo, input.uv);
#endif
    surface.normalWS = AnimeSurfaceNormal(input, faceSign, input.uv);
    surface.emission = half3(0, 0, 0);
#if defined(ANIME_GROUP_EMISSION)
    if (ANIME_EMISSION_ON)
        surface.emission = AnimeSampleEmission(input.uv);
#endif

    InputData inputData;
    AnimeInitializeInputData(input, surface.normalWS, inputData);

    half3 color = AnimeShade(surface, inputData);
    color = MixFog(color, inputData.fogCoord);
    outColor = half4(color, 1.0h);

#if defined(_WRITE_RENDERING_LAYERS)
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif
