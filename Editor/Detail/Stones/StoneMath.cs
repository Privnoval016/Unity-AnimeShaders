using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>Finding the stones of a tiled set from its occlusion, as pure image maths at the occlusion's own
     * resolution (thin joints vanish when averaged down): the occlusion is blurred (so pitting doesn't split a stone) and
     * thresholded (grout is darker); each bright area, found on the tile as a torus so a stone cut by the tile's edge is
     * one stone, is a stone. Each texel then holds its stone's value and the stone's home tile relative to the texel's
     * own copy of the tile, so the shader can tell every stone on a floor apart; the result is picked down to
     * <see cref="MapSize" />.</summary>
     * <remarks>
     * The map: R the stone's value (1 to 255, from its wrapped centre; 0 none); G the home-tile code (0 none, else
     * <see cref="Code" /> of the shift, each axis -1, 0 or 1: the tile holding the stone's centre). The code is exact, so
     * both pieces of a stone crossing the tile's edge agree. A stone that wraps onto itself or spans the whole tile is
     * dropped (its texels are then no farther than a tile from its centre, so the shift stays within one); one under the minimum size is left to the fill. Every other texel (joints, pits, small pieces)
     * takes the nearest stone, so joints split down their middle (the shader weights the variation by the shade, so a
     * dark joint barely shows the split). Deterministic: a fixed scan order, ties to the first reached.
     * </remarks>
     */
    public static class StoneMath
    {
        #region Private Fields

        private const int CodesPerAxis = 3;
        private const int Unlabelled = -1;

        private static readonly Vector2Int[] Neighbours = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        private struct Stone
        {
            public long SumX;
            public long SumY;
            public int Count;
            public int MinX;
            public int MaxX;
            public int MinY;
            public int MaxY;
            public bool Wraps;
        }

        #endregion

        #region Properties

        /** <summary>The stone map's width and height.</summary> */
        public const int MapSize = 512;

        /** <summary>The code meaning "no stone here".</summary> */
        public const byte None = 0;

        #endregion

        #region Public API

        /** <summary>The stone map of <paramref name="occlusion" /> (read from red).</summary> */
        public static StoneMap Find(DetailImage occlusion, StoneSettings settings)
        {
            int width = occlusion.Width, height = occlusion.Height, texels = width * height;
            var red = new float[texels];
            for (int i = 0; i < texels; i++)
                red[i] = occlusion.Pixels[i].r / 255f;
            float[] blurred = DetailMath.Blur(red, width, height, settings.Blur / 100f * width);

            var label = new int[texels];
            var unwrappedX = new int[texels];
            var unwrappedY = new int[texels];
            var queue = new int[texels];
            var stones = new List<Stone>();
            Array.Fill(label, Unlabelled);
            for (int start = 0; start < texels; start++)
            {
                if (blurred[start] > settings.Threshold && label[start] == Unlabelled)
                    stones.Add(Flood(start, stones.Count, width, height, blurred, settings.Threshold, label, unwrappedX, unwrappedY, queue));
            }

            int minCount = Mathf.CeilToInt(settings.MinSize / 100f * texels);
            var homeX = new int[stones.Count];
            var homeY = new int[stones.Count];
            var value = new byte[stones.Count];
            var kept = new bool[stones.Count];
            int count = 0, dropped = 0;
            for (int id = 0; id < stones.Count; id++)
            {
                Stone stone = stones[id];
                bool fits = !stone.Wraps && stone.MaxX - stone.MinX < width - 1 && stone.MaxY - stone.MinY < height - 1;
                if (!fits)
                    dropped++;
                kept[id] = fits && stone.Count >= minCount;
                if (!kept[id])
                    continue;
                count++;
                float centreX = (float)stone.SumX / stone.Count + 0.5f;
                float centreY = (float)stone.SumY / stone.Count + 0.5f;
                homeX[id] = Mathf.FloorToInt(centreX / width);
                homeY[id] = Mathf.FloorToInt(centreY / height);
                int cellX = DataTextures.Wrap(Mathf.FloorToInt(centreX), width) * MapSize / width;
                int cellY = DataTextures.Wrap(Mathf.FloorToInt(centreY), height) * MapSize / height;
                value[id] = (byte)(1 + Hash(cellX, cellY) % 255u);
            }

            var values = new byte[texels];
            var codes = new byte[texels];
            var shiftX = new int[texels];
            var shiftY = new int[texels];
            var reached = new bool[texels];
            int sources = 0;
            for (int i = 0; i < texels; i++)
            {
                if (label[i] < 0 || !kept[label[i]])
                    continue;
                int id = label[i];
                shiftX[i] = homeX[id] - FloorDiv(unwrappedX[i], width);
                shiftY[i] = homeY[id] - FloorDiv(unwrappedY[i], height);
                values[i] = value[id];
                codes[i] = Code(shiftX[i], shiftY[i]);
                reached[i] = true;
                queue[sources++] = i;
            }
            Fill(queue, sources, width, height, reached, values, codes, shiftX, shiftY);

            var pixels = new Color32[MapSize * MapSize];
            for (int y = 0; y < MapSize; y++)
            {
                int row = (y * height + height / 2) / MapSize * width;
                for (int x = 0; x < MapSize; x++)
                {
                    int texel = row + (x * width + width / 2) / MapSize;
                    pixels[y * MapSize + x] = new Color32(values[texel], codes[texel], 0, byte.MaxValue);
                }
            }
            return new StoneMap(new DetailImage(MapSize, MapSize, pixels), count, dropped);
        }

        /** <summary>The code of a home-tile shift, each axis -1, 0 or 1: 1 to 9 (0 is <see cref="None" />).</summary> */
        public static byte Code(int shiftX, int shiftY)
        {
            return (byte)((shiftX + 1) * CodesPerAxis + shiftY + 1 + 1);
        }

        /** <summary>The shift a code holds; false for <see cref="None" /> or a code out of range.</summary> */
        public static bool TryShift(byte code, out int shiftX, out int shiftY)
        {
            int index = code - 1;
            shiftX = index / CodesPerAxis - 1;
            shiftY = index % CodesPerAxis - 1;
            return code != None && index < CodesPerAxis * CodesPerAxis;
        }

        #endregion

        #region Helpers

        /** <summary>One stone's texels from <paramref name="start" />, each with its unwrapped coordinates; a texel met again at other coordinates means the stone wraps onto itself.</summary> */
        private static Stone Flood(int start, int id, int width, int height, float[] blurred, float threshold, int[] label, int[] unwrappedX, int[] unwrappedY, int[] queue)
        {
            var stone = new Stone { MinX = int.MaxValue, MinY = int.MaxValue, MaxX = int.MinValue, MaxY = int.MinValue };
            label[start] = id;
            unwrappedX[start] = start % width;
            unwrappedY[start] = start / width;
            int head = 0, tail = 0;
            queue[tail++] = start;
            while (head < tail)
            {
                int texel = queue[head++];
                int x = unwrappedX[texel], y = unwrappedY[texel];
                stone.SumX += x;
                stone.SumY += y;
                stone.Count++;
                stone.MinX = Mathf.Min(stone.MinX, x);
                stone.MaxX = Mathf.Max(stone.MaxX, x);
                stone.MinY = Mathf.Min(stone.MinY, y);
                stone.MaxY = Mathf.Max(stone.MaxY, y);
                foreach (Vector2Int step in Neighbours)
                {
                    int nx = x + step.x, ny = y + step.y;
                    int next = DataTextures.Wrap(ny, height) * width + DataTextures.Wrap(nx, width);
                    if (blurred[next] <= threshold)
                        continue;
                    if (label[next] == Unlabelled)
                    {
                        label[next] = id;
                        unwrappedX[next] = nx;
                        unwrappedY[next] = ny;
                        queue[tail++] = next;
                    }
                    else if (unwrappedX[next] != nx || unwrappedY[next] != ny)
                    {
                        stone.Wraps = true;
                    }
                }
            }
            return stone;
        }

        /** <summary>Spreads the stones into every other texel, nearest first, each stone's shift corrected as the spread crosses the tile's edge.</summary> */
        private static void Fill(int[] queue, int sources, int width, int height, bool[] reached, byte[] values, byte[] codes, int[] shiftX, int[] shiftY)
        {
            int head = 0, tail = sources;
            while (head < tail)
            {
                int texel = queue[head++];
                int x = texel % width, y = texel / width;
                foreach (Vector2Int step in Neighbours)
                {
                    int nx = x + step.x, ny = y + step.y;
                    int next = DataTextures.Wrap(ny, height) * width + DataTextures.Wrap(nx, width);
                    if (reached[next])
                        continue;
                    reached[next] = true;
                    int sx = shiftX[texel] - FloorDiv(nx, width);
                    int sy = shiftY[texel] - FloorDiv(ny, height);
                    if (Mathf.Abs(sx) > 1 || Mathf.Abs(sy) > 1)
                        continue;
                    shiftX[next] = sx;
                    shiftY[next] = sy;
                    values[next] = values[texel];
                    codes[next] = Code(sx, sy);
                    queue[tail++] = next;
                }
            }
        }

        private static int FloorDiv(int value, int period)
        {
            return value >= 0 ? value / period : -((period - 1 - value) / period);
        }

        private static uint Hash(int x, int y)
        {
            uint hash = (uint)x * 0x8da6b343u ^ (uint)y * 0xd8163841u;
            hash ^= hash >> 13;
            hash *= 0x5bd1e995u;
            hash ^= hash >> 15;
            return hash;
        }

        #endregion
    }
}
