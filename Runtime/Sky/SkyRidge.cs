using System;
using UnityEngine;

namespace AnimeShaders
{
    /** <summary>One layer of hills or mountains the sky draws on its horizon: a silhouette, not a model.</summary> */
    [Serializable]
    public struct SkyRidge
    {
        #region Inspector Fields

        [Tooltip("The layer's colour before the haze: the haze pales it by its distance.")]
        [SerializeField] private Color colour;

        [Tooltip("How high the layer rises above the horizon on average, in degrees.")]
        [Range(0f, 30f)] [SerializeField] private float height;

        [Tooltip("How far its peaks and dips stray from that height, in degrees.")]
        [Range(0f, 20f)] [SerializeField] private float roughness;

        [Tooltip("How many hills run around the whole horizon (more is busier).")]
        [Range(1f, 64f)] [SerializeField] private float hills;

        [Tooltip("Which ridge line, of endless ones: change it for a different skyline.")]
        [SerializeField] private float seed;

        [Tooltip("How far away the layer stands, in metres: the haze pales it as it would the ground there.")]
        [Min(0f)] [SerializeField] private float distance;

        #endregion

        #region Properties

        /** <summary>The layer's colour before the haze.</summary> */
        public Color Colour => colour;

        /** <summary>Its average height above the horizon, in degrees.</summary> */
        public float Height => height;

        /** <summary>How far its line strays from that height, in degrees.</summary> */
        public float Roughness => roughness;

        /** <summary>How many hills run around the horizon.</summary> */
        public float Hills => hills;

        /** <summary>Which ridge line.</summary> */
        public float Seed => seed;

        /** <summary>How far away it stands, in metres.</summary> */
        public float Distance => distance;

        #endregion

        #region Public API

        /** <summary>A layer as given.</summary> */
        public SkyRidge(Color colour, float height, float roughness, float hills, float seed, float distance)
        {
            this.colour = colour;
            this.height = height;
            this.roughness = roughness;
            this.hills = hills;
            this.seed = seed;
            this.distance = distance;
        }

        #endregion
    }
}
