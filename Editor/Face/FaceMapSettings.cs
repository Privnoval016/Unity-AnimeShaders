namespace AnimeShaders.EditorTools
{
    /** <summary>How a face map is baked (<see cref="FaceMapMath" />).</summary> */
    public readonly struct FaceMapSettings
    {
        #region Properties

        /** <summary>The map's width and height in texels.</summary> */
        public int Size { get; }

        /** <summary>The smoothing's radius (a Gaussian's sigma) as a percentage of the map's width.</summary> */
        public float Smoothing { get; }

        /** <summary>How far each normal leans toward the face's sphere, 0 to 1.</summary> */
        public float SphereBlend { get; }

        /** <summary>How many texels each island is padded by, so filtering never reaches empty texels.</summary> */
        public int Gutter { get; }

        #endregion

        #region Public API

        /** <summary>Settings as given.</summary> */
        public FaceMapSettings(int size, float smoothing, float sphereBlend, int gutter)
        {
            Size = size;
            Smoothing = smoothing;
            SphereBlend = sphereBlend;
            Gutter = gutter;
        }

        #endregion
    }
}
