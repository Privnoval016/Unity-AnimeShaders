using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /** <summary>A baked face map and what the bake found about its source.</summary> */
    public sealed class FaceMap
    {
        #region Properties

        /** <summary>The map, row by row from the bottom: R and G the horizontal normal's x and z (0.5 for 0), B the face weight, A 1 where the face (or its gutter) is.</summary> */
        public Color[] Pixels { get; }

        /** <summary>The map's width and height.</summary> */
        public int Size { get; }

        /** <summary>About how many texels across the face is (the square root of its texels).</summary> */
        public int TexelsAcross { get; }

        /** <summary>The share of the face's texels that two triangles both covered: overlapping or mirrored UVs.</summary> */
        public float Overlap { get; }

        /** <summary>The face's mean normal at rest, in the model's space: near +Z for a model that faces its front.</summary> */
        public Vector3 MeanNormal { get; }

        #endregion

        #region Public API

        /** <summary>A baked map.</summary> */
        public FaceMap(Color[] pixels, int size, int texelsAcross, float overlap, Vector3 meanNormal)
        {
            Pixels = pixels;
            Size = size;
            TexelsAcross = texelsAcross;
            Overlap = overlap;
            MeanNormal = meanNormal;
        }

        #endregion
    }
}
