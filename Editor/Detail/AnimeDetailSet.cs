using UnityEngine;
using UnityEngine.Serialization;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>A detail set: a photo-scanned surface's maps and how to pack them (<see cref="AnimeDetailPacker" />) into
     * one detail map for a ground layer. The Inspector packs it and says when the packed map is out of date.</summary>
     */
    [CreateAssetMenu(menuName = "Anime Shaders/Detail Set", fileName = "DetailSet")]
    public class AnimeDetailSet : ScriptableObject
    {
        #region Inspector Fields

        [SerializeField, FormerlySerializedAs("_colour"), Tooltip("The colour map: its brightness becomes the shade the layer paints with its own colours.")]
        private Texture2D colour;

        [SerializeField, FormerlySerializedAs("_occlusion"), Tooltip("Optional: the ambient occlusion map (read from red), darkening the shade in grout and crevices.")]
        private Texture2D occlusion;

        [SerializeField, FormerlySerializedAs("_normal"), Tooltip("The normal map: tilts the lighting over the surface's bumps.")]
        private Texture2D normal;

        [SerializeField, FormerlySerializedAs("_mask"), Tooltip("Optional (read from red): white where the layer's first colour pair shows, black where its second does. None: the first pair everywhere.")]
        private Texture2D mask;

        [SerializeField, FormerlySerializedAs("_normalConvention"), Tooltip("Which way the normal map's green points. Poly Haven's \"nor_gl\" and ambientCG's \"NormalGL\" are OpenGL.")]
        private NormalConvention normalConvention;

        [SerializeField, Range(0f, 3f), Tooltip("Softens the shade toward a painted look: a blur radius as a percentage of the tile's width (the same look at any resolution), wrapping across the tile's edges. 0 keeps the photo's fine detail.")]
        private float soften = 0.3f;

        [SerializeField, FormerlySerializedAs("_occlusionWeight"), Range(0f, 1f), Tooltip("How strongly occlusion darkens the shade: higher keeps grout and crevices crisp while the soften blurs the rest.")]
        private float occlusionWeight = 0.8f;

        [SerializeField, FormerlySerializedAs("_size"), Min(0), Tooltip("The packed map's width and height in texels. 0: the colour map's size. 2048 holds a 2 m tile at about 1 mm a texel.")]
        private int size = RecommendedSize;

        [SerializeField, FormerlySerializedAs("_packed"), HideInInspector]
        private Texture2D packed;

        [SerializeField, FormerlySerializedAs("_packedHash"), HideInInspector]
        private string packedHash;

        #endregion

        #region Properties

        /** <summary>The size the Inspector suggests; larger maps cost video memory and packing time.</summary> */
        public const int RecommendedSize = 2048;

        /** <summary>The colour map.</summary> */
        public Texture2D Colour => colour;

        /** <summary>The ambient occlusion map, or null.</summary> */
        public Texture2D Occlusion => occlusion;

        /** <summary>The normal map.</summary> */
        public Texture2D Normal => normal;

        /** <summary>The optional mask.</summary> */
        public Texture2D Mask => mask;

        /** <summary>How the set is packed.</summary> */
        public DetailSettings Settings => new(soften, occlusionWeight, size, normalConvention);

        /** <summary>The packed map last written, or null.</summary> */
        public Texture2D Packed => packed;

        /** <summary>The hash of the sources and settings the packed map was made from.</summary> */
        public string PackedHash => packedHash;

        #endregion

        #region Public API

        /** <summary>Records the packed map just written and the hash it was made from.</summary> */
        public void Record(Texture2D packedMap, string hash)
        {
            packed = packedMap;
            packedHash = hash;
        }

        #endregion
    }
}
