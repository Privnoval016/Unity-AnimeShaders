using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>A global volume made for a test, placed in the stage camera's own (preview) scene, since URP blends only
     * the volumes a camera renders, at a priority above any other, every parameter overridden, so its values are the
     * ones the camera gets. Destroyed, with its profile, on dispose.</summary>
     */
    public sealed class MadeVolume : IDisposable
    {
        #region Private Fields

        private const float Priority = 100000f;

        private readonly GameObject _holder;
        private readonly VolumeProfile _profile;

        #endregion

        #region Public API

        /** <summary>A global volume in <paramref name="camera" />'s scene, over every other.</summary> */
        public MadeVolume(Camera camera)
        {
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _holder = new GameObject("MadeVolume") { hideFlags = HideFlags.DontSave };
            SceneManager.MoveGameObjectToScene(_holder, camera.scene);
            var volume = _holder.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = Priority;
            volume.sharedProfile = _profile;
        }

        /** <summary>Adds an override of <typeparamref name="T" /> with every parameter overridden, for the test to set.</summary> */
        public T Add<T>() where T : VolumeComponent
        {
            return _profile.Add<T>(true);
        }

        /** <summary>Destroys the volume, its profile and its overrides.</summary> */
        public void Dispose()
        {
            Object.DestroyImmediate(_holder);
            foreach (VolumeComponent component in _profile.components)
                Object.DestroyImmediate(component);
            Object.DestroyImmediate(_profile);
        }

        #endregion
    }
}
