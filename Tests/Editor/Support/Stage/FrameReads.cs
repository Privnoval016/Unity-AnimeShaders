using System;
using UnityEngine;

namespace AnimeShaders.Tests
{
    /** <summary>Reads on a frame from <see cref="LitStage.Render" /> (bottom row first, square).</summary> */
    public static class FrameReads
    {
        #region Public API

        /** <summary>The pixel at column <paramref name="x" />, row <paramref name="y" />.</summary> */
        public static Color At(Color[] frame, int x, int y)
        {
            return frame[y * Side(frame) + x];
        }

        /** <summary>Whether two colours' red, green and blue each differ by at most <paramref name="tolerance" />.</summary> */
        public static bool Near(Color a, Color b, float tolerance)
        {
            return Mathf.Abs(a.r - b.r) <= tolerance && Mathf.Abs(a.g - b.g) <= tolerance
                && Mathf.Abs(a.b - b.b) <= tolerance;
        }

        /** <summary>Whether something drew on the pixel (the stage clears to alpha 0; every pack pass writes alpha 1).</summary> */
        public static bool Covered(Color pixel)
        {
            return pixel.a > 0.5f;
        }

        /** <summary>Whether every channel of every pixel is a finite number (no NaN, no infinity).</summary> */
        public static bool AllFinite(Color[] frame)
        {
            foreach (Color pixel in frame)
            {
                float sum = pixel.r + pixel.g + pixel.b + pixel.a;
                if (float.IsNaN(sum) || float.IsInfinity(sum))
                    return false;
            }
            return true;
        }

        /** <summary>How many pixels of <paramref name="frame" /> pass <paramref name="test" />.</summary> */
        public static int Count(Color[] frame, Func<Color, bool> test)
        {
            int count = 0;
            foreach (Color pixel in frame)
            {
                if (test(pixel))
                    count++;
            }
            return count;
        }

        /** <summary>How many pixels pass <paramref name="test" /> in a run from the left edge of row <paramref name="y" />, starting at the first that does.</summary> */
        public static int RunFromLeft(Color[] frame, int y, Func<Color, bool> test)
        {
            int side = Side(frame);
            int x = 0;
            while (x < side && !test(At(frame, x, y)))
                x++;
            int run = 0;
            while (x + run < side && test(At(frame, x + run, y)))
                run++;
            return run;
        }

        #endregion

        #region Helpers

        private static int Side(Color[] frame)
        {
            return Mathf.RoundToInt(Mathf.Sqrt(frame.Length));
        }

        #endregion
    }
}
