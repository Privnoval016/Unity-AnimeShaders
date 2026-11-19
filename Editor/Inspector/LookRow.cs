using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>
     * The look row at the top of the material inspector: which look the material follows (a field offering every look that
     * shares a setting with its shader), then its state and Apply, Extract and Update Look.
     * </summary>
     */
    internal static class LookRow
    {
        #region Private Fields

        private const double ConfirmSeconds = 4.0;

        private static AnimeLookPreset _confirming;
        private static double _confirmUntil;

        #endregion

        #region Public API

        /**
         * <summary>
         * Draws the row for <paramref name="materials" />, which follow <paramref name="followed" /> (the first's; null for
         * none), with <paramref name="differing" /> of the first's settings off it. Picking a look makes every one follow it.
         * </summary>
         */
        public static void Draw(Material[] materials, AnimeLookPreset followed, bool gone, bool mixed, int differing)
        {
            Material first = materials[0];
            if (!AnimeLooks.CanFollow(first, out string reason))
            {
                EditorGUILayout.HelpBox(reason, MessageType.None);
                return;
            }

            AnimeLookPreset picked = Field(AnimeLooks.For(first.shader), followed, mixed);
            if (picked != followed)
            {
                int skipped = 0;
                foreach (Material material in materials)
                    skipped += AnimeLooks.Follow(material, picked) ? 0 : 1;
                if (skipped > 0)
                    Debug.LogWarning($"{skipped} of the selected materials can't follow a look.");
                GUIUtility.ExitGUI();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(StateText(followed, gone, mixed, differing), StateStyle(differing));
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(followed == null || differing == 0 && !mixed))
                {
                    if (GUILayout.Button(new GUIContent("Apply", Tip.Apply), EditorStyles.miniButtonLeft, GUILayout.Width(InspectorStyle.ButtonWidth)))
                        AnimeLooks.Apply(materials, followed);
                }
                bool one = materials.Length == 1;
                using (new EditorGUI.DisabledScope(!one))
                {
                    if (GUILayout.Button(new GUIContent("Extract", one ? Tip.Extract : Tip.OneOnly), followed == null ? EditorStyles.miniButtonRight : EditorStyles.miniButtonMid, GUILayout.Width(InspectorStyle.ButtonWidth)))
                    {
                        EditorGUIUtility.PingObject(AnimeLooks.Extract(first));
                        GUIUtility.ExitGUI();
                    }
                    if (followed != null)
                        DrawUpdate(first, followed, one);
                }
            }
        }

        #endregion

        #region Helpers

        private static AnimeLookPreset Field(List<AnimeLookPreset> looks, AnimeLookPreset followed, bool mixed)
        {
            var names = new GUIContent[looks.Count + 1];
            names[0] = new GUIContent("None");
            int current = 0;
            for (int i = 0; i < looks.Count; i++)
            {
                names[i + 1] = new GUIContent(looks[i].name);
                if (looks[i] == followed)
                    current = i + 1;
            }
            EditorGUI.showMixedValue = mixed;
            int chosen = EditorGUILayout.Popup(new GUIContent("Look", Tip.Look), current, names);
            EditorGUI.showMixedValue = false;
            return chosen == 0 ? null : looks[chosen - 1];
        }

        private static string StateText(AnimeLookPreset followed, bool gone, bool mixed, int differing)
        {
            if (gone)
                return "Its look was deleted: pick another, or None.";
            if (mixed)
                return "The selected materials follow different looks.";
            if (followed == null)
                return "Follows no look: pick one, or Extract this one.";
            return differing == 0 ? "As the look." : differing == 1 ? "1 setting differs (marked)." : $"{differing} settings differ (marked).";
        }

        private static GUIStyle StateStyle(int differing)
        {
            var style = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            if (differing > 0)
            {
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = InspectorStyle.Accent;
            }
            return style;
        }

        private static void DrawUpdate(Material material, AnimeLookPreset look, bool one)
        {
            bool confirming = _confirming == look && EditorApplication.timeSinceStartup < _confirmUntil;
            if (!confirming && _confirming == look)
                _confirming = null;
            var label = new GUIContent(confirming ? "Overwrite?" : "Update Look", one ? Tip.Update : Tip.OneOnly);
            if (!GUILayout.Button(label, EditorStyles.miniButtonRight, GUILayout.Width(InspectorStyle.ButtonWidth * 1.5f)))
                return;
            if (!confirming)
            {
                _confirming = look;
                _confirmUntil = EditorApplication.timeSinceStartup + ConfirmSeconds;
                return;
            }
            _confirming = null;
            AnimeLooks.Update(look, material);
        }

        /** <summary>The row's tooltips.</summary> */
        private static class Tip
        {
            public const string Look = "The look this material follows: a setting that differs from it is marked with a dot, and its section bar shows how many. Lists every look sharing a setting with this shader.";
            public const string Apply = "Sets the look's values on the selected materials, so nothing differs (each takes the settings its shader has; textures and the model's own settings are left). Undo takes it back.";
            public const string Extract = "Saves this material's look as a new look asset beside it, which it then follows.";
            public const string Update = "Writes this material's values into the look it follows: every material following that look then takes them on Apply. Click again to confirm; Undo takes it back.";
            public const string OneOnly = "Select one material.";
        }

        #endregion
    }
}
