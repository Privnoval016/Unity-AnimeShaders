using UnityEditor;
using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>A head frame's Inspector: its fields, then a warning for each reason its materials would shade as plain
     * (no head found, nothing listed, a listed renderer with no face, hair or eye material).</summary>
     */
    [CustomEditor(typeof(AnimeHeadFrame))]
    public sealed class AnimeHeadFrameEditor : Editor
    {
        #region Public API

        /** <summary>Draws the fields and the warnings.</summary> */
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var frame = (AnimeHeadFrame)target;
            if (frame.isActiveAndEnabled && !frame.Ready)
                EditorGUILayout.HelpBox("No head found: set Head, or put this on a humanoid whose skinned mesh is skinned to its head.", MessageType.Warning);
            if (frame.Renderers.Count == 0)
                EditorGUILayout.HelpBox("No renderers listed: list the face, hair, eyes and brows (Reset fills them from the children).", MessageType.Warning);
            foreach (Renderer listed in frame.Renderers)
            {
                if (listed != null && !ReadsHead(listed))
                    EditorGUILayout.HelpBox($"{listed.name} has no Anime/Face, Anime/Hair or Anime/Eyes material: it reads no head frame.", MessageType.Info);
            }
        }

        #endregion

        #region Helpers

        private static bool ReadsHead(Renderer renderer)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material != null && AnimeHeadFrame.ReadsHead(material.shader))
                    return true;
            }
            return false;
        }

        #endregion
    }
}
