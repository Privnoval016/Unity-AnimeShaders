using System;
using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /** <summary>An image as pixels, row by row from the bottom row, the order Unity's textures use.</summary> */
    public sealed class DetailImage
    {
        #region Properties

        /** <summary>The width in pixels.</summary> */
        public int Width { get; }

        /** <summary>The height in pixels.</summary> */
        public int Height { get; }

        /** <summary>The pixels, <see cref="Width" /> times <see cref="Height" /> of them.</summary> */
        public Color32[] Pixels { get; }

        #endregion

        #region Public API

        /** <summary>An image of the given size holding <paramref name="pixels" />.</summary> */
        public DetailImage(int width, int height, Color32[] pixels)
        {
            if (width <= 0 || height <= 0 || pixels == null || pixels.Length != width * height)
                throw new ArgumentException($"A {width} by {height} image needs {width * height} pixels.");
            Width = width;
            Height = height;
            Pixels = pixels;
        }

        /** <summary>An image of the given size, every pixel <paramref name="colour" />.</summary> */
        public static DetailImage Filled(int width, int height, Color32 colour)
        {
            var pixels = new Color32[width * height];
            Array.Fill(pixels, colour);
            return new DetailImage(width, height, pixels);
        }

        #endregion
    }
}
