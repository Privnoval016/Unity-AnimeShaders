using System;
using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /** <summary>A face mesh as the baker reads it: at rest, in the model's own space (front +Z, up +Y).</summary> */
    public sealed class FaceMapSource
    {
        #region Properties

        /** <summary>Each vertex's position.</summary> */
        public Vector3[] Positions { get; }

        /** <summary>Each vertex's normal (the mesh's own, smoothed as modelled).</summary> */
        public Vector3[] Normals { get; }

        /** <summary>Each vertex's UV, where its texel is.</summary> */
        public Vector2[] Uvs { get; }

        /** <summary>The triangles, three indices each.</summary> */
        public int[] Triangles { get; }

        /** <summary>Each vertex's weight to the head (its bone and the bones below it): 1 the face, 0 the body.</summary> */
        public float[] FaceWeights { get; }

        #endregion

        #region Public API

        /** <summary>A source as given; every per-vertex array the same length.</summary> */
        public FaceMapSource(Vector3[] positions, Vector3[] normals, Vector2[] uvs, int[] triangles, float[] faceWeights)
        {
            int count = positions?.Length ?? 0;
            if (normals?.Length != count || uvs?.Length != count || faceWeights?.Length != count || triangles == null || triangles.Length % 3 != 0)
                throw new ArgumentException("A face map source needs a normal, a UV and a face weight per vertex, and whole triangles.");
            Positions = positions;
            Normals = normals;
            Uvs = uvs;
            Triangles = triangles;
            FaceWeights = faceWeights;
        }

        #endregion
    }
}
