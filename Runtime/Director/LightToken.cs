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

        /** <summary>Whether its light is gone: disposed, or dropped by <see cref="AnimeLightDirector.Clear" />. A holder pushes again rather than keep one.</summary> */
        public bool Released { get; private set; }

        #endregion

        #region Public API

        /** <summary>Removes the light; a token already removed, or from before a reset, does nothing.</summary> */
        public void Dispose()
        {
            Released = true;
            AnimeLightDirector.Release(this);
        }

        #endregion

        #region Helpers

        internal LightToken(LightSet set, LightLevel level)
        {
            Set = set;
            Level = level;
        }

        /** <summary>Marks the light gone without asking the director (it is dropping it itself).</summary> */
        internal void Drop()
        {
            Released = true;
        }

        #endregion
    }
}
