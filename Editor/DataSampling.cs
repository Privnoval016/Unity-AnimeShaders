namespace AnimeShaders.EditorTools
{
    /** <summary>How a data texture is sampled (<see cref="DataTextures.HoldTo" />).</summary> */
    public enum DataSampling
    {
        /** <summary>Mipmapped, trilinear and anisotropic, repeating: a map tiled over a surface at every distance.</summary> */
        Blended,

        /** <summary>Point-sampled without mips, repeating: values that must never blend (ids, codes).</summary> */
        Exact,

        /** <summary>Bilinear without mips, clamped: a map laid once over a mesh's UVs, whose values blend but never wrap.</summary> */
        Clamped,
    }
}
