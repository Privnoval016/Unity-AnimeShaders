using UnityEditor;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>
     * The material inspector's lit ball: a sphere drawn with a copy of the material whose base map is white, so it shows
     * the tones, the second band, the fresnel rim and the line rather than the texture (a character's atlas wrapped on a
     * sphere reads as noise). One preview kept for every inspector, let go before a reload.
     * </summary>
     */
    internal static class MaterialPreview
    {
        #region Private Fields

        private static readonly Vector3 CameraAt = new(0f, 0f, -3.2f);
        private static readonly Quaternion LightTurn = Quaternion.Euler(20f, 125f, 0f);
        private const float FieldOfView = 21f;

        private static PreviewRenderUtility _stage;
        private static Material _copy;
        private static Mesh _sphere;

        #endregion

        #region Public API

        /** <summary>Draws <paramref name="material" /> on the ball in <paramref name="area" /> (on repaint only).</summary> */
        public static void Draw(Rect area, Material material)
        {
            if (Event.current.type != EventType.Repaint || area.width < 2f)
                return;
            Ready();
            if (_copy.shader != material.shader)
                _copy.shader = material.shader;
            _copy.CopyPropertiesFromMaterial(material);
            if (_copy.HasTexture(Ids.BaseMap))
                _copy.SetTexture(Ids.BaseMap, Texture2D.whiteTexture);
            foreach (InspectorLayout.Section section in InspectorLayout.For(material.shader).Sections)
            {
                if (section.SwitchPass != null)
                    _copy.SetShaderPassEnabled(section.SwitchPass, material.GetShaderPassEnabled(section.SwitchPass));
            }

            _stage.BeginPreview(area, GUIStyle.none);
            _stage.camera.transform.SetPositionAndRotation(CameraAt, Quaternion.identity);
            _stage.camera.fieldOfView = FieldOfView;
            _stage.camera.nearClipPlane = 0.1f;
            _stage.camera.farClipPlane = 20f;
            _stage.camera.clearFlags = CameraClearFlags.SolidColor;
            _stage.camera.backgroundColor = InspectorStyle.Card;
            _stage.lights[0].transform.rotation = LightTurn;
            _stage.lights[0].intensity = 1f;
            _stage.lights[0].color = Color.white;
            _stage.lights[1].intensity = 0f;
            _stage.DrawMesh(_sphere, Matrix4x4.identity, _copy, 0);
            _stage.camera.Render();
            GUI.DrawTexture(area, _stage.EndPreview(), ScaleMode.StretchToFill, false);
        }

        #endregion

        #region Helpers

        private static void Ready()
        {
            if (_stage != null && _copy != null)
                return;
            LetGo();
            _stage = new PreviewRenderUtility();
            _copy = new Material(Shader.Find("Hidden/InternalErrorShader")) { hideFlags = HideFlags.HideAndDontSave };
            _sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            AssemblyReloadEvents.beforeAssemblyReload += LetGo;
            EditorApplication.quitting += LetGo;
        }

        private static void LetGo()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= LetGo;
            EditorApplication.quitting -= LetGo;
            _stage?.Cleanup();
            _stage = null;
            if (_copy != null)
                Object.DestroyImmediate(_copy);
            _copy = null;
        }

        #endregion
    }
}
