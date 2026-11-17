#ifndef ANIME_HASH_INCLUDED
#define ANIME_HASH_INCLUDED

// A value per cell from arithmetic alone (no sine, which loses precision on large arguments on mobile GPUs), 0 to 1;
// a different seed gives an unrelated value for the same cell. The sky's stars and the hair's strands use it.
float AnimeHash(float cell, float seed)
{
    float p = frac((cell + seed * 17.0) * 0.1031);
    p *= p + 33.33;
    p *= p + p;
    return frac(p);
}

#endif
