using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeShaders
{
    /**
     * <summary>Sun shafts as a volume override: light streaming from the sun past whatever stands in front of the sky,
     * drawn by <see cref="AnimeSunShaftsFeature" /> on the renderers that have it. Blended like any volume (a scene's global
     * one, local ones by area, a fight's on top). Intensity 0, the default, draws nothing.</summary>
     */
    [Serializable]
    [VolumeComponentMenu("Anime Shaders/Sun Shafts")]
    public sealed class AnimeSunShafts : VolumeComponent
    {
        #region Inspector Fields

        [Tooltip("How bright the shafts are (0: none, and nothing is drawn).")]
        public ClampedFloatParameter intensity = new(0f, 0f, 4f);

        [Tooltip("The shafts' colour, over the sun light's own.")]
        public ColorParameter tint = new(Color.white, false, false, true);

        [Tooltip("Only sky brighter than this gives off shafts: near the sky's bloom ceiling, the sun and its hottest glow, so clouds crossing it break the light into rays.")]
        public ClampedFloatParameter threshold = new(0.85f, 0f, 2f);

        [Tooltip("How far round the sun the sky gives off shafts, as a share of the screen.")]
        public ClampedFloatParameter reach = new(0.35f, 0.02f, 1f);

        [Tooltip("How long the shafts stream from the sun, as a share of the way to it.")]
        public ClampedFloatParameter length = new(0.7f, 0f, 1f);

        [Tooltip("How quickly a shaft fades along its length (lower: shorter, softer).")]
        public ClampedFloatParameter decay = new(0.96f, 0.8f, 1f);

        [Tooltip("Samples along each shaft per pass: more is smoother and costs more.")]
        public ClampedIntParameter samples = new(16, 4, 48);

        #endregion

        #region Public API

        /** <summary>Whether the shafts draw at all.</summary> */
        public bool IsActive()
        {
            return intensity.value > 0f;
        }

        #endregion
    }
}
