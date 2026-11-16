using System.Collections.Generic;
using UnityEngine;

namespace AnimeShaders.Tests
{
    /** <summary>Meshes made for tests; the caller destroys every mesh made here (the built-in sphere excepted).</summary> */
    public static class TestMeshes
    {
        #region Public API

        /** <summary>Unity's built-in sphere (radius 0.5), shared: never destroyed.</summary> */
        public static Mesh Sphere()
        {
            return Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
        }

        /** <summary>A unit quad in the XY plane facing -Z (toward the stage's camera), uvs 0 to 1, tangent +X.</summary> */
        public static Mesh Quad()
        {
            var mesh = new Mesh { name = "TestQuad" };
            mesh.SetVertices(new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
            });
            mesh.SetNormals(new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back });
            var tangent = new Vector4(1f, 0f, 0f, 1f);
            mesh.SetTangents(new[] { tangent, tangent, tangent, tangent });
            mesh.SetUVs(0, new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) });
            mesh.SetTriangles(new[] { 0, 1, 2, 2, 3, 0 }, 0);
            return mesh;
        }

        /**
         * <summary>A unit cube centred on the origin with hard edges (four vertices per face), front faces outward,
         * its faces split across <paramref name="submeshes" /> submeshes (1 to 6); tangents carry
         * <paramref name="tangentSign" /> as their w (-1 as on mirrored UVs), or none when 0.</summary>
         */
        public static Mesh HardCube(int submeshes = 1, float tangentSign = 1f)
        {
            Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            var faces = new List<Vector3[]>();
            foreach (Vector3 normal in normals)
            {
                Vector3 tangent = Mathf.Abs(normal.y) > 0.5f ? Vector3.right : Vector3.Cross(Vector3.up, normal);
                Vector3 bitangent = Vector3.Cross(normal, tangent);
                Vector3 centre = normal * 0.5f;
                faces.Add(new[]
                {
                    centre - (tangent + bitangent) * 0.5f, centre + (tangent - bitangent) * 0.5f,
                    centre + (tangent + bitangent) * 0.5f, centre - (tangent - bitangent) * 0.5f,
                });
            }
            Mesh mesh = Hard("TestHardCube", faces, Mathf.Clamp(submeshes, 1, 6), tangentSign);
            return mesh;
        }

        /**
         * <summary>A hard-edged triangular prism whose end caps are not right triangles, so faces meet its corners at
         * unequal angles.</summary>
         */
        public static Mesh Wedge()
        {
            Vector3 a = new(0f, 0f, 0f), b = new(1.2f, 0f, 0f), c = new(0.3f, 0.9f, 0f);
            Vector3 depth = new(0f, 0f, 1f);
            var faces = new List<Vector3[]>
            {
                new[] { a, c, b },
                new[] { a + depth, b + depth, c + depth },
                new[] { a, b, b + depth, a + depth },
                new[] { b, c, c + depth, b + depth },
                new[] { c, a, a + depth, c + depth },
            };
            return Hard("TestWedge", faces, 1, 1f);
        }

        #endregion

        #region Helpers

        /** <summary>A mesh of flat polygons (fans), each with its own vertices and its own normal from its winding.</summary> */
        private static Mesh Hard(string name, List<Vector3[]> faces, int submeshes, float tangentSign)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var perSubmesh = new List<int>[submeshes];
            for (int i = 0; i < submeshes; i++)
                perSubmesh[i] = new List<int>();

            for (int face = 0; face < faces.Count; face++)
            {
                Vector3[] corners = faces[face];
                Vector3 normal = Vector3.Cross(corners[1] - corners[0], corners[2] - corners[0]).normalized;
                Vector3 tangent = (corners[1] - corners[0]).normalized;
                int first = vertices.Count;
                foreach (Vector3 corner in corners)
                {
                    vertices.Add(corner);
                    normals.Add(normal);
                    tangents.Add(new Vector4(tangent.x, tangent.y, tangent.z, tangentSign));
                }
                for (int i = 1; i + 1 < corners.Length; i++)
                    perSubmesh[face % submeshes].AddRange(new[] { first, first + i, first + i + 1 });
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            if (tangentSign != 0f)
                mesh.SetTangents(tangents);
            mesh.subMeshCount = submeshes;
            for (int i = 0; i < submeshes; i++)
                mesh.SetTriangles(perSubmesh[i], i);
            mesh.RecalculateBounds();
            return mesh;
        }

        #endregion
    }
}
