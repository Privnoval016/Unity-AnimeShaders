using UnityEngine;

namespace AnimeShaders
{
    /**
     * <summary>A sky and its world as data: the sky's colours, sun, clouds, ridges and stars, the ambient and the haze
     * (<see cref="SkyState" />), and the scene's base light (a <see cref="LightSet" />, the characters' and the world's on top
     * of the sky). <see cref="SkyApply" /> writes it to a scene; a <see cref="SkyPresetApplier" /> picks one.</summary>
     */
    [CreateAssetMenu(menuName = "Anime Shaders/Sky Preset", fileName = "SkyPreset")]
    public sealed class SkyPreset : ScriptableObject
    {
        #region Inspector Fields

        [Tooltip("Everything this preset sets a scene to.")]
        [SerializeField] private SkyState state = SkyState.Default;

        [Tooltip("The scene's base light under this sky: the characters' and the world's (a light area or a shot can rise over it). None: no director light.")]
        [SerializeField] private LightSet light;

        #endregion

        #region Properties

        /** <summary>The base light under this sky, or null.</summary> */
        public LightSet Light => light;

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
