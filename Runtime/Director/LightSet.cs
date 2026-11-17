using UnityEngine;

namespace AnimeShaders
{
    /**
     * <summary>A light for the characters and one for the world, each in groups that set their values only when switched
     * on (an unswitched group leaves what lies beneath): pushed on <see cref="AnimeLightDirector" /> by a sky preset (the
     * base), a <see cref="LightArea" /> or a shot.</summary>
     */
    [CreateAssetMenu(menuName = "Anime Shaders/Light Set", fileName = "LightSet")]
    public sealed class LightSet : ScriptableObject
    {
        #region Inspector Fields

        [Tooltip("How the characters are lit (characters, faces, hair, eyes).")]
        [SerializeField] private CharacterLight characters = new();

        [Tooltip("How the world is lit, on top of the sky preset (ground, buildings, props).")]
        [SerializeField] private WorldLight world = new();

        #endregion

        #region Properties

        /** <summary>The characters' light.</summary> */
        public CharacterLight Characters => characters;

        /** <summary>The world's light.</summary> */
        public WorldLight World => world;

        #endregion

        #region MonoBehaviour Callbacks

        private void OnValidate()
        {
            AnimeLightDirector.Changed(this);
        }

        #endregion
    }
}
