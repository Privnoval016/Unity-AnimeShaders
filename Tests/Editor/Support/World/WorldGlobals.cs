using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using World = AnimeShaders.AnimeShaderIds.World;

namespace AnimeShaders.Tests
{
    /**
     * <summary>Holds the world's globals (the cloud field, the haze and their keywords, published by a scene's sky
     * preset) neutral for a test, whatever scene the Editor has open, and puts back what they were on dispose.</summary>
     */
    public sealed class WorldGlobals : IDisposable
    {
        #region Private Fields

        private static readonly IReadOnlyList<int> Vectors = SkyApply.Globals;
        private static readonly IReadOnlyList<string> Keywords = SkyApply.Keywords;

        private readonly Vector4[] _vectors = new Vector4[Vectors.Count];
        private readonly bool[] _keywords = new bool[Keywords.Count];
        private readonly Texture _noise;

        #endregion

        #region Public API

        /** <summary>Saves the globals and clears them: no clouds, no haze.</summary> */
        public WorldGlobals()
        {
            for (int i = 0; i < Vectors.Count; i++)
                _vectors[i] = Shader.GetGlobalVector(Vectors[i]);
            for (int i = 0; i < Keywords.Count; i++)
                _keywords[i] = Shader.IsKeywordEnabled(GlobalKeyword.Create(Keywords[i]));
            _noise = Shader.GetGlobalTexture(World.AnimeCloudNoise);
            SkyApply.Unpublish();
        }

        /** <summary>Puts every global back as it was.</summary> */
        public void Dispose()
        {
            for (int i = 0; i < Vectors.Count; i++)
                Shader.SetGlobalVector(Vectors[i], _vectors[i]);
            for (int i = 0; i < Keywords.Count; i++)
                Shader.SetKeyword(GlobalKeyword.Create(Keywords[i]), _keywords[i]);
            Shader.SetGlobalTexture(World.AnimeCloudNoise, _noise);
        }

        #endregion
    }
}
