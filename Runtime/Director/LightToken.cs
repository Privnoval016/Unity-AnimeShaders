using System;

namespace AnimeShaders
{
    /** <summary>One pushed light on <see cref="AnimeLightDirector" />: disposing it removes that light (again, or late: nothing).</summary> */
    public sealed class LightToken : IDisposable
    {
        #region Properties

        /** <summary>The light pushed.</summary> */
        public LightSet Set { get; }

        /** <summary>The level it was pushed at.</summary> */
        public LightLevel Level { get; }

        /** <summary>Its place among pushes at one level: later wins.</summary> */
        internal long Order { get; }

        /** <summary>Whether it has been disposed.</summary> */
        internal bool Released { get; private set; }

        #endregion

        #region Public API

        internal LightToken(LightSet set, LightLevel level, long order)
        {
            Set = set;
            Level = level;
            Order = order;
        }

        /** <summary>Removes the light; a token already removed, or from before a reset, does nothing.</summary> */
        public void Dispose()
        {
            Released = true;
            AnimeLightDirector.Release(this);
        }

        #endregion
    }
}
