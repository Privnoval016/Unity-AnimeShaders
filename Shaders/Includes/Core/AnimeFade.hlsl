#ifndef ANIME_FADE_INCLUDED
#define ANIME_FADE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// Dither fade read from the renderer's user value (Renderer.SetShaderUserValue, unity_RendererUserValue, a uint).
//
// Encoding: the low 8 bits are the FADE LEVEL, 0 = solid (what an unset renderer holds, so nothing fades by
// default), 255 = fully faded. Higher bits are reserved and ignored. Coverage is 1 - level / 255; a pixel is kept when
// coverage exceeds its threshold in a fixed screen-space 4x4 Bayer matrix, so a rising level only ever removes pixels
// and every step's kept set contains the next one's (no flicker, no noise that moves).

#define ANIME_FADE_LEVEL_MASK 0xFFu
#define ANIME_FADE_LEVEL_FULL 255u

uint AnimeFadeLevel()
{
    return unity_RendererUserValue & ANIME_FADE_LEVEL_MASK;
}

bool AnimeFadeIsFull()
{
    return AnimeFadeLevel() >= ANIME_FADE_LEVEL_FULL;
}

float AnimeBayer4Threshold(uint2 pixel)
{
    uint x = pixel.x & 3u;
    uint y = pixel.y & 3u;
    uint d = x ^ y;
    uint index = ((d & 1u) << 3) | ((y & 1u) << 2) | (((d >> 1) & 1u) << 1) | ((y >> 1) & 1u);
    return (float(index) + 0.5) * (1.0 / 16.0);
}

// A clip-space position that draws nothing when the renderer is fully faded, so a hidden body costs no pixels in any
// pass (the vertex work still runs).
float4 AnimeFadeCull(float4 positionCS)
{
    return AnimeFadeIsFull() ? float4(2.0, 2.0, 2.0, 1.0) : positionCS;
}

void AnimeFadeClip(float4 positionCS)
{
    uint level = AnimeFadeLevel();
    [branch] if (level != 0u)
    {
        float coverage = 1.0 - float(level) * (1.0 / 255.0);
        clip(coverage - AnimeBayer4Threshold(uint2(positionCS.xy)));
    }
}

#endif
