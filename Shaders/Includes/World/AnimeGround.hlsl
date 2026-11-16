#ifndef ANIME_GROUND_INCLUDED
#define ANIME_GROUND_INCLUDED

// The ground's surface (ANIME_CUSTOM_SURFACE): two layers, the field and, inside an optional disc (a plaza, an arena,
// a ring; radius 0 for none), the circle. Each is painted from a packed detail map (AnimeDetailPacker: R shade with a
// mean of 0.5, G mask, B and A the normal's X and Y) in world XZ: its shade picks a colour between the layer's dark and
// light (or its second pair where the mask is off) and its normal tilts the surface. The pack's noise adds large soft
// patches, the field's tinted patches, bare earth and the circle's wobbly edge.
//
// A layer with a stone map (AnimeDetailPacker's, from a detail set with Stones) varies each stone on the floor: its
// shade (Stone Shade) and, for a share of stones, the second colour pair (Stone Mix). The map gives each texel its
// stone's value and the tile holding the stone's centre; hashed with that tile, every stone on the floor differs, and
// both pieces of a stone crossing a tile's edge agree.
//
// Reads: the noise once (twice with a patch tint), then each layer's map only where that layer shows (once, or twice
// with Break Up Tiling, and its stone map once, point-sampled, while it varies stones).
// Those reads sit in branches, so every gradient they use is taken before the branches and everything that needs a
// derivative (the steps, the lighting) runs after them.

#include "../Core/AnimeSurface.hlsl"

#if !defined(ANIME_GROUP_GROUND)
    #error The ground needs ANIME_GROUP_GROUND in the shader's HLSLINCLUDE.
#endif

TEXTURE2D(_AnimeNoiseTex);
SAMPLER(sampler_AnimeNoiseTex);

// Where the noise's value channel turns from the first read to the second when Break Up Tiling is on.
#define ANIME_GROUND_BREAK_UP_FROM 0.3h
#define ANIME_GROUND_BREAK_UP_TO 0.7h
// How sharply bare earth starts once the noise passes its amount.
#define ANIME_GROUND_EARTH_EDGE 0.05h
// The second read's offset in tiles, so the two reads never line up at the origin.
#define ANIME_GROUND_BREAK_UP_OFFSET 0.5
// The narrowest a step's soft edge gets, so a flat shade never meets a zero-width edge.
#define ANIME_GROUND_STEP_MIN_WIDTH 1e-3h
// The stone variation fades out in the joints: none where the shade is below FROM, full above TO.
#define ANIME_GROUND_GROUT_FROM 0.15h
#define ANIME_GROUND_GROUT_TO 0.35h
// The tangent turns from world X to world Z as the surface normal leans this far toward X, so no slope sees a seam.
#define ANIME_GROUND_FRAME_TURN_FROM 0.7
#define ANIME_GROUND_FRAME_TURN_TO 0.95

// A layer's painting settings, gathered so one function paints either layer.
struct AnimeGroundLayer
{
    half3 dark;
    half3 light;
    half3 secondDark;
    half3 secondLight;
    half steps;
    half stoneShade;
    half stoneMix;
};

struct AnimeGroundCoordinates
{
    float2 uv;
    float2 dx;
    float2 dy;
    float2 uv2;
    float2 dx2;
    float2 dy2;
};

// stoneShade is the stone's shade offset (-1 to 1), stoneKey its draw for the mix (0 to 1), stoneStrength how much of
// the variation applies (0 off the stones, less where Break Up Tiling blends in reads whose stones don't line up).
struct AnimeGroundSample
{
    half shade;
    half mask;
    half2 normalXY;
    half stoneShade;
    half stoneKey;
    half stoneStrength;
};

half4 AnimeGroundNoise(float2 position)
{
    return SAMPLE_TEXTURE2D(_AnimeNoiseTex, sampler_AnimeNoiseTex, position);
}

// The rotation from a layer's UV to its second read's.
float2x2 AnimeGroundBreakUpRotation()
{
    float s, c;
    sincos(radians(_BreakUpAngle), s, c);
    return float2x2(c, -s, s, c);
}

