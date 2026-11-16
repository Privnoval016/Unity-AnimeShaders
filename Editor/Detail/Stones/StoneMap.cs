namespace AnimeShaders.EditorTools
{
    /** <summary>A stone map found by <see cref="StoneMath.Find" />: the image, how many stones it holds, and how many were dropped.</summary> */
    public sealed class StoneMap
    {
        #region Properties

        /** <summary>The map: R the stone's value (1 to 255; 0 none), G its home-tile code (0 none; see <see cref="StoneMath" />).</summary> */
        public DetailImage Image { get; }

        /** <summary>The stones kept.</summary> */
        public int Count { get; }

        /** <summary>The stones dropped for wrapping onto themselves or spanning the whole tile.</summary> */
        public int Dropped { get; }

        #endregion

        #region Public API

        /** <summary>A found map.</summary> */
        public StoneMap(DetailImage image, int count, int dropped)
        {
            Image = image;
            Count = count;
            Dropped = dropped;
        }

        #endregion
    }
}
