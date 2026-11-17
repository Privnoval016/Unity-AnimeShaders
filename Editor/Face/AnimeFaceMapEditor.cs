using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>A face map's Inspector: its fields, the baked map and its video memory, whether it is up to date, what
     * stops the mesh baking, what the last bake found (too few texels across the face, overlapping or mirrored UVs, a
     * face not facing the model's front), the import rule's label, and a Bake button.</summary>
     */
    [CustomEditor(typeof(AnimeFaceMap))]
    public sealed class AnimeFaceMapEditor : Editor
    {
        #region Private Fields

        private const float BytesPerTexel = 8f;
        private const float Megabyte = 1024f * 1024f;

        private string _hash;
        private string _problem;

        #endregion

        #region Properties

        /** <summary>Fewer texels across the face than this and its shadow line shows steps up close.</summary> */
        public const int FewTexels = 256;

        /** <summary>More of the face than this drawn twice: its UVs overlap or mirror.</summary> */
        public const float MostOverlap = 0.01f;

        /** <summary>A face whose mean normal leans less than this toward the model's front doesn't face it.</summary> */
        public const float LeastFacing = 0.5f;

        #endregion

        #region MonoBehaviour Callbacks

        private void OnEnable()
        {
            Refresh();
        }

        #endregion

        #region Public API

        /** <summary>Draws the asset's fields, then the map's state, what the bake found and the button that bakes it.</summary> */
        public override void OnInspectorGUI()
        {
            var faceMap = (AnimeFaceMap)target;
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
                Refresh();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Map", faceMap.Map, typeof(Texture2D), false);
            int size = faceMap.Settings.Size;
            EditorGUILayout.LabelField("Video memory", $"About {size * (float)size * BytesPerTexel / Megabyte:0.#} MB (half float, no mips)");

            if (_problem != null)
                EditorGUILayout.HelpBox(_problem, MessageType.Warning);
            else if (faceMap.Map == null)
                EditorGUILayout.HelpBox("Not baked yet.", MessageType.Info);
            else if (faceMap.BakedHash != _hash)
                EditorGUILayout.HelpBox("Out of date: the model or settings changed since it was baked.", MessageType.Warning);
            if (faceMap.Map != null)
                DrawFound(faceMap);

            using (new EditorGUI.DisabledScope(_problem != null))
            {
                if (GUILayout.Button("Bake"))
                {
                    BakeLogged(faceMap);
                    Refresh();
                }
            }
        }

        #endregion

        #region Helpers

        private void Refresh()
        {
            var faceMap = target as AnimeFaceMap;
            _hash = faceMap == null ? null : AnimeFaceMapBaker.HashOf(faceMap);
            _problem = faceMap == null ? null : AnimeFaceMapBaker.Problem(faceMap.Mesh);
        }

        private static void DrawFound(AnimeFaceMap faceMap)
        {
            EditorGUILayout.LabelField("Texels across the face", faceMap.TexelsAcross.ToString());
            if (faceMap.TexelsAcross < FewTexels)
                EditorGUILayout.HelpBox($"Under {FewTexels} texels across: raise the size, or give the face more of its texture.", MessageType.Warning);
            if (faceMap.Overlap > MostOverlap)
                EditorGUILayout.HelpBox($"{faceMap.Overlap:P0} of the face's texels are drawn twice: overlapping or mirrored UVs. Give each half of the face its own UVs.", MessageType.Warning);
            if (faceMap.MeanNormal.z < LeastFacing)
                EditorGUILayout.HelpBox("The face doesn't face the model's front (its root's +Z): the shadow will turn the wrong way.", MessageType.Warning);
            string path = AssetDatabase.GetAssetPath(faceMap.Map);
            if (DataTextures.HasLabel(path, AnimeFaceMapImport.Label))
                return;
            EditorGUILayout.HelpBox("The map has lost its label, so its import settings aren't held.", MessageType.Warning);
            if (GUILayout.Button("Give It Back"))
                DataTextures.AddLabel(path, AnimeFaceMapImport.Label);
        }

        private static void BakeLogged(AnimeFaceMap faceMap)
        {
            try
            {
                AnimeFaceMapBaker.Bake(faceMap);
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError(exception.Message, faceMap);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError(exception.Message, faceMap);
            }
            catch (IOException exception)
            {
                Debug.LogError(exception.Message, faceMap);
            }
        }

        #endregion
    }
}
