using UnityEngine;
using UnityEngine.Rendering;
using Ids = AnimeShaders.AnimeShaderIds.Sky;

namespace AnimeShaders
{
    /**
     * <summary>The one writer of a sky's state: the sky material, the sun light and the scene's render settings. The
     * ambient comes from the state's colours (Trilight) and reflections are off, so applying costs no sky-to-cubemap
     * render. Never call it during a fight: a light that turns mid-fight changes every shadow and rim.</summary>
     */
    public static class SkyApply
    {
        #region Public API

        /** <summary>Writes <paramref name="state" /> to <paramref name="sky" /> (an Anime/Sky material) and <paramref name="sun" />, and makes them the scene's skybox and sun.</summary> */
        public static void Apply(in SkyState state, Material sky, Light sun)
        {
            if (sky != null)
            {
                ToSky(state, sky);
                RenderSettings.skybox = sky;
            }
            if (sun != null)
            {
                sun.color = state.SunLight;
                sun.intensity = state.SunIntensity;
                sun.transform.rotation = state.SunRotation;
                RenderSettings.sun = sun;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = state.AmbientSky;
            RenderSettings.ambientEquatorColor = state.AmbientEquator;
            RenderSettings.ambientGroundColor = state.AmbientGround;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = null;
            RenderSettings.fog = state.Fog;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = state.Horizon;
            RenderSettings.fogStartDistance = state.FogStart;
            RenderSettings.fogEndDistance = state.FogEnd;
        }

        /** <summary>Writes the sky's own values to <paramref name="sky" /> only (no light, no render settings).</summary> */
        public static void ToSky(in SkyState state, Material sky)
        {
            sky.SetColor(Ids.ZenithColor, state.Zenith);
            sky.SetColor(Ids.HorizonColor, state.Horizon);
            sky.SetColor(Ids.BelowColor, state.Below);
            sky.SetFloat(Ids.HorizonHeight, state.HorizonHeight);
            sky.SetColor(Ids.SunColor, state.DiscColor);
            sky.SetFloat(Ids.SunSize, state.DiscSize);
            sky.SetFloat(Ids.SunStrength, state.DiscStrength);
            sky.SetFloat(Ids.SunHaloStrength, state.HaloStrength);
            sky.SetColor(Ids.CloudLitColor, state.CloudLit);
            sky.SetColor(Ids.CloudShadeColor, state.CloudShade);
            sky.SetFloat(Ids.CloudCover, state.CloudCover);
            sky.SetFloat(Ids.CloudSpeed, state.CloudSpeed);
            sky.SetVector(Ids.CloudWind, new Vector4(state.Wind.x, state.Wind.y, 0f, 0f));
            sky.SetFloat(Ids.StarDensity, state.StarDensity);
            sky.SetFloat(Ids.Stars, state.Stars ? 1f : 0f);
            sky.SetKeyword(new LocalKeyword(sky.shader, Ids.StarsKeyword), state.Stars);
        }

        #endregion
    }
}
