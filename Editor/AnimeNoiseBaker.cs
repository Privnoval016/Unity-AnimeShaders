using UnityEditor;
using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>Bakes the pack's noise texture, a tiling 1024 square RGBA made by this code alone (maths, no art): R
     * value noise, G fbm, B the distance to the nearest cell edge (flagstone grout), A a value per cell (each stone's
     * shade). Shaders sample it instead of computing noise per pixel. Every channel tiles.</summary>
     * <remarks>Saved as a PNG beside the pack's shaders; <see cref="AnimeNoiseImport" /> forces its import settings.
     * Deterministic: the same bytes on every bake.</remarks>
     */
    public static class AnimeNoiseBaker
    {
        #region Private Fields

        private const int ValueCells = 16;
        private const int FbmCells = 8;
        private const int FbmOctaves = 4;
        private const int StoneCells = 8;
        private const float EdgeRange = 0.25f;


        #endregion

        #region Properties

        /** <summary>The texture's width and height in pixels.</summary> */
        public const int Size = 1024;

        /** <summary>The bake's version: raise it when the noise changes, and bake again.</summary> */
        public const int Version = 1;

        /** <summary>The file name the bake is saved under, beside the pack's shaders.</summary> */
        public const string FileName = "AnimeNoise.png";

        #endregion

        #region Public API

        /** <summary>The noise as pixels (row by row, bottom row first), <paramref name="size" /> square.</summary> */
        public static Color32[] Pixels(int size = Size)
        {
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;
                    Stone(u, v, out float edge, out float cell);
                    pixels[y * size + x] = new Color32(
                        DataTextures.Byte(ValueNoise(u * ValueCells, v * ValueCells, ValueCells, 0)),
                        DataTextures.Byte(Fbm(u, v)),
                        DataTextures.Byte(edge),
                        DataTextures.Byte(cell));
                }
            }
            return pixels;
        }

        /** <summary>Bakes the noise and writes it as a PNG at <paramref name="path" /> (a project path), then imports it.</summary> */
        public static void Bake(string path)
        {
            DataTextures.Write(Pixels(), Size, Size, path, AnimeNoiseImport.Label);
        }

        /** <summary>Bakes the noise into the pack, beside its shaders, and makes it the default of every pack shader that reads it.</summary> */
        [MenuItem("Tools/Anime Shaders/Bake Noise Texture")]
        public static void BakeIntoPack()
        {
            string path = PackPath();
            Bake(path);
            AssignDefaults(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        }

        /** <summary>Sets <paramref name="noise" /> as the default noise texture of every pack shader that has the property, so new materials take it.</summary> */
        public static void AssignDefaults(Texture2D noise)
        {
            ShaderDefaults.Set(new[] { AnimeShaderIds.Sky.NoiseTexName }, noise);
        }

        /** <summary>Where the pack's noise file sits: in the Textures folder beside the pack's shaders.</summary> */
        public static string PackPath()
        {
            return $"{ShaderDefaults.PackShadersFolder()}/Textures/{FileName}";
        }

        #endregion

        #region Helpers

        private static uint Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)x * 0x8da6b343u ^ (uint)y * 0xd8163841u ^ (uint)seed * 0xcb1ab31fu;
                h ^= h >> 13;
                h *= 0x5bd1e995u;
                h ^= h >> 15;
                return h;
            }
        }

        private static float Random01(int x, int y, int seed)
        {
            return (Hash(x, y, seed) & 0xffffffu) / 16777215f;
        }

        private static float Fade(float t)
        {
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        /** <summary>Smooth value noise on a grid of <paramref name="period" /> cells that wraps, 0 to 1.</summary> */
        private static float ValueNoise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = Fade(x - x0), fy = Fade(y - y0);
            float a = Random01(DataTextures.Wrap(x0, period), DataTextures.Wrap(y0, period), seed);
            float b = Random01(DataTextures.Wrap(x0 + 1, period), DataTextures.Wrap(y0, period), seed);
            float c = Random01(DataTextures.Wrap(x0, period), DataTextures.Wrap(y0 + 1, period), seed);
            float d = Random01(DataTextures.Wrap(x0 + 1, period), DataTextures.Wrap(y0 + 1, period), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static float Fbm(float u, float v)
        {
            float sum = 0f, amplitude = 0.5f, total = 0f;
            int cells = FbmCells;
            for (int octave = 0; octave < FbmOctaves; octave++)
            {
                sum += amplitude * ValueNoise(u * cells, v * cells, cells, octave + 1);
                total += amplitude;
                amplitude *= 0.5f;
                cells *= 2;
            }
            return sum / total;
        }

        /** <summary>Jittered cells that wrap: the distance to the nearest cell edge (0 on it, 1 at <see cref="EdgeRange" /> of a cell or more) and the nearest cell's own value.</summary> */
        private static void Stone(float u, float v, out float edge, out float cell)
        {
            var here = new Vector2(u * StoneCells, v * StoneCells);
            int cx = Mathf.FloorToInt(here.x), cy = Mathf.FloorToInt(here.y);
            Vector2 nearest = Vector2.zero;
            float nearestDistance = float.MaxValue;
            int nearestX = 0, nearestY = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    Vector2 point = CellPoint(cx + dx, cy + dy, out int wx, out int wy);
                    float distance = (point - here).sqrMagnitude;
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = point;
                        nearestX = wx;
                        nearestY = wy;
                    }
                }
            }

            float toEdge = float.MaxValue;
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    Vector2 point = CellPoint(cx + dx, cy + dy, out _, out _);
                    Vector2 between = point - nearest;
                    if (between.sqrMagnitude < 1e-8f)
                        continue;
                    toEdge = Mathf.Min(toEdge, Vector2.Dot((nearest + point) * 0.5f - here, between.normalized));
                }
            }
            edge = Mathf.Clamp01(toEdge / EdgeRange);
            cell = Random01(nearestX, nearestY, 13);
        }

        private static Vector2 CellPoint(int gx, int gy, out int wx, out int wy)
        {
            wx = DataTextures.Wrap(gx, StoneCells);
            wy = DataTextures.Wrap(gy, StoneCells);
            return new Vector2(gx + 0.15f + 0.7f * Random01(wx, wy, 11), gy + 0.15f + 0.7f * Random01(wx, wy, 12));
        }

        #endregion
    }
}
