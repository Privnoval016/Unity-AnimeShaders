namespace AnimeShaders.EditorTools
{
    /** <summary>A detail set's maps as images: the colour and the normal always, the occlusion and the mask when it has them.</summary> */
    public readonly struct DetailSources
    {
        #region Properties

        /** <summary>The colour map: its brightness becomes the shade.</summary> */
        public DetailImage Colour { get; }

        /** <summary>The ambient occlusion map (read from red), or null for none.</summary> */
        public DetailImage Occlusion { get; }

        /** <summary>The normal map.</summary> */
        public DetailImage Normal { get; }

        /** <summary>The mask (read from red), or null: the mask is then 1.</summary> */
        public DetailImage Mask { get; }

        #endregion

        #region Public API

        /** <summary>Sources as given.</summary> */
        public DetailSources(DetailImage colour, DetailImage occlusion, DetailImage normal, DetailImage mask)
        {
            Colour = colour;
            Occlusion = occlusion;
            Normal = normal;
            Mask = mask;
        }

        #endregion
    }
}
