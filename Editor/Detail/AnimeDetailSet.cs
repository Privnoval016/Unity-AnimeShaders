using UnityEngine;
using UnityEngine.Serialization;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>A detail set: a photo-scanned surface's maps and how to pack them (<see cref="AnimeDetailPacker" />) into
     * one detail map for a ground layer, and optionally a stone map (<see cref="StoneMath" />) so the layer can vary each
     * stone. The Inspector packs it and says when the packed map is out of date.</summary>
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

        [Header("Stones")]
        [SerializeField, Tooltip("Makes a stone map beside the packed map, so a ground layer can vary each stone on the floor (Stone Shade, Stone Mix on the material) and a tiled set stops repeating. Needs an occlusion map that is darker in the joints (paving, tiles, bricks).")]
        private bool stones;

        [SerializeField, Range(0f, 2f), Tooltip("Blurs the occlusion before the stones are found, as a percentage of the tile's width, so pitting inside a stone doesn't split it. Raise it when the count is far above the stones you see.")]
        private float stoneBlur = 0.3f;

        [SerializeField, Range(0f, 1f), Tooltip("The blurred occlusion above which a texel is stone, below which it is joint. Raise it when neighbouring stones merge; lower it when stones break up.")]
        private float stoneThreshold = 0.84f;

        [SerializeField, Range(0f, 5f), Tooltip("The smallest stone kept, as a percentage of the tile's area; smaller pieces join the stone beside them.")]
        private float stoneMinSize = 0.4f;

        [SerializeField, FormerlySerializedAs("_packed"), HideInInspector]
        private Texture2D packed;

        [SerializeField, FormerlySerializedAs("_packedHash"), HideInInspector]
        private string packedHash;

        [SerializeField, HideInInspector]
        private Texture2D stoneMap;

        [SerializeField, HideInInspector]
        private int stoneCount;

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

        /** <summary>Whether packing also makes a stone map.</summary> */
        public bool MakesStones => stones;

        /** <summary>How the stones are found.</summary> */
        public StoneSettings StoneSettings => new(stoneBlur, stoneThreshold, stoneMinSize);

        /** <summary>The stone map last written, or null.</summary> */
        public Texture2D StoneMap => stoneMap;

        /** <summary>How many stones the last stone map held.</summary> */
        public int StoneCount => stoneCount;

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

        /** <summary>Records the stone map just written (or null for none) and its stone count.</summary> */
        public void RecordStones(Texture2D map, int count)
        {
            stoneMap = map;
            stoneCount = count;
        }

        #endregion
    }
}