AnimeGroundLayer AnimeFieldLayer()
{
    AnimeGroundLayer layer;
    layer.dark = _FieldDark.rgb;
    layer.light = _FieldLight.rgb;
    layer.secondDark = _FieldSecondDark.rgb;
    layer.secondLight = _FieldSecondLight.rgb;
    layer.steps = round(_FieldSteps);
    layer.stoneShade = _FieldStoneShade;
    layer.stoneMix = _FieldStoneMix;
    return layer;
}

AnimeGroundLayer AnimeCircleLayer()
{
    AnimeGroundLayer layer;
    layer.dark = _CircleDark.rgb;
    layer.light = _CircleLight.rgb;
    layer.secondDark = _CircleSecondDark.rgb;
    layer.secondLight = _CircleSecondLight.rgb;
    layer.steps = round(_CircleSteps);
    layer.stoneShade = _CircleStoneShade;
    layer.stoneMix = _CircleStoneMix;
    return layer;
}

// A layer's UVs and their screen gradients, from the position's gradients taken once in uniform flow.
AnimeGroundCoordinates AnimeGroundCoordinatesOf(float2 positionXZ, float2 positionDx, float2 positionDy, float tile, float2x2 rotation)
{
    float scale = 1.0 / max(tile, 1e-3);
    AnimeGroundCoordinates coordinates;
    coordinates.uv = positionXZ * scale;
    coordinates.dx = positionDx * scale;
    coordinates.dy = positionDy * scale;
    coordinates.uv2 = mul(rotation, coordinates.uv) * _BreakUpScale + ANIME_GROUND_BREAK_UP_OFFSET;
    coordinates.dx2 = mul(rotation, coordinates.dx) * _BreakUpScale;
    coordinates.dy2 = mul(rotation, coordinates.dy) * _BreakUpScale;
    return coordinates;
}

AnimeGroundSample AnimeGroundUnpack(half4 packed)
{
    AnimeGroundSample unpacked;
    unpacked.shade = packed.r;
    unpacked.mask = packed.g;
    unpacked.normalXY = packed.ba * 2.0h - 1.0h;
    unpacked.stoneShade = 0.0h;
    unpacked.stoneKey = 1.0h;
    unpacked.stoneStrength = 0.0h;
    return unpacked;
}

// What a layer reads where it doesn't show: the neutral map's values (it is weighted 0 there).
AnimeGroundSample AnimeGroundNeutral()
{
    AnimeGroundSample neutral;
    neutral.shade = 0.5h;
    neutral.mask = 1.0h;
    neutral.normalXY = half2(0.0h, 0.0h);
    neutral.stoneShade = 0.0h;
    neutral.stoneKey = 1.0h;
    neutral.stoneStrength = 0.0h;
    return neutral;
}

// An integer hash (PCG3D): the same bits on every graphics API.
uint3 AnimeGroundHash(uint3 v)
{
    v = v * 1664525u + 1013904223u;
    v.x += v.y * v.z;
    v.y += v.z * v.x;
    v.z += v.x * v.y;
    v ^= v >> 16u;
    v.x += v.y * v.z;
    v.y += v.z * v.x;
    v.z += v.x * v.y;
    return v;
}

// The stone at a layer UV: its shade offset (-1 to 1), its mix draw (0 to 1) and 1, or (0, 1, 0) where the map has
// none. The map is read at the UV's place in its tile, so the tile index and the texel never disagree at the seam.
half3 AnimeGroundStoneAt(TEXTURE2D_PARAM(stones, stonesSampler), float2 uv)
{
    float2 tile = floor(uv);
    half2 texel = SAMPLE_TEXTURE2D_LOD(stones, stonesSampler, uv - tile, 0).rg;
    uint code = uint(round(texel.g * 255.0h));
    uint value = uint(round(texel.r * 255.0h));
    uint index = max(code, 1u) - 1u;
    int2 home = int2(tile) + int2(int(index / 3u) - 1, int(index % 3u) - 1);
    uint3 hash = AnimeGroundHash(uint3(asuint(home), value));
    float2 draws = float2(hash.xy) * (1.0 / 4294967296.0);
    return code == 0u ? half3(0.0h, 1.0h, 0.0h) : half3(half(draws.x * 2.0 - 1.0), half(draws.y), 1.0h);
}

