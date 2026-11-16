using UnityEngine;

namespace AnimeShaders
{
    /**
     * <summary>A sky and its world as data: the sky's colours, sun, clouds, ridges and stars, the ambient and the haze
     * (<see cref="SkyState" />). <see cref="SkyApply" /> writes it to a scene; a <see cref="SkyPresetApplier" /> picks one.</summary>
     */
    [CreateAssetMenu(menuName = "Anime Shaders/Sky Preset", fileName = "SkyPreset")]
    public sealed class SkyPreset : ScriptableObject
    {
        #region Inspector Fields

        [Tooltip("Everything this preset sets a scene to.")]
        [SerializeField] private SkyState state = SkyState.Default;

        #endregion

        #region Public API

        /** <summary>The preset as the values a scene is set to.</summary> */
        public SkyState Resolve()
        {
            return state;
        }

        #endregion
    }
}
