using System.Collections.Generic;
using System.Linq;
using AnimeShaders.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace AnimeShaders.Tests
{
    /**
     * <summary>The outline normal bake on a hard-edged cube: every corner's line normal points out of the corner; a flat
     * sheet's open edges push outward at a full width (corners too), its middle not; a UV seam is not an open edge; a
     * closed mesh has none; a degenerate triangle is skipped; a mesh a ten-thousandth of the size bakes the same (Unity's
     * normalize gives zero below 1e-5).</summary>
     */
    public class OutlineNormalBakerTests
    {
        #region Private Fields

        private readonly List<Mesh> _made = new();

        #endregion

        #region Helpers

        private Mesh Made(Mesh mesh)
        {
            _made.Add(mesh);
            return mesh;
        }

        private Mesh Cube(int submeshes = 1, float tangentSign = 1f)
        {
            return Made(TestMeshes.HardCube(submeshes, tangentSign));
        }

        /** <summary>For each vertex: the faces meeting its position, each normal weighted by its corner angle there, summed and normalised (read from the triangles, not the baker).</summary> */
        private static Vector3[] AngleWeighted(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            var sums = new Dictionary<Vector3, Vector3>();
            int[] triangles = mesh.triangles;
            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 a = vertices[triangles[t]], b = vertices[triangles[t + 1]], c = vertices[triangles[t + 2]];
                Vector3 face = Vector3.Cross(b - a, c - a).normalized;
                Add(sums, a, face * Vector3.Angle(b - a, c - a));
                Add(sums, b, face * Vector3.Angle(c - b, a - b));
                Add(sums, c, face * Vector3.Angle(a - c, b - c));
            }
            var result = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                result[i] = sums[vertices[i]].normalized;
            return result;
        }

        private static void Add(Dictionary<Vector3, Vector3> sums, Vector3 at, Vector3 value)
        {
            sums.TryGetValue(at, out Vector3 sum);
            sums[at] = sum + value;
        }

        /** <summary>The baked line normals rebuilt in object space the way the line pass rebuilds them.</summary> */
        private static Vector3[] Rebuilt(Mesh mesh)
        {
            var packed = new List<Vector3>();
            mesh.GetUVs(AnimeShaderIds.OutlineNormalChannel, packed);
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;
            var result = new Vector3[packed.Count];
            for (int i = 0; i < packed.Count; i++)
            {
                Vector3 n = normals[i].normalized;
                Vector3 t = ((Vector3)tangents[i] - n * Vector3.Dot(n, tangents[i])).normalized;
                Vector3 b = Vector3.Cross(n, t) * (tangents[i].w < 0f ? -1f : 1f);
                result[i] = (t * packed[i].x + b * packed[i].y + n * packed[i].z).normalized;
            }
            return result;
        }

        private static void AssertCornersPointOut(Mesh mesh)
        {
            Vector3[] rebuilt = Rebuilt(mesh);
            Vector3[] vertices = mesh.vertices;
            Assert.AreEqual(vertices.Length, rebuilt.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 expected = vertices[i].normalized;
                Assert.Less(Vector3.Angle(expected, rebuilt[i]), 0.1f, $"vertex {i}");
            }
        }

        #endregion

        [TearDown]
        public void TearDown()
        {
            foreach (Mesh mesh in _made)
                Object.DestroyImmediate(mesh);
            _made.Clear();
        }

        [Test]
        public void HardCube_CornersPointOutOfTheCorner()
        {
            Mesh cube = Cube();
            Assert.IsTrue(OutlineNormalBaker.Bake(cube));
            AssertCornersPointOut(cube);
        }

        [Test]
        public void FacesSplitAcrossSubmeshes_StillShareTheirCorners()
        {
            Mesh cube = Cube(submeshes: 3);
            OutlineNormalBaker.Bake(cube);
            AssertCornersPointOut(cube);
        }

        [Test]
        public void MeshWithoutTangents_GetsTangents_AndStillBakes()
        {
            Mesh cube = Cube(tangentSign: 0f);
            OutlineNormalBaker.Bake(cube);
            Assert.AreEqual(cube.vertexCount, cube.tangents.Length);
            AssertCornersPointOut(cube);
        }

        [Test]
        public void LightingNormals_AreUntouched()
        {
            Mesh cube = Cube();
            Vector3[] before = cube.normals;
            OutlineNormalBaker.Bake(cube);
            CollectionAssert.AreEqual(before, cube.normals);
        }

        [Test]
        public void TwoBakes_WriteTheSameValues()
        {
            Mesh first = Cube();
            Mesh second = Cube();
            OutlineNormalBaker.Bake(first);
            OutlineNormalBaker.Bake(second);
            var a = new List<Vector3>();
            var b = new List<Vector3>();
            first.GetUVs(AnimeShaderIds.OutlineNormalChannel, a);
            second.GetUVs(AnimeShaderIds.OutlineNormalChannel, b);
            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void VerticesCloserThanTheGrid_ShareANormal_FartherOnesDoNot()
        {
            Mesh cube = Cube();
            Vector3[] vertices = cube.vertices;
            vertices[0] += Vector3.one * (OutlineNormalBaker.DefaultQuantise * 0.01f);
            cube.vertices = vertices;
            OutlineNormalBaker.Bake(cube);
            AssertCornersPointOut(cube);

            Mesh apart = Cube();
            vertices = apart.vertices;
            vertices[0] += Vector3.right * 0.01f;
            apart.vertices = vertices;
            OutlineNormalBaker.Bake(apart);
            Assert.Greater(Vector3.Angle(vertices[0].normalized, Rebuilt(apart)[0]), 1f);
        }

        [Test]
        public void MirroredTangents_StillPointOutOfTheCorner()
        {
            Mesh cube = Cube(tangentSign: -1f);
            OutlineNormalBaker.Bake(cube);
            AssertCornersPointOut(cube);
        }

        [Test]
        public void FacesMeetingAtUnequalAngles_WeighByTheirAngle()
        {
            Mesh wedge = Made(TestMeshes.Wedge());
            OutlineNormalBaker.Bake(wedge);
            Vector3[] expected = AngleWeighted(wedge);
            Vector3[] rebuilt = Rebuilt(wedge);
            for (int i = 0; i < expected.Length; i++)
                Assert.Less(Vector3.Angle(expected[i], rebuilt[i]), 0.1f, $"vertex {i}");
        }

        [Test]
        public void AMeshWithItsOwnUv3_IsLeftAlone()
        {
            Mesh cube = Cube();
            var own = new List<Vector3>();
            for (int i = 0; i < cube.vertexCount; i++)
                own.Add(new Vector3(i, 0f, 0f));
            cube.SetUVs(AnimeShaderIds.OutlineNormalChannel, own);
            Assert.IsFalse(OutlineNormalBaker.Bake(cube));
            var after = new List<Vector3>();
            cube.GetUVs(AnimeShaderIds.OutlineNormalChannel, after);
            CollectionAssert.AreEqual(own, after);
        }

        [Test]
        public void MeshWithoutNormals_IsRefused()
        {
            var mesh = new Mesh { vertices = new[] { Vector3.zero, Vector3.up, Vector3.right } };
            _made.Add(mesh);
            Assert.IsFalse(OutlineNormalBaker.Bake(mesh));
            var packed = new List<Vector3>();
            mesh.GetUVs(AnimeShaderIds.OutlineNormalChannel, packed);
            Assert.IsEmpty(packed);
        }

        /** <summary>Each vertex's baked open-edge direction rebuilt in object space as the line pass rebuilds it, or zero.</summary> */
        private static Vector3[] OpenEdges(Mesh mesh)
        {
            var packed = new List<Vector4>();
            mesh.GetUVs(AnimeShaderIds.OutlineNormalChannel, packed);
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;
            var result = new Vector3[packed.Count];
            for (int i = 0; i < packed.Count; i++)
            {
                if (packed[i].w <= AnimeShaderIds.OutlineOpenEdgeAbove)
                    continue;
                Vector3 n = normals[i].normalized;
                Vector3 t = ((Vector3)tangents[i] - n * Vector3.Dot(n, tangents[i])).normalized;
                Vector3 b = Vector3.Cross(n, t) * (tangents[i].w < 0f ? -1f : 1f);
                float angle = packed[i].w - AnimeShaderIds.OutlineOpenEdgeOffset;
                result[i] = t * Mathf.Cos(angle) + b * Mathf.Sin(angle);
            }
            return result;
        }

        /** <summary>A flat grid of quads in the XY plane, <paramref name="cells" /> by <paramref name="cells" />, facing -Z.</summary> */
        private Mesh Grid(int cells, bool splitAtTheMiddleColumn = false)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int cy = 0; cy < cells; cy++)
            {
                for (int cx = 0; cx < cells; cx++)
                {
                    int start = vertices.Count;
                    float x0 = cx - cells * 0.5f, y0 = cy - cells * 0.5f;
                    float u = splitAtTheMiddleColumn && cx >= cells / 2 ? 0.5f : 0f;
                    vertices.AddRange(new[] { new Vector3(x0, y0, 0f), new Vector3(x0, y0 + 1f, 0f), new Vector3(x0 + 1f, y0 + 1f, 0f), new Vector3(x0 + 1f, y0, 0f) });
                    uvs.AddRange(new[] { new Vector2(u, 0f), new Vector2(u, 1f), new Vector2(u + 0.5f, 1f), new Vector2(u + 0.5f, 0f) });
                    triangles.AddRange(new[] { start, start + 1, start + 2, start + 2, start + 3, start });
                }
            }
            var mesh = new Mesh { name = "TestGrid" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(Enumerable.Repeat(Vector3.back, vertices.Count).ToList());
            mesh.SetTangents(Enumerable.Repeat(new Vector4(1f, 0f, 0f, 1f), vertices.Count).ToList());
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            return Made(mesh);
        }

        [Test]
        public void AFlatSheetsOpenEdges_PushOutward_ItsMiddleDoesNot()
        {
            Mesh grid = Grid(3);
            Assert.IsTrue(OutlineNormalBaker.Bake(grid));
            Vector3[] outward = OpenEdges(grid);
            Vector3[] vertices = grid.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                bool onTheEdge = Mathf.Abs(vertices[i].x) > 1.4f || Mathf.Abs(vertices[i].y) > 1.4f;
                if (!onTheEdge)
                {
                    Assert.AreEqual(Vector3.zero, outward[i], $"vertex {i} inside the sheet");
                    continue;
                }
                Assert.Greater(Vector3.Dot(outward[i], vertices[i]), 0f, $"vertex {i} on the edge points away from the middle");
                Assert.AreEqual(1f, outward[i].magnitude, 1e-3f, $"vertex {i}: a full width, corners too");
            }
        }

        [Test]
        public void AUvSeam_IsNotAnOpenEdge()
        {
            Mesh grid = Grid(2, splitAtTheMiddleColumn: true);
            OutlineNormalBaker.Bake(grid);
            Vector3[] outward = OpenEdges(grid);
            Vector3[] vertices = grid.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (Mathf.Abs(vertices[i].x) > 1e-3f)
                    continue;
                Assert.Less(Mathf.Abs(outward[i].x), 1e-3f, $"seam vertex {i}: no push across the seam");
            }
        }

        [Test]
        public void AClosedMesh_HasNoOpenEdges()
        {
            Mesh cube = Cube();
            OutlineNormalBaker.Bake(cube);
            CollectionAssert.AreEqual(new Vector3[cube.vertexCount], OpenEdges(cube));
        }

        [Test]
        public void ADegenerateTriangle_IsSkipped()
        {
            Mesh plain = Grid(1);
            Mesh withDegenerate = Grid(1);
            var triangles = new List<int>(withDegenerate.triangles) { 0, 0, 1 };
            withDegenerate.SetTriangles(triangles, 0);
            OutlineNormalBaker.Bake(plain);
            OutlineNormalBaker.Bake(withDegenerate);
            CollectionAssert.AreEqual(OpenEdges(plain), OpenEdges(withDegenerate));
        }

        [Test]
        public void AMeshATenThousandthTheSize_BakesTheSame()
        {
            Mesh full = Grid(3);
            Mesh small = Grid(3);
            small.SetVertices(small.vertices.Select(vertex => vertex * 1e-4f).ToList());
            small.RecalculateBounds();
            OutlineNormalBaker.Bake(full);
            OutlineNormalBaker.Bake(small);
            Vector3[] expected = OpenEdges(full), actual = OpenEdges(small);
            for (int i = 0; i < expected.Length; i++)
                Assert.Less(Vector3.Distance(expected[i], actual[i]), 1e-3f, $"vertex {i}");
        }
    }
}
