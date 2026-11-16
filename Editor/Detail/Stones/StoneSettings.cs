namespace AnimeShaders.EditorTools
{
    /** <summary>How a detail set finds its stones: see <see cref="AnimeDetailSet" /> for what each setting does.</summary> */
    public readonly struct StoneSettings
    {
        #region Properties

        /** <summary>The occlusion's blur radius (a Gaussian's sigma) as a percentage of the tile's width.</summary> */
        public float Blur { get; }

        /** <summary>The blurred occlusion above which a texel is stone, 0 to 1.</summary> */
        public float Threshold { get; }

        /** <summary>The smallest stone kept, as a percentage of the tile's area.</summary> */
        public float MinSize { get; }

        #endregion

        #region Public API

        /** <summary>Settings as given.</summary> */
        public StoneSettings(float blur, float threshold, float minSize)
        {
            Blur = blur;
            Threshold = threshold;
            MinSize = minSize;
        }

        #endregion
    }
}