// One layer's reads: one, or two blended by the noise where Break Up Tiling reaches, and its stone while it varies them. The blend keeps the shade's spread
// about its mean of 0.5 and the normal's about flat, so a mixed patch is as contrasted and as bumpy as a plain one; the
// second read's normal is turned back into the layer's frame.
AnimeGroundSample AnimeGroundRead(TEXTURE2D_PARAM(detail, detailSampler), TEXTURE2D_PARAM(stones, stonesSampler),
    AnimeGroundCoordinates coordinates, half breakUp, half breakUpNoise, float2x2 rotation, bool readStones)
{
    AnimeGroundSample first = AnimeGroundUnpack(SAMPLE_TEXTURE2D_GRAD(detail, detailSampler, coordinates.uv, coordinates.dx, coordinates.dy));
    if (readStones)
    {
        half3 stone = AnimeGroundStoneAt(TEXTURE2D_ARGS(stones, stonesSampler), coordinates.uv);
        first.stoneShade = stone.x;
        first.stoneKey = stone.y;
        first.stoneStrength = stone.z;
    }
    half weight = smoothstep(ANIME_GROUND_BREAK_UP_FROM, ANIME_GROUND_BREAK_UP_TO, breakUpNoise) * breakUp;
    UNITY_BRANCH
    if (weight > 0.0h)
    {
        AnimeGroundSample second = AnimeGroundUnpack(SAMPLE_TEXTURE2D_GRAD(detail, detailSampler, coordinates.uv2, coordinates.dx2, coordinates.dy2));
        second.normalXY = half2(mul(float2(second.normalXY), rotation));
        half spread = rsqrt(weight * weight + (1.0h - weight) * (1.0h - weight));
        first.shade = saturate((lerp(first.shade, second.shade, weight) - 0.5h) * spread + 0.5h);
        first.mask = lerp(first.mask, second.mask, weight);
        first.normalXY = lerp(first.normalXY, second.normalXY, weight) * spread;
        first.stoneStrength *= 1.0h - weight;
    }
    return first;
}

// The shade in a whole number of tones (fewer than 2: unchanged), each edge softened over about a pixel.
half AnimeGroundSteps(half shade, half steps)
{
    half intervals = max(steps - 1.0h, 1.0h);
    half scaled = shade * intervals;
    half width = clamp(half(fwidth(scaled)), ANIME_GROUND_STEP_MIN_WIDTH, 0.5h);
    half below = floor(scaled);
    half stepped = saturate((below + smoothstep(0.5h - width, 0.5h + width, scaled - below)) / intervals);
    return steps >= 2.0h ? stepped : shade;
}

// Between two colours by a perceptual shade: the colours' square roots are blended, then squared.
half3 AnimeGroundGradient(half3 dark, half3 light, half shade)
{
    half3 blended = lerp(sqrt(dark), sqrt(light), shade);
    return blended * blended;
}

// The stone's variation applies near (it fades with the detail) and off the joints, so it never steps where grout is.
half3 AnimeGroundPaint(AnimeGroundSample sample, AnimeGroundLayer layer, half shift, half near)
{
    half stone = sample.stoneStrength * near * smoothstep(ANIME_GROUND_GROUT_FROM, ANIME_GROUND_GROUT_TO, sample.shade);
    half shade = saturate(sample.shade + shift + sample.stoneShade * layer.stoneShade * stone);
    shade = lerp(shade, AnimeGroundSteps(shade, layer.steps), near);
    half mixed = sample.stoneKey < layer.stoneMix ? stone : 0.0h;
    half3 first = AnimeGroundGradient(layer.dark, layer.light, shade);
    half3 second = AnimeGroundGradient(layer.secondDark, layer.secondLight, shade);
    return lerp(second, first, sample.mask * (1.0h - mixed));
}

// The surface normal tilted by a detail normal's X and Y. The frame: world X made perpendicular to the normal (turning
// smoothly to world Z as the normal leans toward X) and the bitangent cross(tangent, normal), which is +Z on a flat
// floor, where V runs.
half3 AnimeGroundTilt(half3 normalWS, half2 normalXY, half strength)
{
    float3 normal = normalWS;
    float turn = smoothstep(ANIME_GROUND_FRAME_TURN_FROM, ANIME_GROUND_FRAME_TURN_TO, abs(normal.x));
    float3 axis = normalize(lerp(float3(1.0, 0.0, 0.0), float3(0.0, 0.0, 1.0), turn));
    float3 tangent = normalize(axis - normal * dot(axis, normal));
    float3 bitangent = cross(tangent, normal);
    float z = sqrt(saturate(1.0 - dot(float2(normalXY), float2(normalXY))));
    float2 tilt = float2(normalXY) * strength;
    return half3(normalize(tangent * tilt.x + bitangent * tilt.y + normal * z));
}

