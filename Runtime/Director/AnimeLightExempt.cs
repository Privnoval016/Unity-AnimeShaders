using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeShaders
{
    /** <summary>On a camera that keeps its own light (a figure framed on its own, a portrait): it renders with no director light.</summary> */
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
            Resume();
        }

        /** <summary>Between frames nothing renders, so a suspension still held here lost its end (a render that threw): the light comes back.</summary> */
        private void LateUpdate()
        {
            Resume();
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
            if (camera == _camera)
                Resume();
        }

        #endregion

        #region Helpers

        private void Resume()
        {
            if (!_suspended)
                return;
            AnimeLightDirector.EndSuspend();
            _suspended = false;
        }

        #endregion
    }
}
