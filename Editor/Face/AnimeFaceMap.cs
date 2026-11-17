using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>A face map: which way each texel of a face mesh faces round the head, and how much of it is face, baked
     * from the mesh (<see cref="AnimeFaceMapBaker" />) for <c>Anime/Face</c>, so the face's shadow follows the head's
     * turn in a clean line. The Inspector bakes it and says when the map is out of date or the mesh has a problem.</summary>
     */
    [CreateAssetMenu(menuName = "Anime Shaders/Face Map", fileName = "FaceMap")]
    public sealed class AnimeFaceMap : ScriptableObject
    {
        #region Inspector Fields

        [SerializeField, Tooltip("The face's mesh, inside a model with a humanoid avatar and Read/Write on. A mesh shared with the body is fine: only what the head moves counts as face.")]
        private Mesh mesh;

        [SerializeField, Min(MinSize), Tooltip("The map's width and height in texels. 1024 keeps the shadow's edge smooth on a face that fills half the texture.")]
        private int size = RecommendedSize;

        [SerializeField, Range(0f, 3f), Tooltip("Blurs which way the face faces, as a percentage of the map's width, within each UV island. A little smooths the shadow's edge past the mesh's triangles; more also melts the nose's own shading into the face's.")]
        private float smoothing = 0.3f;

        [SerializeField, Range(0f, 1f), Tooltip("Leans each normal toward the head's sphere: for faces whose own normals aren't smoothed yet. 0 keeps the mesh's normals.")]
        private float sphereBlend;

        [SerializeField, Range(0, 32), Tooltip("Texels each island is padded by, so filtering never reads the empty space round it.")]
        private int gutter = 8;

        [SerializeField, HideInInspector]
        private Texture2D map;

        [SerializeField, HideInInspector]
        private string bakedHash;

        [SerializeField, HideInInspector]
        private int texelsAcross;

        [SerializeField, HideInInspector]
        private float overlap;

        [SerializeField, HideInInspector]
        private Vector3 meanNormal;

        #endregion

        #region Properties

        /** <summary>The size the Inspector suggests.</summary> */
        public const int RecommendedSize = 1024;

        /** <summary>The smallest size a map may have.</summary> */
        public const int MinSize = 64;

        /** <summary>The face's mesh.</summary> */
        public Mesh Mesh => mesh;

        /** <summary>How the map is baked.</summary> */
        public FaceMapSettings Settings => new(size, smoothing, sphereBlend, gutter);

        /** <summary>The map last baked, or null.</summary> */
        public Texture2D Map => map;

        /** <summary>The hash of the model and settings the map was baked from.</summary> */
        public string BakedHash => bakedHash;

        /** <summary>About how many texels across the face was at the last bake.</summary> */
        public int TexelsAcross => texelsAcross;

        /** <summary>The share of the face's texels two triangles covered at the last bake.</summary> */
        public float Overlap => overlap;

        /** <summary>The face's mean normal at the last bake, in the model's space.</summary> */
        public Vector3 MeanNormal => meanNormal;

        #endregion

        #region Public API

        /** <summary>Records the map just written, the hash it was baked from and what the bake found.</summary> */
        public void Record(Texture2D bakedMap, string hash, FaceMap found)
        {
            map = bakedMap;
            bakedHash = hash;
            texelsAcross = found.TexelsAcross;
            overlap = found.Overlap;
            meanNormal = found.MeanNormal;
        }

        #endregion
    }
}
