using System;
using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>Packing a detail set's maps into one detail map, as pure image maths: R the shade, G the mask, B and A
     * the normal's X and Y.</summary>
     * <remarks>The shade is the colour's luminance as stored (perceptual), softened, darkened by occlusion, stretched so
     * its 1st to 99th percentile fills 0..1, then curved so its mean is <see cref="Middle" /> (the ground blends and
     * fades about it, and its mips settle there). A flat shade is only moved to the middle. Every step wraps, so a
     * tiling set packs to a tiling map.</remarks>
     */
    public static class DetailMath
    {
        #region Private Fields

        private const int HistogramBins = 1024;
        private const float LowPercentile = 0.01f;
        private const float HighPercentile = 0.99f;
        private const float KernelReach = 3f;
        private const float FlatRange = 1f / 255f;
        private const float MeanMargin = 1e-3f;
        private const float MinPower = 1f / 16f;
        private const float MaxPower = 16f;
        private const int PowerSearchSteps = 32;

        private static readonly Vector3 LuminanceWeights = new(0.2126f, 0.7152f, 0.0722f);

        #endregion

        #region Properties

        /** <summary>The shade's mean in every packed map, and the neutral shade: the middle the ground's shader blends about.</summary> */
        public const float Middle = 0.5f;

        #endregion

        #region Public API

        /** <summary>The packed map of <paramref name="sources" />.</summary> */
        public static DetailImage Pack(DetailSources sources, DetailSettings settings)
        {
            if (sources.Colour == null || sources.Normal == null)
                throw new ArgumentException($"A detail set needs its {(sources.Colour == null ? "colour" : "normal")} map.");
            DetailImage colour = sources.Colour;
            int width = settings.Size > 0 ? settings.Size : colour.Width;
            int height = settings.Size > 0 ? settings.Size : colour.Height;

            float sigma = settings.Soften / 100f * colour.Width;
            float[] shade = Resample(Blur(Luminance(colour), colour.Width, colour.Height, sigma), colour, width, height);
            if (sources.Occlusion != null)
            {
                float[] occluded = Resample(Channel(sources.Occlusion, 0), sources.Occlusion, width, height);
                for (int i = 0; i < shade.Length; i++)
                    shade[i] *= Mathf.Lerp(1f, occluded[i], settings.OcclusionWeight);
            }
            Stretch(shade);

            float[] masked = sources.Mask == null ? null : Resample(Channel(sources.Mask, 0), sources.Mask, width, height);
            float[] normalX = Resample(Channel(sources.Normal, 0), sources.Normal, width, height);
            float[] normalY = Resample(Channel(sources.Normal, 1), sources.Normal, width, height);
            bool flipY = settings.Normals == NormalConvention.DirectX;
            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(DataTextures.Byte(shade[i]), DataTextures.Byte(masked == null ? 1f : masked[i]),
                    DataTextures.Byte(normalX[i]), DataTextures.Byte(flipY ? 1f - normalY[i] : normalY[i]));
            }
            return new DetailImage(width, height, pixels);
        }

        /** <summary>The neutral map: shade at the middle, mask 1, a flat normal.</summary> */
        public static DetailImage Neutral(int size)
        {
            byte middle = DataTextures.Byte(Middle);
            return DetailImage.Filled(size, size, new Color32(middle, byte.MaxValue, middle, middle));
        }

        /** <summary>A Gaussian blur of <paramref name="sigma" /> texels that wraps at every edge.</summary> */
        internal static float[] Blur(float[] values, int width, int height, float sigma)
        {
            if (sigma <= 0f)
                return values;
            int reach = Mathf.CeilToInt(KernelReach * sigma);
            int span = reach * 2 + 1;
            var kernel = new float[span];
            float total = 0f;
            for (int k = -reach; k <= reach; k++)
            {
                kernel[k + reach] = Mathf.Exp(-k * k / (2f * sigma * sigma));
                total += kernel[k + reach];
            }
            for (int k = 0; k < span; k++)
                kernel[k] /= total;

            int[] across = WrappedTaps(width, reach);
            int[] down = WrappedTaps(height, reach);
            var horizontal = new float[values.Length];
            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    float sum = 0f;
                    int tap = x * span;
                    for (int k = 0; k < span; k++)
                        sum += kernel[k] * values[row + across[tap + k]];
                    horizontal[row + x] = sum;
                }
            }
            var blurred = new float[values.Length];
            for (int y = 0; y < height; y++)
            {
                int tap = y * span;
                for (int x = 0; x < width; x++)
                {
                    float sum = 0f;
                    for (int k = 0; k < span; k++)
                        sum += kernel[k] * horizontal[down[tap + k] * width + x];
                    blurred[y * width + x] = sum;
                }
            }
            return blurred;
        }

        #endregion

        #region Helpers

        private static float[] Luminance(DetailImage image)
        {
            var values = new float[image.Pixels.Length];
            for (int i = 0; i < values.Length; i++)
            {
                Color32 pixel = image.Pixels[i];
                values[i] = (pixel.r * LuminanceWeights.x + pixel.g * LuminanceWeights.y + pixel.b * LuminanceWeights.z) / 255f;
            }
            return values;
        }

        private static float[] Channel(DetailImage image, int channel)
        {
            var values = new float[image.Pixels.Length];
            for (int i = 0; i < values.Length; i++)
                values[i] = image.Pixels[i][channel] / 255f;
            return values;
        }

        /** <summary>The kernel's taps as indices into a line of <paramref name="length" />, wrapped: one row of offsets per position.</summary> */
        private static int[] WrappedTaps(int length, int reach)
        {
            int span = reach * 2 + 1;
            var taps = new int[length * span];
            for (int position = 0; position < length; position++)
            {
                for (int k = -reach; k <= reach; k++)
                    taps[position * span + k + reach] = DataTextures.Wrap(position + k, length);
            }
            return taps;
        }

        /** <summary>Bilinear, wrapping, from <paramref name="source" />'s size to the given one; the same size is returned as it is.</summary> */
        private static float[] Resample(float[] values, DetailImage source, int width, int height)
        {
            if (source.Width == width && source.Height == height)
                return values;
            var resampled = new float[width * height];
            for (int y = 0; y < height; y++)
            {
                float sy = (y + 0.5f) * source.Height / height - 0.5f;
                int y0 = Mathf.FloorToInt(sy);
                float fy = sy - y0;
                int row0 = DataTextures.Wrap(y0, source.Height) * source.Width;
                int row1 = DataTextures.Wrap(y0 + 1, source.Height) * source.Width;
                for (int x = 0; x < width; x++)
                {
                    float sx = (x + 0.5f) * source.Width / width - 0.5f;
                    int x0 = Mathf.FloorToInt(sx);
                    float fx = sx - x0;
                    int c0 = DataTextures.Wrap(x0, source.Width), c1 = DataTextures.Wrap(x0 + 1, source.Width);
                    float bottom = Mathf.Lerp(values[row0 + c0], values[row0 + c1], fx);
                    float top = Mathf.Lerp(values[row1 + c0], values[row1 + c1], fx);
                    resampled[y * width + x] = Mathf.Lerp(bottom, top, fy);
                }
            }
            return resampled;
        }

        /** <summary>Stretches the 1st to 99th percentile over 0..1 and curves the result so its mean is the middle; a flat range is only moved to the middle.</summary> */
        private static void Stretch(float[] values)
        {
            int[] counts = Histogram(values);
            float low = Percentile(counts, values.Length, LowPercentile, false);
            float high = Percentile(counts, values.Length, HighPercentile, true);

            if (high - low < FlatRange)
            {
                float shift = Middle - Mean(values);
                for (int i = 0; i < values.Length; i++)
                    values[i] = Mathf.Clamp01(values[i] + shift);
                return;
            }
            for (int i = 0; i < values.Length; i++)
                values[i] = Mathf.Clamp01((values[i] - low) / (high - low));
            float mean = Mean(values);
            if (mean < MeanMargin || mean > 1f - MeanMargin)
                return;
            float power = PowerForMiddle(Histogram(values), values.Length);
            for (int i = 0; i < values.Length; i++)
                values[i] = Mathf.Pow(values[i], power);
        }

        private static int[] Histogram(float[] values)
        {
            var counts = new int[HistogramBins];
            foreach (float value in values)
                counts[Mathf.Clamp((int)(value * HistogramBins), 0, HistogramBins - 1)]++;
            return counts;
        }

        /** <summary>The value below which <paramref name="fraction" /> of the counted values lie: a bin's low edge, or its high edge for the upper percentile.</summary> */
        private static float Percentile(int[] counts, int total, float fraction, bool upperEdge)
        {
            float wanted = fraction * total;
            int running = 0;
            for (int bin = 0; bin < counts.Length; bin++)
            {
                running += counts[bin];
                if (running >= wanted)
                    return (bin + (upperEdge ? 1f : 0f)) / HistogramBins;
            }
            return 1f;
        }

        /**
         * <summary>The power that brings the mean of the counted values (0..1) to the middle, found by halving the range
         * of powers: the mean of the powered values falls as the power rises.</summary>
         */
        private static float PowerForMiddle(int[] counts, int total)
        {
            float lowLog = Mathf.Log(MinPower), highLog = Mathf.Log(MaxPower);
            for (int step = 0; step < PowerSearchSteps; step++)
            {
                float middleLog = (lowLog + highLog) * 0.5f;
                if (PoweredMean(counts, total, Mathf.Exp(middleLog)) > Middle)
                    lowLog = middleLog;
                else
                    highLog = middleLog;
            }
            return Mathf.Exp((lowLog + highLog) * 0.5f);
        }

        private static float PoweredMean(int[] counts, int total, float power)
        {
            double sum = 0;
            for (int bin = 0; bin < counts.Length; bin++)
            {
                if (counts[bin] > 0)
                    sum += counts[bin] * Math.Pow((bin + 0.5) / counts.Length, power);
            }
            return (float)(sum / total);
        }

        private static float Mean(float[] values)
        {
            double sum = 0;
            foreach (float value in values)
                sum += value;
            return (float)(sum / values.Length);
        }

        #endregion
    }
}
