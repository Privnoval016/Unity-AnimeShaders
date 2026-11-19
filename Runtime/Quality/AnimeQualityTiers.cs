using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeShaders
{
    /** <summary>The in-shader features a quality tier can drop: each skips its work in the shaders, adding no variant.</summary> */
    [Flags]
    public enum AnimeTierDrop
    {
        None = 0,
        FresnelRim = 1 << 0,
        SecondBand = 1 << 1,
        AngelRing = 1 << 2,
        ExtraLights = 1 << 3
    }

    /**
     * <summary>
     * Which in-shader features each quality tier drops, by the tier's render pipeline asset (each quality level has its
     * own), handed to <see cref="AnimeTiers.Use" />. A pipeline not listed drops nothing. Screen-space features (the
     * depth-edge rim, the front hair's shadow) drop by leaving their renderer feature off that tier's renderer instead.
     * </summary>
     */
    [CreateAssetMenu(menuName = "Anime Shaders/Quality Tiers", fileName = "AnimeQualityTiers")]
    public sealed class AnimeQualityTiers : ScriptableObject
    {
        /** <summary>One tier: its pipeline asset and what it drops.</summary> */
        [Serializable]
        public struct Tier
        {
            [Tooltip("The render pipeline asset of a quality level (Project Settings > Quality).")]
            public RenderPipelineAsset pipeline;
            [Tooltip("The in-shader features it drops.")]
            public AnimeTierDrop drops;
        }

        #region Inspector Fields

        [Tooltip("Each tier's pipeline asset and the features it drops; a pipeline not listed drops nothing.")]
        [SerializeField] private List<Tier> tiers = new();

        #endregion

        #region Properties

        /** <summary>Every tier listed.</summary> */
        public IReadOnlyList<Tier> Tiers => tiers;

        #endregion

        #region Public API

        /** <summary>What <paramref name="pipeline" /> drops; nothing when it isn't listed.</summary> */
        public AnimeTierDrop DropsFor(RenderPipelineAsset pipeline)
        {
            foreach (Tier tier in tiers)
            {
                if (tier.pipeline != null && tier.pipeline == pipeline)
                    return tier.drops;
            }
            return AnimeTierDrop.None;
        }

        /** <summary>Sets what <paramref name="pipeline" /> drops (for tiers built in code: tests).</summary> */
        internal void Set(RenderPipelineAsset pipeline, AnimeTierDrop drops)
        {
            tiers.RemoveAll(tier => tier.pipeline == pipeline);
            tiers.Add(new Tier { pipeline = pipeline, drops = drops });
        }

        #endregion
    }
}
