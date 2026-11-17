using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeShaders
{
    /** <summary>On a camera that keeps its own light (a menu's figure, a portrait): it renders with no director light.</summary> */
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class AnimeLightExempt : MonoBehaviour
    {
        #region Private Fields

        private Camera _camera;
        private bool _suspended;

        #endregion

        #region MonoBehaviour Callbacks

        private void OnEnable()
        {
            _camera = GetComponent<Camera>();
            RenderPipelineManager.beginCameraRendering += OnBegin;
            RenderPipelineManager.endCameraRendering += OnEnd;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBegin;
            RenderPipelineManager.endCameraRendering -= OnEnd;
            if (_suspended)
                AnimeLightDirector.EndSuspend();
            _suspended = false;
        }

        #endregion

        #region Event Handlers

        private void OnBegin(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _camera || _suspended)
                return;
            AnimeLightDirector.BeginSuspend();
            _suspended = true;
        }

        private void OnEnd(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _camera || !_suspended)
                return;
            AnimeLightDirector.EndSuspend();
            _suspended = false;
        }

        #endregion
    }
}
