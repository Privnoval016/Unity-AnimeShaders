namespace AnimeShaders.EditorTools
{
    /** <summary>Which way a normal map's green channel points.</summary> */
    public enum NormalConvention
    {
        /** <summary>Green up (Unity, Blender, OpenGL).</summary> */
        OpenGL,

        /** <summary>Green down (DirectX, Unreal): flipped as it is packed.</summary> */
        DirectX,
    }
}
