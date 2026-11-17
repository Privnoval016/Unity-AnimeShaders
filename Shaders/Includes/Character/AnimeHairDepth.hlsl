#ifndef ANIME_HAIR_DEPTH_INCLUDED
#define ANIME_HAIR_DEPTH_INCLUDED

// The hair's depth (AnimeHairDepthFeature: the hair in view, half resolution, one over the eye depth, 0 where none) and
// reading it: the face's front-hair shadow and the eyes' mark through their fringe both read it. Without the feature
// _AnimeHairDepthOn is 0 (a global never set reads 0), and every read says no hair without loading anything.

TEXTURE2D(_AnimeHairDepth);
float _AnimeHairDepthOn;
float4 _AnimeHairDepthSize;

// No hair: farther than anything drawn.
#define ANIME_NO_HAIR 1e6

// The eye depth of the nearest hair at a texel of the hair's depth (clamped to the texture), or ANIME_NO_HAIR.
float AnimeHairDepthAt(int2 texel)
{
    UNITY_BRANCH
    if (_AnimeHairDepthOn < 0.5)
        return ANIME_NO_HAIR;
    texel = clamp(texel, int2(0, 0), int2(_AnimeHairDepthSize.xy) - 1);
    float inverseDepth = LOAD_TEXTURE2D_LOD(_AnimeHairDepth, texel, 0).r;
    return inverseDepth > 1e-4 ? rcp(inverseDepth) : ANIME_NO_HAIR;
}

// The texel of the hair's depth under a screen position (0 to 1).
int2 AnimeHairDepthTexel(float2 screenUV)
{
    return int2(floor(screenUV * _AnimeHairDepthSize.xy));
}

#endif
