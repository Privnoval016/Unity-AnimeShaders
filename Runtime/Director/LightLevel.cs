namespace AnimeShaders
{
    /** <summary>Which light wins where several set the same group: a later level over an earlier; at one level, the later push.</summary> */
    public enum LightLevel
    {
        /** <summary>A scene's own light (the sky preset's).</summary> */
        Base = 0,

        /** <summary>A place's light, while it is inside a <see cref="LightArea" /> (an interior).</summary> */
        Area = 1,

        /** <summary>A shot's light, for the length of the shot.</summary> */
        Shot = 2,
    }
}
