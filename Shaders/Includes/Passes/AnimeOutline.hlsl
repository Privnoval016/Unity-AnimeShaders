#ifndef ANIME_OUTLINE_INCLUDED
#define ANIME_OUTLINE_INCLUDED

// The hull outline pass: the mesh drawn again with Cull Front, pushed out along a smoothed normal. With Line On Open
// Edges (two-sided cloth) it is drawn from both sides (_OutlineCull Off, kept in step by AnimeMaterials.SetOpenEdges and
// the inspector), the hull's part toward the camera turned away so it lies behind the sheet, and a vertex of an open
// edge (UV3.w, baked) is also pushed outward in its sheet's plane by the same width, so a hem seen face-on has its line.
// The Line Z Offset must still keep the hull behind a curved sheet where it widens past it.
//
// With _ANIME_OUTLINE_BAKED, outline normals come from TEXCOORD3, baked on import as a TANGENT-SPACE vector relative to
// the vertex's own normal, tangent and bitangent (AnimeShaders.EditorTools.OutlineNormalBaker). The vertex rebuilds the
// object-space normal from its own skinned frame, so the line follows skinning (UV channels are not skinned). Without
// the keyword the vertex normal is used: a mesh with no TEXCOORD3 is not read as zero on every API, so the channel is
// only read when the material says the mesh has it.
//
// Width is in thousandths of the screen height at the clamped view depth, so a line keeps its screen size under zoom
// and field of view. The Z offset pushes the hull away from the camera along the view ray; with the vertex colour
// option on, the width and the offset also read the chosen vertex colour channels.

#include "../Core/AnimeSurface.hlsl"
#if !defined(ANIME_GROUP_LINE)
    #error The line pass needs ANIME_GROUP_LINE in the shader's HLSLINCLUDE.
#endif
#include "../Core/AnimeDirector.hlsl"
#include "../Core/AnimeFade.hlsl"
#include "../Core/AnimeFog.hlsl"

// An outline channel w above this marks an open edge; its angle is w less the offset (AnimeShaderIds).
#define ANIME_OUTLINE_OPEN_EDGE_ABOVE 0.5
#define ANIME_OUTLINE_OPEN_EDGE_OFFSET 4.0

struct AnimeOutlineAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 texcoord : TEXCOORD0;
#if defined(_ANIME_OUTLINE_BAKED)
    float4 outlineNormal : TEXCOORD3;
