using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>Bakes smoothed outline normals into UV3 of every mesh in a model that carries the
     * <see cref="AnimeShaderIds.OutlineNormalLabel"/> asset label.</summary>
     * <remarks>Each vertex's normal is averaged, weighted by corner angle, with those of every vertex of the mesh at
     * the same quantised position (across submeshes, so a material seam doesn't crack the line), and stored in that
     * vertex's own tangent space, so the value follows skinning (UV3.xyz). A vertex on an open edge (an edge of one
     * triangle, positions welded as above so UV and material seams are not edges) also gets the outward direction in its
     * sheet's plane, summed over its open edges and normalised, as an angle in its tangent plane plus
     * <see cref="AnimeShaderIds.OutlineOpenEdgeOffset" /> (UV3.w; 0 elsewhere): the line widens past hems with Line On
     * Open Edges. Degenerate triangles are skipped; an edge on three or more triangles is not open; two coincident
     * layers read as closed. Lighting normals are never changed; a mesh with no tangents gets them built from its UVs. A mesh that already has UV3 is left alone with a warning. Changing a
     * model's labels does not reimport it: reimport the model after adding the label.</remarks>
     */
    public class OutlineNormalBaker : AssetPostprocessor
    {
        #region Private Fields

        private const VertexAttribute OutlineAttribute = VertexAttribute.TexCoord0 + AnimeShaderIds.OutlineNormalChannel;

        /** <summary>Below this sine of its smallest corner a triangle is too thin to have a plane: skipped.</summary> */
        private const float ThinnestTriangle = 1e-6f;

        #endregion

        #region Properties

        /**
         * <summary>The default position quantisation, as a share of the mesh's size (its bounds' diagonal): vertices
         * closer than that share a smoothed normal and an edge. Relative, so a model imported at a hundredth of its size
         * (a centimetre export) welds as finely as one in metres.</summary>
         */
        public const float DefaultQuantise = 1e-5f;


        /** <summary>The bake's version: raising it reimports every model (labelled or not) once.</summary> */
        public const uint BakeVersion = 4u;

        #endregion

        #region Public API

        /**
         * <summary>Bakes the outline normals of one mesh into <see cref="AnimeShaderIds.OutlineNormalChannel"/>.</summary>
         * <param name="mesh">A readable mesh.</param>
         * <param name="quantise">Positions are grouped at this share of the mesh's size.</param>
         * <returns>True when the channel was written; false when the mesh has no normals or already uses the channel
         * for something else (a model's own fourth UV set is never overwritten).</returns>
         */
        public static bool Bake(Mesh mesh, float quantise = DefaultQuantise)
        {
            Vector3[] normals = mesh.normals;
            Vector3[] vertices = mesh.vertices;
            if (normals.Length == 0 || normals.Length != vertices.Length)
                return false;
            if (mesh.HasVertexAttribute(OutlineAttribute))
                return false;

            Vector4[] tangents = mesh.tangents;
            if (tangents.Length != vertices.Length)
            {
                if (mesh.HasVertexAttribute(VertexAttribute.TexCoord0))
                    mesh.RecalculateTangents();
                else
                    mesh.tangents = BuildTangents(normals);
                tangents = mesh.tangents;
            }

            float grid = Mathf.Max(quantise * mesh.bounds.size.magnitude, 1e-9f);
            Vector3[] smoothed = SmoothByPosition(mesh, vertices, normals, grid);
            Vector3[] outward = OpenEdgeOutward(mesh, vertices, grid);
            var packed = new List<Vector4>(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 normal = ToTangentSpace(smoothed[i], normals[i], tangents[i]);
                packed.Add(new Vector4(normal.x, normal.y, normal.z, OpenEdgeAngle(outward[i], normals[i], tangents[i])));
            }
            mesh.SetUVs(AnimeShaderIds.OutlineNormalChannel, packed);
            return true;
        }

        /** <summary>Bumps the postprocessor version so models reimport when the bake changes.</summary> */
        public override uint GetVersion()
        {
            return BakeVersion;
        }

        #endregion

        #region Event Handlers

        private void OnPostprocessModel(GameObject root)
        {
            if (!HasOutlineLabel(assetPath))
                return;

            var done = new HashSet<Mesh>();
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
                BakeOnce(filter.sharedMesh, done);
            foreach (SkinnedMeshRenderer skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                BakeOnce(skinned.sharedMesh, done);
        }

        #endregion

        #region Helpers

        private static bool HasOutlineLabel(string path)
        {
            string[] labels = AssetDatabase.GetLabels(AssetDatabase.GUIDFromAssetPath(path));
            return Array.IndexOf(labels, AnimeShaderIds.OutlineNormalLabel) >= 0;
        }

        private void BakeOnce(Mesh mesh, HashSet<Mesh> done)
        {
            if (mesh == null || !done.Add(mesh))
                return;
            if (mesh.HasVertexAttribute(OutlineAttribute))
                Debug.LogWarning($"{assetPath}: {mesh.name} already has UV3; outline normals not baked.");
            else
                Bake(mesh);
        }

        private static Vector4[] BuildTangents(Vector3[] normals)
        {
            var tangents = new Vector4[normals.Length];
            for (int i = 0; i < normals.Length; i++)
            {
                Vector3 normal = normals[i].normalized;
                Vector3 reference = Mathf.Abs(normal.y) < 0.99f ? Vector3.up : Vector3.right;
                Vector3 tangent = Vector3.Cross(reference, normal).normalized;
                tangents[i] = new Vector4(tangent.x, tangent.y, tangent.z, 1f);
            }
            return tangents;
        }

        private static Vector3[] SmoothByPosition(Mesh mesh, Vector3[] vertices, Vector3[] normals, float quantise)
        {
            var weighted = new Vector3[vertices.Length];
            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                if (mesh.GetTopology(submesh) == MeshTopology.Triangles)
                    AddCornerWeights(mesh.GetTriangles(submesh), vertices, normals, weighted);
            }

            var groups = new Dictionary<Vector3Int, Vector3>(vertices.Length);
            for (int v = 0; v < vertices.Length; v++)
            {
                Vector3Int key = Quantise(vertices[v], quantise);
                groups.TryGetValue(key, out Vector3 sum);
                groups[key] = sum + weighted[v];
            }

            var result = new Vector3[vertices.Length];
            for (int v = 0; v < vertices.Length; v++)
            {
                Vector3 average = groups[Quantise(vertices[v], quantise)];
                result[v] = average.sqrMagnitude > 1e-12f ? average.normalized : normals[v].normalized;
            }
            return result;
        }

        private static void AddCornerWeights(int[] triangles, Vector3[] vertices, Vector3[] normals, Vector3[] weighted)
        {
            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                for (int corner = 0; corner < 3; corner++)
                {
                    int v = triangles[t + corner];
                    float angle = CornerAngle(vertices[v], vertices[triangles[t + (corner + 1) % 3]],
                        vertices[triangles[t + (corner + 2) % 3]]);
                    weighted[v] += normals[v].normalized * angle;
                }
            }
        }

        private static float CornerAngle(Vector3 corner, Vector3 next, Vector3 previous)
        {
            Vector3 a = next - corner;
            Vector3 b = previous - corner;
            float lengths = a.magnitude * b.magnitude;
            if (lengths < 1e-12f)
                return 0f;
            return Mathf.Acos(Mathf.Clamp(Vector3.Dot(a, b) / lengths, -1f, 1f));
        }

        private static Vector3Int Quantise(Vector3 position, float quantise)
        {
            return new Vector3Int(
                (int)Math.Round(position.x / quantise),
                (int)Math.Round(position.y / quantise),
                (int)Math.Round(position.z / quantise));
        }

        /**
         * <summary>Each vertex's outward direction along the open edges it lies on (zero inside the mesh): per open edge,
         * perpendicular to it in its triangle's plane, away from the triangle's third corner; summed over the welded
         * position's open edges and normalised.</summary>
         */
        private static Vector3[] OpenEdgeOutward(Mesh mesh, Vector3[] vertices, float quantise)
        {
            var welded = new int[vertices.Length];
            var ids = new Dictionary<Vector3Int, int>(vertices.Length);
            for (int v = 0; v < vertices.Length; v++)
            {
                Vector3Int key = Quantise(vertices[v], quantise);
                if (!ids.TryGetValue(key, out int id))
                {
                    id = ids.Count;
                    ids[key] = id;
                }
                welded[v] = id;
            }

            var edges = new Dictionary<(int, int), (int Count, Vector3 Outward)>();
            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                if (mesh.GetTopology(submesh) != MeshTopology.Triangles)
                    continue;
                int[] triangles = mesh.GetTriangles(submesh);
                for (int t = 0; t + 2 < triangles.Length; t += 3)
                {
                    int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
                    if (welded[a] == welded[b] || welded[b] == welded[c] || welded[a] == welded[c])
                        continue;
                    Vector3 ab = vertices[b] - vertices[a], ac = vertices[c] - vertices[a];
                    Vector3 face = Vector3.Cross(ab, ac);
                    if (face.sqrMagnitude <= ThinnestTriangle * ThinnestTriangle * ab.sqrMagnitude * ac.sqrMagnitude)
                        continue;
                    AddEdge(edges, welded, vertices, face, a, b, c);
                    AddEdge(edges, welded, vertices, face, b, c, a);
                    AddEdge(edges, welded, vertices, face, c, a, b);
                }
            }

            var sums = new Vector3[ids.Count];
            foreach (KeyValuePair<(int, int), (int Count, Vector3 Outward)> edge in edges)
            {
                if (edge.Value.Count != 1)
                    continue;
                sums[edge.Key.Item1] += edge.Value.Outward;
                sums[edge.Key.Item2] += edge.Value.Outward;
            }

            var outward = new Vector3[vertices.Length];
            for (int v = 0; v < vertices.Length; v++)
            {
                Vector3 sum = sums[welded[v]];
                outward[v] = sum.sqrMagnitude > 1e-8f ? sum.normalized : Vector3.zero;
            }
            return outward;
        }

        private static void AddEdge(Dictionary<(int, int), (int Count, Vector3 Outward)> edges, int[] welded, Vector3[] vertices,
            Vector3 face, int from, int to, int third)
        {
            Vector3 along = vertices[to] - vertices[from];
            Vector3 across = Vector3.Cross(face / face.magnitude, along / along.magnitude);
            if (Vector3.Dot(across, vertices[third] - vertices[from]) > 0f)
                across = -across;
            (int, int) key = welded[from] < welded[to] ? (welded[from], welded[to]) : (welded[to], welded[from]);
            edges.TryGetValue(key, out (int Count, Vector3 Outward) seen);
            edges[key] = (seen.Count + 1, seen.Outward + across);
        }

        /** <summary>The outward direction as an angle in the vertex's tangent plane plus the offset, or 0 for none.</summary> */
        private static float OpenEdgeAngle(Vector3 outward, Vector3 normal, Vector4 tangent)
        {
            Vector3 n = normal.normalized;
            Vector3 inPlane = outward - n * Vector3.Dot(outward, n);
            Vector3 t = (Vector3)tangent - n * Vector3.Dot(n, tangent);
            if (inPlane.sqrMagnitude < 1e-8f || t.sqrMagnitude < 1e-8f)
                return 0f;
            t.Normalize();
            Vector3 b = Vector3.Cross(n, t) * (tangent.w < 0f ? -1f : 1f);
            return Mathf.Atan2(Vector3.Dot(inPlane, b), Vector3.Dot(inPlane, t)) + AnimeShaderIds.OutlineOpenEdgeOffset;
        }

        private static Vector3 ToTangentSpace(Vector3 smoothed, Vector3 normal, Vector4 tangent)
        {
            Vector3 n = normal.normalized;
            Vector3 t = (Vector3)tangent - n * Vector3.Dot(n, tangent);
            if (t.sqrMagnitude < 1e-8f)
                return new Vector3(0f, 0f, Vector3.Dot(smoothed, n));
            t.Normalize();
            Vector3 b = Vector3.Cross(n, t) * (tangent.w < 0f ? -1f : 1f);
            return new Vector3(Vector3.Dot(smoothed, t), Vector3.Dot(smoothed, b), Vector3.Dot(smoothed, n));
        }

        #endregion
    }
}