// The field's tinted patches: where the noise's fbm, read at the tint's own scale, passes 1 - cover, the colour takes
// the tint's hue and keeps its own brightness, by the tint's alpha.
half3 AnimeGroundPatchTint(half3 colour, half value)
{
    half from = 1.0h - _PatchTintCover;
    half weight = smoothstep(from, from + max(_PatchTintSoftness, ANIME_GROUND_STEP_MIN_WIDTH), value) * _PatchTint.a;
    half3 tinted = saturate(_PatchTint.rgb * (AnimeLuma(colour) / max(AnimeLuma(_PatchTint.rgb), ANIME_GROUND_STEP_MIN_WIDTH)));
    return lerp(colour, tinted, weight);
}

void AnimeCustomSurface(float3 positionWS, float2 uv, inout AnimeSurface surface)
{
    float2 p = positionWS.xz;
    half4 patch = AnimeGroundNoise(p * _PatchScale);
    half tintNoise = 0.0h;
    UNITY_BRANCH
    if (_PatchTint.a > 0.0h)
        tintNoise = AnimeGroundNoise(p * _PatchTintScale).g;
    half near = 1.0h - half(saturate((distance(positionWS, _WorldSpaceCameraPos) - _DetailFadeStart) / max(_DetailFadeRange, 0.01)));

    float fromCentre = distance(p, _GroundCircle.xy) + (patch.g - 0.5) * 2.0 * _GroundCircle.w;
    float softness = max(_EdgeSoftness, 1e-3);
    half inside = _GroundCircle.z > 0.0
        ? half(1.0 - smoothstep(_GroundCircle.z - softness, _GroundCircle.z + softness, fromCentre))
        : 0.0h;

    float2x2 rotation = AnimeGroundBreakUpRotation();
    float2 positionDx = ddx(p);
    float2 positionDy = ddy(p);
    AnimeGroundCoordinates fieldUV = AnimeGroundCoordinatesOf(p, positionDx, positionDy, _FieldTile, rotation);
    AnimeGroundCoordinates circleUV = AnimeGroundCoordinatesOf(p, positionDx, positionDy, _CircleTile, rotation);

    bool fieldStones = _FieldStoneShade > 0.0h || _FieldStoneMix > 0.0h;
    bool circleStones = _CircleStoneShade > 0.0h || _CircleStoneMix > 0.0h;
    AnimeGroundSample field = AnimeGroundNeutral();
    AnimeGroundSample circle = AnimeGroundNeutral();
    UNITY_BRANCH
    if (inside < 1.0h)
        field = AnimeGroundRead(TEXTURE2D_ARGS(_FieldDetail, sampler_FieldDetail), TEXTURE2D_ARGS(_FieldStones, sampler_PointRepeat), fieldUV, _FieldBreakUp, patch.r, rotation, fieldStones);
    UNITY_BRANCH
    if (inside > 0.0h)
        circle = AnimeGroundRead(TEXTURE2D_ARGS(_CircleDetail, sampler_CircleDetail), TEXTURE2D_ARGS(_CircleStones, sampler_PointRepeat), circleUV, _CircleBreakUp, patch.r, rotation, circleStones);

    half shift = (patch.g - 0.5h) * _PatchStrength;
    half3 fieldColour = AnimeGroundPatchTint(AnimeGroundPaint(field, AnimeFieldLayer(), shift, near), tintNoise);
    half earth = smoothstep(1.0h - _EarthAmount, 1.0h - _EarthAmount + ANIME_GROUND_EARTH_EDGE, patch.g);
    fieldColour = lerp(fieldColour, _EarthColor.rgb, earth);
    half3 circleColour = AnimeGroundPaint(circle, AnimeCircleLayer(), shift, near);
    surface.albedo *= lerp(fieldColour, circleColour, inside);

    half2 normalXY = lerp(field.normalXY, circle.normalXY, inside);
    half strength = lerp(_FieldNormal * (1.0h - earth), _CircleNormal, inside) * near;
    surface.normalWS = AnimeGroundTilt(surface.normalWS, normalXY, strength);
}

#endif
