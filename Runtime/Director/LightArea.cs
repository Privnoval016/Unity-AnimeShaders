using UnityEngine;

namespace AnimeShaders
{
    /**
     * <summary>A place with its own light (an interior): while what it tracks is inside its box it pushes its
     * <see cref="LightSet" /> at <see cref="LightLevel.Area" />, and removes it once that has gone past the box's edge by
     * the margin (so a camera moving along the edge never flickers it) or as it is switched off. It follows its target in Play
     * Mode.</summary>
     */
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class LightArea : MonoBehaviour
    {
        #region Inspector Fields

        [Tooltip("The light inside.")]
        [SerializeField] private LightSet lightSet;

        [Tooltip("What decides being inside: a target transform (a player, a camera rig). Empty: the main camera.")]
        [SerializeField] private Transform tracked;

        [Tooltip("The box, in this object's space, centred on it.")]
        [SerializeField] private Vector3 size = new(10f, 4f, 10f);

        [Tooltip("Metres past the box's edge before the light leaves, once inside (whatever the box's scale).")]
        [SerializeField, Min(0f)] private float edgeMargin = 0.5f;

        #endregion

        #region Private Fields

        /** <summary>The smallest scale an axis is taken to have, so a flattened box never divides by zero.</summary> */
        private const float MinScale = 1e-4f;

        private LightToken _token;

        #endregion

        #region Properties

        /** <summary>Whether its light is pushed.</summary> */
        public bool Lit => _token != null && !_token.Released;

        #endregion

        #region MonoBehaviour Callbacks

        private void Update()
        {
            if (!Application.isPlaying)
                return;
            Transform target = tracked;
            if (target == null)
            {
                Camera main = Camera.main;
                target = main != null ? main.transform : null;
            }
            Step(target != null ? target.position : (Vector3?)null);
        }

        private void OnDisable()
        {
            _token?.Dispose();
            _token = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.85f, 0.4f);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }

        #endregion

        #region Public API

        /** <summary>Pushes or removes its light for what it tracks being at <paramref name="position" /> (none: removes).</summary> */
        public void Step(Vector3? position)
        {
            bool inside = position != null && Contains(position.Value, Lit ? edgeMargin : 0f);
            if (inside && !Lit && lightSet != null)
                _token = AnimeLightDirector.Push(lightSet, LightLevel.Area);
            else if (!inside && _token != null)
            {
                _token.Dispose();
                _token = null;
            }
        }

        #endregion

        #region Helpers

        private bool Contains(Vector3 position, float margin)
        {
            Vector3 local = transform.InverseTransformPoint(position);
            Vector3 scale = transform.lossyScale;
            Vector3 half = size * 0.5f + new Vector3(margin / Mathf.Max(Mathf.Abs(scale.x), MinScale), margin / Mathf.Max(Mathf.Abs(scale.y), MinScale),
                margin / Mathf.Max(Mathf.Abs(scale.z), MinScale));
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        #endregion
    }
}