#endif
    float4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct AnimeOutlineVaryings
{
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    half fogFactor : TEXCOORD2;
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

// The vertex's own frame: its normal, and its tangent made square to it (false when the tangent is unusable).
bool AnimeOutlineFrame(float3 normalOS, float4 tangentOS, out float3 t, out float3 b, out float3 n)
{
    n = normalize(normalOS);
    t = tangentOS.xyz - n * dot(n, tangentOS.xyz);
    bool usable = dot(t, t) > 1e-8;
    t = usable ? normalize(t) : float3(0.0, 0.0, 0.0);
    b = cross(n, t) * (tangentOS.w < 0.0 ? -1.0 : 1.0);
    return usable;
}

float3 AnimeOutlineNormalOS(float3 normalOS, float4 tangentOS, float3 packedNormal)
{
    float3 t, b, n;
    bool usable = AnimeOutlineFrame(normalOS, tangentOS, t, b, n) && dot(packedNormal, packedNormal) > 1e-6;
    float3 rebuilt = t * packedNormal.x + b * packedNormal.y + n * packedNormal.z;
    return usable ? normalize(rebuilt) : n;
}

// An open edge's outward direction in object space from its baked angle (UV3.w), or zero inside the mesh.
float3 AnimeOutlineOpenEdgeOS(float3 normalOS, float4 tangentOS, float packedAngle)
{
    float3 t, b, n;
    bool usable = AnimeOutlineFrame(normalOS, tangentOS, t, b, n) && packedAngle > ANIME_OUTLINE_OPEN_EDGE_ABOVE;
    float angle = packedAngle - ANIME_OUTLINE_OPEN_EDGE_OFFSET;
    return usable ? t * cos(angle) + b * sin(angle) : float3(0.0, 0.0, 0.0);
}

// The outline direction with its part toward the camera turned away (towardCamera unit length).
float3 AnimeOutlineAwayFromCamera(float3 normalWS, float3 towardCamera)
{
    return normalWS - 2.0 * max(0.0, dot(normalWS, towardCamera)) * towardCamera;
}

float AnimeOutlineWorldWidth(float3 positionWS)
{
    float scale;
    if (IsPerspectiveProjection())
    {
        float depth = -TransformWorldToView(positionWS).z;
        depth = clamp(depth, _OutlineDistanceMin, max(_OutlineDistanceMin, _OutlineDistanceMax));
        scale = 2.0 * depth / max(abs(UNITY_MATRIX_P[1][1]), 1e-4);
    }
    else
    {
        scale = 2.0 * unity_OrthoParams.y;
    }
    return _OutlineWidth * 0.001 * scale * AnimeDirectorOutlineWidthMultiplier();
}

// The open-edge push in world space, a unit direction whatever the object's scale; zero where the vertex has no baked
// smoothed normal (a mesh never baked reads its UV3 as anything) or is not on an open edge.
float3 AnimeOutlineOpenEdgeWS(float3 normalOS, float4 tangentOS, float4 packed)
{
    float3 outward = dot(packed.xyz, packed.xyz) > 1e-6 ? AnimeOutlineOpenEdgeOS(normalOS, tangentOS, packed.w) : float3(0, 0, 0);
    return dot(outward, outward) > 1e-12 ? TransformObjectToWorldDir(outward, true) : float3(0, 0, 0);
}

AnimeOutlineVaryings AnimeOutlineVertex(AnimeOutlineAttributes input)
{
    AnimeOutlineVaryings output = (AnimeOutlineVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
#if defined(_ANIME_OUTLINE_BAKED)
    float3 normalWS = TransformObjectToWorldNormal(AnimeOutlineNormalOS(input.normalOS, input.tangentOS, input.outlineNormal.xyz));
#else
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
#endif

    float width = AnimeOutlineWorldWidth(positionWS);
    float zOffset = _OutlineZOffset;
#if defined(_ANIME_OUTLINE_VCOLOR)
    width *= input.color[(uint)_OutlineWidthChannel];
    zOffset += input.color[(uint)_OutlineZChannel] * _OutlineVertexZ;
#endif

    float3 hullWS = positionWS + normalWS * width;
    UNITY_BRANCH
    if (_OutlineOpenEdges > 0.0)
    {
        hullWS = positionWS + AnimeOutlineAwayFromCamera(normalWS, GetWorldSpaceNormalizeViewDir(positionWS)) * width;
#if defined(_ANIME_OUTLINE_BAKED)
        float3 openEdgeWS = AnimeOutlineOpenEdgeWS(input.normalOS, input.tangentOS, input.outlineNormal);
        hullWS += openEdgeWS * (width * _OutlineOpenEdges);
#endif
    }
    hullWS -= GetWorldSpaceNormalizeViewDir(hullWS) * zOffset;

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.positionWS = hullWS;
    output.positionCS = TransformWorldToHClip(hullWS);
    output.fogFactor = ComputeFogFactor(output.positionCS.z);
    output.positionCS = AnimeFadeCull(output.positionCS);
    return output;
}

void AnimeOutlineFragment(
    AnimeOutlineVaryings input,
    out half4 outColor : SV_Target0
#if defined(_WRITE_RENDERING_LAYERS)
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    AnimeFadeClip(input.positionCS);

    half3 color = _OutlineColor.rgb;
#if defined(_ALPHATEST_ON) || defined(_ANIME_OUTLINE_BASETINT)
    half4 baseSample = AnimeSampleBase(input.uv);
    AnimeAlphaClip(baseSample.a * _BaseColor.a);
    #if defined(_ANIME_OUTLINE_BASETINT)
    color *= lerp(half3(1, 1, 1), baseSample.rgb * _BaseColor.rgb, _OutlineBaseTint);
    #endif
#endif

    half fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
    color = AnimeApplyFog(color, input.positionWS, fogCoord);
    outColor = half4(color, 1.0h);
#if defined(_WRITE_RENDERING_LAYERS)
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif
