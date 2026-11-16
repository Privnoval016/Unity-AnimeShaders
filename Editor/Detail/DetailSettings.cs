namespace AnimeShaders.EditorTools
{
    /** <summary>How a detail set is packed: see <see cref="AnimeDetailSet" /> for what each setting does.</summary> */
    public readonly struct DetailSettings
    {
        #region Properties

        /** <summary>The shade's blur radius (a Gaussian's sigma) as a percentage of the colour map's width; 0 none.</summary> */
        public float Soften { get; }

        /** <summary>How strongly occlusion darkens the shade, 0 to 1.</summary> */
        public float OcclusionWeight { get; }

        /** <summary>The packed map's width and height; 0 takes the colour map's size.</summary> */
        public int Size { get; }

        /** <summary>Which way the normal map's green points.</summary> */
        public NormalConvention Normals { get; }

        #endregion

        #region Public API

        /** <summary>Settings as given.</summary> */
        public DetailSettings(float soften, float occlusionWeight, int size, NormalConvention normals)
        {
            Soften = soften;
            OcclusionWeight = occlusionWeight;
            Size = size;
            Normals = normals;
        }

        #endregion
    }
}
