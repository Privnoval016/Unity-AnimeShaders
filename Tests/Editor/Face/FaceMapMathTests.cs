using System;
using System.Collections.Generic;
using AnimeShaders.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace AnimeShaders.Tests
{
    /**
     * <summary>The face map's bake on made faces: a hemisphere facing the front stores its horizontal normal and lights
     * as plain Lambert does round the horizon; islands are smoothed apart, never across the gap between them; the gutter
     * pads each island by its width and no more; overlapping triangles are counted; the face weight is interpolated and
     * weightless triangles left out; a texel whose normal is vertical takes its neighbour's direction; the same source
     * bakes the same map; the mean normal tells a face turned away from the front.</summary>
     */
    public class FaceMapMathTests
    {
        #region Private Fields

        private const int Size = 128;
        private const int Grid = 48;
        private const float Radius = 0.95f;
        private const float MostDegrees = 3f;
        private const float LeastAgreement = 0.99f;
        private const float Close = 1e-3f;

        private static readonly FaceMapSettings Plain = new(Size, 0f, 0f, 0);

        #endregion

        #region Helpers

        /** <summary>A unit hemisphere facing +Z (or <paramref name="facing" />'s turn of it), laid flat in UV by x and y.</summary> */
        private static FaceMapSource Hemisphere(Quaternion facing)
        {
            var positions = new List<Vector3>();
            var uvs = new List<Vector2>();
            var index = new int[(Grid + 1) * (Grid + 1)];
            for (int j = 0; j <= Grid; j++)
            {
                for (int i = 0; i <= Grid; i++)
                {
                    float x = (i / (float)Grid * 2f - 1f) * Radius, y = (j / (float)Grid * 2f - 1f) * Radius;
                    float z = Mathf.Sqrt(Mathf.Max(0f, 1f - x * x - y * y));
                    index[j * (Grid + 1) + i] = positions.Count;
                    positions.Add(facing * new Vector3(x, y, z));
                    uvs.Add(UvOf(x, y));
                }
            }
            var triangles = new List<int>();
            for (int j = 0; j < Grid; j++)
            {
                for (int i = 0; i < Grid; i++)
                {
                    int a = index[j * (Grid + 1) + i], b = a + 1, c = a + Grid + 1, d = c + 1;
                    if (!Inside(positions[a]) || !Inside(positions[b]) || !Inside(positions[c]) || !Inside(positions[d]))
                        continue;
                    triangles.AddRange(new[] { a, c, b, b, c, d });
                }
            }
            var weights = new float[positions.Count];
            Array.Fill(weights, 1f);
            return new FaceMapSource(positions.ToArray(), positions.ToArray(), uvs.ToArray(), triangles.ToArray(), weights);

            bool Inside(Vector3 p) => (Quaternion.Inverse(facing) * p).z > 1f - Radius;
        }

        private static Vector2 UvOf(float x, float y)
        {
            return new Vector2(x * 0.4f + 0.5f, y * 0.4f + 0.5f);
        }

        /** <summary>Quads in UV, each its own vertices: corners (u0, v0) to (u1, v1), the left edge's and right edge's normals and face weights.</summary> */
        private static FaceMapSource Quads(params (Vector2 From, Vector2 To, Vector3 LeftNormal, Vector3 RightNormal, float LeftWeight, float RightWeight)[] quads)
        {
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var weights = new List<float>();
            var triangles = new List<int>();
            foreach ((Vector2 from, Vector2 to, Vector3 left, Vector3 right, float leftWeight, float rightWeight) in quads)
            {
                int start = positions.Count;
                foreach (Vector2 corner in new[] { from, new Vector2(to.x, from.y), new Vector2(from.x, to.y), to })
                {
                    bool isLeft = Mathf.Approximately(corner.x, from.x);
                    positions.Add(corner);
                    uvs.Add(corner);
                    normals.Add(isLeft ? left : right);
                    weights.Add(isLeft ? leftWeight : rightWeight);
                }
                triangles.AddRange(new[] { start, start + 2, start + 1, start + 1, start + 2, start + 3 });
            }
            return new FaceMapSource(positions.ToArray(), normals.ToArray(), uvs.ToArray(), triangles.ToArray(), weights.ToArray());
        }

        private static Vector2 Direction(FaceMap map, int x, int y)
        {
            Color pixel = map.Pixels[y * map.Size + x];
            return new Vector2(pixel.r * 2f - 1f, pixel.g * 2f - 1f);
        }

        private static Color At(FaceMap map, Vector2 uv)
        {
            return map.Pixels[Mathf.FloorToInt(uv.y * map.Size) * map.Size + Mathf.FloorToInt(uv.x * map.Size)];
        }

        /** <summary>Where the hemisphere's texel (x, y) lies on it, or false off it.</summary> */
        private static bool OnHemisphere(int x, int y, float within, out Vector3 point)
        {
            float px = ((x + 0.5f) / Size - 0.5f) / 0.4f, py = ((y + 0.5f) / Size - 0.5f) / 0.4f;
            float rest = 1f - px * px - py * py;
            point = new Vector3(px, py, Mathf.Sqrt(Mathf.Max(0f, rest)));
            return px * px + py * py < within * within;
        }

        #endregion

        [Test]
        public void AHemisphere_StoresItsHorizontalNormal()
        {
            FaceMap map = FaceMapMath.Bake(Hemisphere(Quaternion.identity), Plain);
            int checkedTexels = 0;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (!OnHemisphere(x, y, 0.85f, out Vector3 point))
                        continue;
                    Vector2 expected = new Vector2(point.x, point.z).normalized;
                    Assert.Less(Vector2.Angle(expected, Direction(map, x, y)), MostDegrees, $"texel {x}, {y}");
                    checkedTexels++;
                }
            }
            Assert.Greater(checkedTexels, Size * Size / 4, "the hemisphere covers the map's middle");
        }

        [Test]
        public void AHemisphere_LightsAsLambert_AllRoundTheHorizon()
        {
            FaceMap map = FaceMapMath.Bake(Hemisphere(Quaternion.identity), new FaceMapSettings(Size, 0.3f, 0f, 4));
            for (int step = 0; step < 8; step++)
            {
                float angle = step * 45f * Mathf.Deg2Rad + 0.1f;
                var light = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                int agree = 0, total = 0;
                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        if (!OnHemisphere(x, y, Radius * 0.97f, out Vector3 point))
                            continue;
                        bool lambert = point.x * light.x + point.z * light.y > 0f;
                        bool mapped = Vector2.Dot(Direction(map, x, y), light) > 0f;
                        agree += lambert == mapped ? 1 : 0;
                        total++;
                    }
                }
                Assert.GreaterOrEqual(agree / (float)total, LeastAgreement, $"light at {step * 45} degrees");
            }
        }

        [Test]
        public void Islands_AreSmoothedApart_NeverAcrossTheGap()
        {
            Vector3 leftward = new Vector3(-1f, 0f, 1f).normalized, rightward = new Vector3(1f, 0f, 1f).normalized;
            FaceMap map = FaceMapMath.Bake(Quads(
                (new Vector2(0.2f, 0.2f), new Vector2(0.48f, 0.8f), rightward, rightward, 1f, 1f),
                (new Vector2(0.52f, 0.2f), new Vector2(0.8f, 0.8f), leftward, leftward, 1f, 1f)), new FaceMapSettings(Size, 3f, 0f, 0));
            Assert.Less(Vector2.Distance(new Vector2(rightward.x, rightward.z), Direction(map, 60, 64)), Close, "the left island's edge by the gap");
            Assert.Less(Vector2.Distance(new Vector2(leftward.x, leftward.z), Direction(map, 67, 64)), Close, "the right island's edge by the gap");
        }

        [Test]
        public void TheGutter_PadsEachIsland_ByItsWidth()
        {
            const int gutter = 4;
            FaceMap map = FaceMapMath.Bake(Quads((new Vector2(0.5f, 0.25f), new Vector2(0.75f, 0.75f), Vector3.right, Vector3.right, 1f, 1f)),
                new FaceMapSettings(Size, 0f, 0f, gutter));
            int edge = 64, row = 64;
            for (int step = 1; step <= gutter; step++)
            {
                Color padded = map.Pixels[row * Size + edge - step];
                Assert.AreEqual(1f, padded.a, $"{step} texels out: padded");
                Assert.Less(Vector2.Distance(Vector2.right, Direction(map, edge - step, row)), Close, $"{step} texels out: the edge's direction");
            }
            Assert.AreEqual(0f, map.Pixels[row * Size + edge - gutter - 1].a, "past the gutter: empty");
        }

        [Test]
        public void OverlappingTriangles_AreCounted()
        {
            var quad = (new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.75f), Vector3.forward, Vector3.forward, 1f, 1f);
            Assert.AreEqual(0f, FaceMapMath.Bake(Quads(quad), Plain).Overlap, "one quad: no overlap");
            Assert.Greater(FaceMapMath.Bake(Quads(quad, quad), Plain).Overlap, 0.9f, "two on the same UVs: nearly all of it");
        }

        [Test]
        public void TheFaceWeight_IsInterpolated_AndWeightlessTrianglesLeftOut()
        {
            FaceMap map = FaceMapMath.Bake(Quads(
                (new Vector2(0.1f, 0.1f), new Vector2(0.5f, 0.5f), Vector3.forward, Vector3.forward, 0f, 1f),
                (new Vector2(0.6f, 0.6f), new Vector2(0.9f, 0.9f), Vector3.forward, Vector3.forward, 0f, 0f)), Plain);
            Assert.AreEqual(0.25f, At(map, new Vector2(0.2f, 0.3f)).b, 0.02f, "a quarter across");
            Assert.AreEqual(0.75f, At(map, new Vector2(0.4f, 0.3f)).b, 0.02f, "three quarters across");
            Assert.AreEqual(0f, At(map, new Vector2(0.75f, 0.75f)).a, "a quad with no face weight isn't drawn");
        }

        [Test]
        public void AVerticalNormal_TakesItsNeighboursDirection()
        {
            FaceMap map = FaceMapMath.Bake(Quads(
                (new Vector2(0.25f, 0.25f), new Vector2(0.5f, 0.75f), Vector3.up, Vector3.up, 1f, 1f),
                (new Vector2(0.5f, 0.25f), new Vector2(0.75f, 0.75f), Vector3.forward, Vector3.forward, 1f, 1f)), Plain);
            Color weak = At(map, new Vector2(0.3f, 0.5f));
            Assert.AreEqual(1f, weak.a, "covered");
            Assert.Less(Vector2.Distance(Vector2.up, new Vector2(weak.r * 2f - 1f, weak.g * 2f - 1f)), Close, "facing the front, as its neighbour");
        }

        [Test]
        public void TheSameSource_BakesTheSameMap()
        {
            var settings = new FaceMapSettings(Size, 1f, 0.3f, 4);
            Assert.AreEqual(FaceMapMath.Bake(Hemisphere(Quaternion.identity), settings).Pixels, FaceMapMath.Bake(Hemisphere(Quaternion.identity), settings).Pixels);
        }

        [Test]
        public void TheMeanNormal_TellsAFaceTurnedFromTheFront()
        {
            Assert.Greater(FaceMapMath.Bake(Hemisphere(Quaternion.identity), Plain).MeanNormal.z, 0.9f, "facing the front");
            Assert.Less(FaceMapMath.Bake(Hemisphere(Quaternion.Euler(-90f, 0f, 0f)), Plain).MeanNormal.z, 0.1f, "facing up");
        }
    }
}
