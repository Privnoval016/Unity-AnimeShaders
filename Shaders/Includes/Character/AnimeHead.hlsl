#ifndef ANIME_HEAD_INCLUDED
#define ANIME_HEAD_INCLUDED

// The head's frame, written per renderer by AnimeHeadFrame (a property block, so these renderers leave the SRP
// Batcher): its centre, and the model's front, right and up as the head carries them, in world space. Never set (no
// head frame on the character), they are zero: AnimeHeadFrameSet is false and a face or hair shader shades as plain.

float4 _AnimeHeadCentre;
float4 _AnimeHeadForward;
float4 _AnimeHeadRight;
float4 _AnimeHeadUp;

bool AnimeHeadFrameSet()
{
    return dot(_AnimeHeadForward.xyz, _AnimeHeadForward.xyz) > 0.25;
}

#endif
