using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>A detail set's Inspector: its fields, the packed map and the stone map with its stone count, whether
     * they are up to date, what they will cost in video memory, and a Pack button.</summary>
     */
    [CustomEditor(typeof(AnimeDetailSet))]
    public class AnimeDetailSetEditor : Editor
    {
        #region Private Fields

        private const float BytesPerTexel = 1f;
        private const float MipFactor = 4f / 3f;
        private const float Megabyte = 1024f * 1024f;
        private const float StoneBytesPerTexel = 4f;
        private const int ManyStones = 400;

        private string _hash;

        #endregion

        #region MonoBehaviour Callbacks

        private void OnEnable()
        {
            Refresh();
        }

        #endregion

        #region Public API

        /** <summary>Draws the set's fields, then the packed map's state, its cost and the button that packs it.</summary> */
        public override void OnInspectorGUI()
        {
            var set = (AnimeDetailSet)target;
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
                Refresh();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Packed Map", set.Packed, typeof(Texture2D), false);

            int size = PackedSize(set);
            if (size > AnimeDetailSet.RecommendedSize)
                EditorGUILayout.HelpBox($"{size} square: more than the {AnimeDetailSet.RecommendedSize} suggested; slower to pack and heavier in memory.", MessageType.Warning);
            if (size > 0)
                EditorGUILayout.LabelField("Video memory", $"About {size * (float)size * BytesPerTexel * MipFactor / Megabyte:0.#} MB (compressed, with mips)");

            if (set.MakesStones)
                DrawStones(set);

            bool missing = set.Colour == null || set.Normal == null;
            if (missing)
                EditorGUILayout.HelpBox("Needs a colour and a normal map.", MessageType.Info);
            else if (set.Packed == null)
                EditorGUILayout.HelpBox("Not packed yet.", MessageType.Info);
            else if (set.PackedHash != _hash)
                EditorGUILayout.HelpBox("Out of date: the sources or settings changed since it was packed.", MessageType.Warning);

            using (new EditorGUI.DisabledScope(missing))
            {
                if (GUILayout.Button("Pack"))
                {
                    PackLogged(set);
                    Refresh();
                }
            }
        }

        #endregion

        #region Helpers

        private void Refresh()
        {
            _hash = target == null ? null : AnimeDetailPacker.HashOf((AnimeDetailSet)target);
        }

        private void DrawStones(AnimeDetailSet set)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Stone Map", set.StoneMap, typeof(Texture2D), false);
            EditorGUILayout.LabelField("Stone map memory", $"About {StoneMath.MapSize * (float)StoneMath.MapSize * StoneBytesPerTexel / Megabyte:0.#} MB (uncompressed, no mips)");
            if (set.Occlusion == null)
                EditorGUILayout.HelpBox("Stones need an occlusion map that is darker in the joints.", MessageType.Warning);
            else if (set.StoneMap == null && set.Packed != null && set.PackedHash == _hash)
                EditorGUILayout.HelpBox("No stones found: lower the threshold, or the occlusion may not separate stone from joint.", MessageType.Warning);
            else if (set.StoneMap != null)
                EditorGUILayout.LabelField("Stones found", set.StoneCount.ToString());
            if (set.StoneMap != null && set.StoneCount > ManyStones)
                EditorGUILayout.HelpBox($"Over {ManyStones} stones: pitting may be splitting them; raise the blur or lower the threshold.", MessageType.Warning);
        }

        private static int PackedSize(AnimeDetailSet set)
        {
            int asked = set.Settings.Size;
            return asked > 0 ? asked : set.Colour != null ? set.Colour.width : 0;
        }

        private static void PackLogged(AnimeDetailSet set)
        {
            try
            {
                AnimeDetailPacker.Pack(set);
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError(exception.Message, set);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError(exception.Message, set);
            }
            catch (IOException exception)
            {
                Debug.LogError(exception.Message, set);
            }
        }

        #endregion
    }
}
