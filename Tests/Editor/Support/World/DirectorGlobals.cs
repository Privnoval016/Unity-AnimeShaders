using System;
using UnityEngine;
using Director = AnimeShaders.AnimeShaderIds.Director;

namespace AnimeShaders.Tests
{
    /** <summary>Holds the light director's globals neutral (zero) for a test and puts back what they were on dispose.</summary> */
    public sealed class DirectorGlobals : IDisposable
    {
        #region Private Fields

        private static readonly int[] All =
            { Director.LightDirection, Director.LightColor, Director.ShadowColor, Director.Ambient, Director.Scalars };

        private readonly Vector4[] _before = new Vector4[All.Length];

        #endregion

        #region Public API

        /** <summary>Saves the globals and sets each to zero.</summary> */
        public DirectorGlobals()
        {
            for (int i = 0; i < All.Length; i++)
            {
                _before[i] = Shader.GetGlobalVector(All[i]);
                Shader.SetGlobalVector(All[i], Vector4.zero);
            }
        }

        /** <summary>Puts every global back as it was.</summary> */
        public void Dispose()
        {
            for (int i = 0; i < All.Length; i++)
                Shader.SetGlobalVector(All[i], _before[i]);
        }

        #endregion
    }
}
