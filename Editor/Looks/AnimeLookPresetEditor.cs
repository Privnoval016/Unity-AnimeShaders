using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>A look's inspector: the shader it was taken from, a tick per section it sets, how many settings each
     * holds, and a warning for a setting its shader no longer has. Its values are taken from a material (Extract, Update in
     * the material inspector), never typed here.</summary>
     */
    [CustomEditor(typeof(AnimeLookPreset))]
    public sealed class AnimeLookPresetEditor : Editor
    {
        #region Public API

        /** <summary>Draws the shader, the section ticks and any stale setting.</summary> */
        public override void OnInspectorGUI()
        {
            var look = (AnimeLookPreset)target;
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField(new GUIContent("Taken From", "The shader it was taken from. It applies to any pack material: each takes the settings its shader has."), look.MadeFrom, typeof(Shader), false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(new GUIContent("Sets", "Untick a section to leave it as each material has it (a line-only look)."), EditorStyles.boldLabel);
            foreach (KeyValuePair<string, int> section in SectionCounts(look))
            {
                bool covered = look.Sets(section.Key);
                bool tick = EditorGUILayout.ToggleLeft($"{section.Key}  ({section.Value})", covered);
                if (tick == covered)
                    continue;
                Undo.RecordObject(look, $"{(tick ? "Set" : "Leave")} {section.Key}");
                look.Cover(section.Key, tick);
                EditorUtility.SetDirty(look);
            }

            List<string> stale = Stale(look);
            if (stale.Count > 0)
                EditorGUILayout.HelpBox($"Holds what its shader no longer has: {string.Join(", ", stale)}. A setting its shader lacks is skipped when it applies; Update it from a material to bring it up to date.", MessageType.Warning);
        }

        #endregion

        #region Helpers

        /** <summary>Each section the look holds, in its order, with how many settings and switches.</summary> */
        private static Dictionary<string, int> SectionCounts(AnimeLookPreset look)
        {
            var counts = new Dictionary<string, int>();
            foreach (AnimeLookPreset.Setting setting in look.Settings)
                counts[setting.section] = counts.TryGetValue(setting.section, out int n) ? n + 1 : 1;
            foreach (AnimeLookPreset.PassSwitch pass in look.Passes)
                counts[pass.section] = counts.TryGetValue(pass.section, out int n) ? n + 1 : 1;
            return counts;
        }

        private static List<string> Stale(AnimeLookPreset look)
        {
            var stale = new List<string>();
            if (look.MadeFrom == null)
                return stale;
            var sections = new HashSet<string>();
            foreach (InspectorLayout.Section section in InspectorLayout.For(look.MadeFrom).Sections)
                sections.Add(section.Name);
            foreach (AnimeLookPreset.Setting setting in look.Settings)
            {
                if (look.MadeFrom.FindPropertyIndex(setting.property) < 0)
                    stale.Add(setting.property);
                else if (!sections.Contains(setting.section) && !stale.Contains($"the section {setting.section}"))
                    stale.Add($"the section {setting.section}");
            }
            return stale;
        }

        #endregion
    }
}
