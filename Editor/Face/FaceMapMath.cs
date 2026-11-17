using System.Collections.Generic;
using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>Baking a face map as pure maths: each face triangle (any vertex weighted to the head) drawn into UV space
     * with its normals' horizontal part (x and z, leaned toward the face's sphere by the sphere blend) and its face
     * weight; the horizontal normal smoothed as a vector within each UV island, never across one; normalised (a texel
     * whose normal is near vertical takes its neighbour's); each island padded by the gutter. The face then lights where
     * the light's horizontal direction is within 90 degrees of the stored one: Lambert in the head's horizontal plane,
     * smoothed.</summary>
     */
    public static class FaceMapMath
    {
        #region Private Fields

        private const float Inside = 1e-4f;
        private const float StrictlyInside = 1e-3f;
        private const float FaceVertex = 0.5f;
        private const float Percent = 100f;
        private const float Degenerate = 1e-4f;
        private const float KernelReach = 3f;

        private static readonly Vector2Int[] Neighbours = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        #endregion

        #region Properties

        /** <summary>A horizontal component of 0 as stored.</summary> */
        public const float Middle = 0.5f;

        #endregion

        #region Public API

        /** <summary>The face map of <paramref name="source" />.</summary> */
        public static FaceMap Bake(FaceMapSource source, FaceMapSettings settings)
        {
            int size = settings.Size, texels = size * size;
            Vector3 centre = Vector3.zero, mean = Vector3.zero;
            float total = 0f;
            for (int i = 0; i < source.Positions.Length; i++)
            {
                float weight = source.FaceWeights[i];
                if (weight < FaceVertex)
                    continue;
                centre += source.Positions[i] * weight;
                mean += source.Normals[i].normalized * weight;
                total += weight;
            }
            centre = total > 0f ? centre / total : Vector3.zero;

            var vertexX = new float[source.Positions.Length];
            var vertexZ = new float[source.Positions.Length];
            for (int i = 0; i < vertexX.Length; i++)
            {
                Vector3 normal = source.Normals[i].normalized;
                if (settings.SphereBlend > 0f && total > 0f)
                    normal = Vector3.Lerp(normal, (source.Positions[i] - centre).normalized, settings.SphereBlend).normalized;
                vertexX[i] = normal.x;
                vertexZ[i] = normal.z;
            }

            var x = new float[texels];
            var z = new float[texels];
            var weights = new float[texels];
            var covered = new bool[texels];
            var strict = new bool[texels];
            int overlapping = Draw(source, size, vertexX, vertexZ, x, z, weights, covered, strict);

            Smooth(x, z, covered, size, settings.Smoothing / Percent * size);

            var strong = new bool[texels];
            for (int i = 0; i < texels; i++)
            {
                if (!covered[i])
                    continue;
                float length = Mathf.Sqrt(x[i] * x[i] + z[i] * z[i]);
                strong[i] = length > Degenerate;
                if (!strong[i])
                    continue;
                x[i] /= length;
                z[i] /= length;
            }
            bool[] filled = Fill(x, z, weights, covered, strong, size, settings.Gutter);

            var pixels = new Color[texels];
            int face = 0, strictCount = 0;
            for (int i = 0; i < texels; i++)
            {
                if (covered[i] && weights[i] >= FaceVertex)
                    face++;
                if (strict[i])
                    strictCount++;
                pixels[i] = strong[i] || filled[i]
                    ? new Color(x[i] * Middle + Middle, z[i] * Middle + Middle, weights[i], 1f)
                    : new Color(Middle, Middle, 0f, 0f);
            }
            float overlap = strictCount > 0 ? (float)overlapping / strictCount : 0f;
            return new FaceMap(pixels, size, Mathf.RoundToInt(Mathf.Sqrt(face)), overlap, mean.normalized);
        }

        #endregion

        #region Helpers

        /** <summary>Draws every face triangle into UV space; returns how many texels two triangles both covered from the inside.</summary> */
        private static int Draw(FaceMapSource source, int size, float[] vertexX, float[] vertexZ, float[] x, float[] z, float[] weights, bool[] covered, bool[] strict)
        {
            int overlapping = 0;
            int[] triangles = source.Triangles;
            for (int t = 0; t < triangles.Length; t += 3)
            {
                int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
                if (Mathf.Max(source.FaceWeights[a], Mathf.Max(source.FaceWeights[b], source.FaceWeights[c])) <= 0f)
                    continue;
                Vector2 pa = source.Uvs[a] * size, pb = source.Uvs[b] * size, pc = source.Uvs[c] * size;
                float area = (pb.x - pa.x) * (pc.y - pa.y) - (pc.x - pa.x) * (pb.y - pa.y);
                if (Mathf.Abs(area) < Degenerate)
                    continue;
                int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(pa.x, Mathf.Min(pb.x, pc.x))), 0, size - 1);
                int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(pa.x, Mathf.Max(pb.x, pc.x))), 0, size - 1);
                int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(pa.y, Mathf.Min(pb.y, pc.y))), 0, size - 1);
                int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(pa.y, Mathf.Max(pb.y, pc.y))), 0, size - 1);
                for (int ty = minY; ty <= maxY; ty++)
                {
                    for (int tx = minX; tx <= maxX; tx++)
                    {
                        var q = new Vector2(tx + 0.5f, ty + 0.5f);
                        float l0 = ((pb.x - q.x) * (pc.y - q.y) - (pc.x - q.x) * (pb.y - q.y)) / area;
                        float l1 = ((pc.x - q.x) * (pa.y - q.y) - (pa.x - q.x) * (pc.y - q.y)) / area;
                        float l2 = 1f - l0 - l1;
                        if (l0 < -Inside || l1 < -Inside || l2 < -Inside)
                            continue;
                        int i = ty * size + tx;
                        bool inside = l0 > StrictlyInside && l1 > StrictlyInside && l2 > StrictlyInside;
                        if (inside && strict[i])
                            overlapping++;
                        x[i] = l0 * vertexX[a] + l1 * vertexX[b] + l2 * vertexX[c];
                        z[i] = l0 * vertexZ[a] + l1 * vertexZ[b] + l2 * vertexZ[c];
                        weights[i] = l0 * source.FaceWeights[a] + l1 * source.FaceWeights[b] + l2 * source.FaceWeights[c];
                        covered[i] = true;
                        strict[i] |= inside;
                    }
                }
            }
            return overlapping;
        }

        /**
         * <summary>Blurs the horizontal normal within each island (weighted by coverage, so nothing outside it leaks in),
         * on a crop padded past the kernel's reach on every side, even beyond the map, since the blur wraps.</summary>
         */
        private static void Smooth(float[] x, float[] z, bool[] covered, int size, float sigma)
        {
            if (sigma <= 0f)
                return;
            int margin = Mathf.CeilToInt(KernelReach * sigma) + 1;
            foreach (List<int> island in Islands(covered, size))
            {
                int minX = size, minY = size, maxX = 0, maxY = 0;
                foreach (int i in island)
                {
                    minX = Mathf.Min(minX, i % size);
                    maxX = Mathf.Max(maxX, i % size);
                    minY = Mathf.Min(minY, i / size);
                    maxY = Mathf.Max(maxY, i / size);
                }
                minX -= margin;
                minY -= margin;
                int width = maxX + margin - minX + 1;
                int height = maxY + margin - minY + 1;
                var cropX = new float[width * height];
                var cropZ = new float[width * height];
                var mask = new float[width * height];
                foreach (int i in island)
                {
                    int at = (i / size - minY) * width + i % size - minX;
                    cropX[at] = x[i];
                    cropZ[at] = z[i];
                    mask[at] = 1f;
                }
                float[] blurredX = DetailMath.Blur(cropX, width, height, sigma);
                float[] blurredZ = DetailMath.Blur(cropZ, width, height, sigma);
                float[] blurredMask = DetailMath.Blur(mask, width, height, sigma);
                foreach (int i in island)
                {
                    int at = (i / size - minY) * width + i % size - minX;
                    if (blurredMask[at] <= Degenerate)
                        continue;
                    x[i] = blurredX[at] / blurredMask[at];
                    z[i] = blurredZ[at] / blurredMask[at];
                }
            }
        }

        /** <summary>The covered texels in connected islands (each texel's four neighbours).</summary> */
        private static List<List<int>> Islands(bool[] covered, int size)
        {
            var islands = new List<List<int>>();
            var seen = new bool[covered.Length];
            var queue = new Queue<int>();
            for (int start = 0; start < covered.Length; start++)
            {
                if (!covered[start] || seen[start])
                    continue;
                var island = new List<int>();
                seen[start] = true;
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    int i = queue.Dequeue();
                    island.Add(i);
                    foreach (Vector2Int step in Neighbours)
                    {
                        int nx = i % size + step.x, ny = i / size + step.y;
                        if (nx < 0 || ny < 0 || nx >= size || ny >= size)
                            continue;
                        int next = ny * size + nx;
                        if (!covered[next] || seen[next])
                            continue;
                        seen[next] = true;
                        queue.Enqueue(next);
                    }
                }
                islands.Add(island);
            }
            return islands;
        }

        /**
         * <summary>Gives each weak texel (its normal near vertical) the nearest strong texel's values, and each empty texel
         * within <paramref name="gutter" /> steps of the face the nearest one's; returns which texels were filled.</summary>
         */
        private static bool[] Fill(float[] x, float[] z, float[] weights, bool[] covered, bool[] strong, int size, int gutter)
        {
            var filled = new bool[x.Length];
            var steps = new int[x.Length];
            var queue = new Queue<int>();
            for (int i = 0; i < x.Length; i++)
            {
                if (strong[i])
                    queue.Enqueue(i);
            }
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                foreach (Vector2Int step in Neighbours)
                {
                    int nx = i % size + step.x, ny = i / size + step.y;
                    if (nx < 0 || ny < 0 || nx >= size || ny >= size)
                        continue;
                    int next = ny * size + nx;
                    if (strong[next] || filled[next])
                        continue;
                    int reached = covered[next] ? 0 : steps[i] + 1;
                    if (!covered[next] && reached > gutter)
                        continue;
                    filled[next] = true;
                    steps[next] = reached;
                    x[next] = x[i];
                    z[next] = z[i];
                    if (!covered[next])
                        weights[next] = weights[i];
                    queue.Enqueue(next);
                }
            }
            return filled;
        }

        #endregion
    }
}
