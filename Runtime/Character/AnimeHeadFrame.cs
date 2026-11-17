using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeShaders
{
    /**
     * <summary>Tells a character's face, hair, eye and brow materials where the head is and which way it faces: each
     * frame, after animation and cloth, it measures the head's centre and its forward, right and up (the model's own
     * front and up at rest, carried by the head bone, whatever the bone's or the mesh's axes), and writes them into those
     * renderers' property blocks (<see cref="AnimeBlocks" />) once they change, just before a camera that can see one of
     * the renderers draws: a figure no camera sees pays only the measuring. Those renderers leave the SRP Batcher.</summary>
     * <remarks>The model's front and up are this object's forward and up: put it on the character's root.</remarks>
     */
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder)]
    public sealed class AnimeHeadFrame : MonoBehaviour, IAnimeBlockOwner
    {
        #region Inspector Fields

        [Tooltip("The head bone. Empty: the humanoid avatar's head (an Animator on this object or below).")]
        [SerializeField] private Transform head;

        [Tooltip("A skinned mesh the head bone moves, read once for the head's rest orientation. Empty: the first one under this object skinned to the head.")]
        [SerializeField] private SkinnedMeshRenderer bindMesh;

        [Tooltip("Where the head's centre is from the head bone, in metres along the head's right, up and forward (the gizmo shows it).")]
        [SerializeField] private Vector3 centreOffset = new(0f, 0.08f, 0f);

        [Tooltip("The renderers whose materials read the head (the face, hair, eyes, brows).")]
        [SerializeField] private Renderer[] renderers = Array.Empty<Renderer>();

        #endregion

        #region Private Fields

        private const float Unchanged = 1e-5f;
        private const float GizmoLength = 0.15f;
        private const float GizmoCentre = 0.01f;

        private static readonly List<AnimeHeadFrame> Enabled = new();
        private static readonly Plane[] Frustum = new Plane[6];

        private readonly AnimeBlockValues _values = new();
        private Transform _head;
        private Vector3 _localForward;
        private Vector3 _localUp;
        private bool _ready;
        private bool _stale;
        private bool _retried;
        private bool _pending;
        private int[] _slots = Array.Empty<int>();
        private Vector3 _lastCentre;
        private Vector3 _lastForward;
        private Vector3 _lastUp;

        #endregion

        #region Properties

        /** <summary>Runs after animation, cloth and look-at code, so it reads the head as drawn.</summary> */
        public const int ExecutionOrder = 32000;

        /** <summary>The model's front at rest, in its root's space: a character faces its root's +Z.</summary> */
        public static readonly Vector3 ModelFront = Vector3.forward;

        /** <summary>The model's up at rest, in its root's space.</summary> */
        public static readonly Vector3 ModelUp = Vector3.up;

        /** <summary>Whether it found the head and its rest frame (else the listed materials shade as plain).</summary> */
        public bool Ready => _ready;

        /** <summary>The renderers it writes.</summary> */
        public IReadOnlyList<Renderer> Renderers => renderers;

        /** <summary>The head's centre as last measured, in world space.</summary> */
        public Vector3 Centre { get; private set; }

        /** <summary>The head's forward as last measured.</summary> */
        public Vector3 Forward { get; private set; }

        /** <summary>The head's right as last measured.</summary> */
        public Vector3 Right { get; private set; }

        /** <summary>The head's up as last measured.</summary> */
        public Vector3 Up { get; private set; }

        #endregion

        #region MonoBehaviour Callbacks

        private void OnEnable()
        {
            _retried = false;
            if (!Enabled.Contains(this))
                Enabled.Add(this);
            RenderPipelineManager.beginCameraRendering -= WriteForCamera;
            RenderPipelineManager.beginCameraRendering += WriteForCamera;
            Rebuild();
        }

        private void LateUpdate()
        {
            if (_stale)
            {
                Rebuild();
            }
            else if (!_ready && !_retried)
            {
                _retried = true;
                Rebuild();
                if (!_ready)
                    Debug.LogWarning($"{name}: no head to frame.", this);
            }
            else if (_ready)
            {
                Measure();
            }
        }

        private void Reset()
        {
            var listed = new List<Renderer>();
            foreach (Renderer child in GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in child.sharedMaterials)
                {
                    if (material == null || !ReadsHead(material.shader))
                        continue;
                    listed.Add(child);
                    break;
                }
            }
            renderers = listed.ToArray();
        }

        private void OnDisable()
        {
            Enabled.Remove(this);
            if (Enabled.Count == 0)
                RenderPipelineManager.beginCameraRendering -= WriteForCamera;
            _pending = false;
            AnimeBlocks.Release(this);
            _values.Clear();
        }

        private void OnValidate()
        {
            _stale = true;
        }

        private void OnDrawGizmosSelected()
        {
            if (!_ready)
                return;
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(Centre, Centre + Forward * GizmoLength);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(Centre, Centre + Right * GizmoLength);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(Centre, Centre + Up * GizmoLength);
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(Centre, GizmoCentre);
        }

        #endregion

        #region Public API

        /**
         * <summary>The model's front and up at rest as directions in <paramref name="bone" />'s own space, read from
         * <paramref name="mesh" />'s bind pose for it; false when the mesh is not skinned to that bone.</summary>
         * <param name="model">The character's root: its forward and up are the model's front and up.</param>
         */
        public static bool BindAxes(SkinnedMeshRenderer mesh, Transform bone, Transform model, out Vector3 localForward, out Vector3 localUp)
        {
            localForward = ModelFront;
            localUp = ModelUp;
            if (mesh == null || bone == null || model == null || mesh.sharedMesh == null)
                return false;
            int index = Array.IndexOf(mesh.bones, bone);
            Matrix4x4[] bindposes = mesh.sharedMesh.bindposes;
            if (index < 0 || index >= bindposes.Length)
                return false;
            Matrix4x4 modelToBone = bindposes[index] * MeshFromModel(mesh.transform, model);
            localForward = modelToBone.MultiplyVector(ModelFront).normalized;
            localUp = modelToBone.MultiplyVector(ModelUp).normalized;
            return true;
        }

        /**
         * <summary>Turns directions and points in <paramref name="model" />'s space into <paramref name="mesh" />'s, the
         * space its mesh data is in (an imported model's meshes are often turned from its root, as Blender's are).</summary>
         */
        public static Matrix4x4 MeshFromModel(Transform mesh, Transform model)
        {
            return mesh.worldToLocalMatrix * model.localToWorldMatrix;
        }

        /** <summary>Whether materials on <paramref name="shader" /> read a head frame (<c>Anime/Face</c>, <c>Anime/Hair</c>, <c>Anime/Eyes</c>).</summary> */
        public static bool ReadsHead(Shader shader)
        {
            return shader != null && (shader.name == AnimeShaderIds.FaceShader || shader.name == AnimeShaderIds.HairShader || shader.name == AnimeShaderIds.EyesShader);
        }

        /** <summary>Measures and writes the head's frame now, seen or not: for a body posed outside the player loop, as an offscreen capture's.</summary> */
        public void Refresh()
        {
            if (_stale)
            {
                Rebuild();
                return;
            }
            if (!_ready)
                return;
            Measure();
            Write();
        }

        #endregion

        #region Event Handlers

        private static void WriteForCamera(ScriptableRenderContext context, Camera camera)
        {
            bool framed = false;
            foreach (AnimeHeadFrame frame in Enabled)
            {
                if (!frame._pending)
                    continue;
                if (!framed)
                {
                    GeometryUtility.CalculateFrustumPlanes(camera, Frustum);
                    framed = true;
                }
                if (frame.SeenBy(camera))
                    frame.Write();
            }
        }

        #endregion

        #region Helpers

        /** <summary>Finds the head and its rest frame again and writes the frame at once.</summary> */
        private void Rebuild()
        {
            _stale = false;
            _ready = Resolve();
            _lastForward = Vector3.zero;
            if (!_ready)
                return;
            Measure();
            Write();
        }

        private bool Resolve()
        {
            _head = head;
            if (_head == null)
            {
                Animator animator = GetComponentInChildren<Animator>();
                if (animator != null && animator.isHuman)
                    _head = animator.GetBoneTransform(HumanBodyBones.Head);
            }
            SkinnedMeshRenderer mesh = bindMesh;
            if (mesh == null && _head != null)
            {
                foreach (SkinnedMeshRenderer skinned in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (Array.IndexOf(skinned.bones, _head) < 0)
                        continue;
                    mesh = skinned;
                    break;
                }
            }
            _slots = new int[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                _slots[i] = renderers[i] != null ? renderers[i].sharedMaterials.Length : 0;
            return _head != null && BindAxes(mesh, _head, transform, out _localForward, out _localUp);
        }

        /** <summary>Whether <paramref name="camera" /> can draw one of the renderers: shown, on a layer it draws, in its scene and view.</summary> */
        private bool SeenBy(Camera camera)
        {
            foreach (Renderer listed in renderers)
            {
                if (listed == null || !listed.enabled || listed.forceRenderingOff || !listed.gameObject.activeInHierarchy)
                    continue;
                if ((camera.cullingMask & (1 << listed.gameObject.layer)) == 0)
                    continue;
                if (camera.scene.IsValid() && listed.gameObject.scene != camera.scene)
                    continue;
                if (GeometryUtility.TestPlanesAABB(Frustum, listed.bounds))
                    return true;
            }
            return false;
        }

        private void Measure()
        {
            Vector3 forward = _head.TransformDirection(_localForward);
            Vector3 up = _head.TransformDirection(_localUp);
            Vector3.OrthoNormalize(ref forward, ref up);
            Vector3 right = Vector3.Cross(up, forward);
            Vector3 centre = _head.position + right * centreOffset.x + up * centreOffset.y + forward * centreOffset.z;
            if ((centre - _lastCentre).sqrMagnitude < Unchanged * Unchanged && (forward - _lastForward).sqrMagnitude < Unchanged * Unchanged
                && (up - _lastUp).sqrMagnitude < Unchanged * Unchanged)
                return;
            _lastCentre = centre;
            _lastForward = forward;
            _lastUp = up;
            Centre = centre;
            Forward = forward;
            Right = right;
            Up = up;
            _values.SetVector(AnimeShaderIds.Head.Centre, centre);
            _values.SetVector(AnimeShaderIds.Head.Forward, forward);
            _values.SetVector(AnimeShaderIds.Head.Right, right);
            _values.SetVector(AnimeShaderIds.Head.Up, up);
            _pending = true;
        }

        private void Write()
        {
            if (!_pending)
                return;
            _pending = false;
            int listed = Mathf.Min(renderers.Length, _slots.Length);
            for (int i = 0; i < listed; i++)
            {
                for (int slot = 0; slot < _slots[i]; slot++)
                    AnimeBlocks.Write(this, renderers[i], slot, _values);
            }
        }

        #endregion
    }
}
