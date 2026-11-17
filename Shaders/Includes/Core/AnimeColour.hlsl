#ifndef ANIME_COLOUR_INCLUDED
#define ANIME_COLOUR_INCLUDED

// The shaded colour's Brightness and Saturation (group ANIME_GROUP_COLOUR), before the haze: Saturation moves it away
// from its own grey (0 grey, 1 unchanged, above 1 richer), Brightness scales it. A shader opts in to lift what it draws
// above what doesn't (the characters over the world).

#if defined(ANIME_GROUP_COLOUR)
half3 AnimeColour(half3 color)
{
    half grey = dot(color, half3(0.2126h, 0.7152h, 0.0722h));
    return max(lerp(grey.xxx, color, _ColourSaturation), 0.0h) * _ColourBrightness;
}
#endif

#endif
