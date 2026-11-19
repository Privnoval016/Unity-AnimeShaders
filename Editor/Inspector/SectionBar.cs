using UnityEditor;
using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /** <summary>A section's bar in the material inspector: its fold arrow and title, how many of its settings differ from the look, and its switch when the section has one.</summary> */
    internal static class SectionBar
    {
        #region Private Fields

        private const float KnobInset = 2f;
        private const float CountWidth = 40f;

        private static GUIStyle _title;
        private static GUIStyle _titleOff;
        private static GUIStyle _count;
        private static bool _stylesDark;

        #endregion

        #region Public API

        /**
         * <summary>
         * Draws a bar and answers whether its section is open (a section switched off may open too, to show its settings
         * greyed). With a switch, a click on its end of the bar flips <paramref name="switchOn" /> (a mixed switch, several
         * materials disagreeing, turns on) and sets <paramref name="switched" />. <paramref name="differing" /> settings that
         * differ from the look are counted on the bar.
         * </summary>
         */
        public static bool Draw(string title, bool open, bool hasSwitch, ref bool switchOn, bool mixed, string switchTooltip, int differing, out bool switched)
        {
            switched = false;
            Styles();
            Rect bar = EditorGUILayout.GetControlRect(false, InspectorStyle.BarHeight);
            bar.xMin -= InspectorStyle.BarReach;
            bar.xMax += InspectorStyle.BarReach;
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(bar, InspectorStyle.Bar);
                EditorGUI.DrawRect(new Rect(bar.x, bar.yMax - 1f, bar.width, 1f), InspectorStyle.Edge);
            }

            float right = 0f;
            if (hasSwitch)
            {
                var target = new Rect(bar.xMax - InspectorStyle.SwitchTarget, bar.y, InspectorStyle.SwitchTarget, bar.height);
                var pill = new Rect(bar.xMax - InspectorStyle.SwitchSize.x - 8f, bar.center.y - InspectorStyle.SwitchSize.y * 0.5f, InspectorStyle.SwitchSize.x, InspectorStyle.SwitchSize.y);
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && target.Contains(Event.current.mousePosition))
                {
                    switchOn = mixed || !switchOn;
                    switched = true;
                    Event.current.Use();
                }
                DrawSwitch(pill, switchOn, mixed);
                GUI.Label(target, new GUIContent(string.Empty, mixed ? "Mixed: the selected materials disagree. Click to turn it on for all." : switchTooltip));
                right = InspectorStyle.SwitchTarget;
            }

            if (differing > 0)
            {
                var count = new Rect(bar.xMax - right - CountWidth, bar.y, CountWidth - 4f, bar.height);
                GUI.Label(count, new GUIContent(differing.ToString(), differing == 1 ? "1 setting here differs from the look." : $"{differing} settings here differ from the look."), _count);
                right += CountWidth;
            }

            bool lit = !hasSwitch || switchOn || mixed;
            var fold = new Rect(bar.x + InspectorStyle.BarReach * 2f, bar.y, bar.width - right - InspectorStyle.BarReach * 2f, bar.height);
            return EditorGUI.Foldout(fold, open, title, true, lit ? _title : _titleOff);
        }

        #endregion

        #region Helpers

        private static void DrawSwitch(Rect pill, bool on, bool mixed)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            Color ground = on && !mixed ? InspectorStyle.Accent : InspectorStyle.SwitchOff;
            GUI.DrawTexture(pill, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, ground, 0f, pill.height * 0.5f);
            float size = pill.height - KnobInset * 2f;
            float x = mixed ? pill.center.x - size * 0.5f : on ? pill.xMax - KnobInset - size : pill.x + KnobInset;
            var knob = new Rect(x, pill.y + KnobInset, size, size);
            GUI.DrawTexture(knob, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, InspectorStyle.Knob, 0f, size * 0.5f);
        }

        /** <summary>The bar's text styles, made again when the Editor's skin changes.</summary> */
        private static void Styles()
        {
            if (_title != null && _stylesDark == EditorGUIUtility.isProSkin)
                return;
            _stylesDark = EditorGUIUtility.isProSkin;
            _title = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
            _titleOff = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Normal };
            _titleOff.normal.textColor = _titleOff.onNormal.textColor = InspectorStyle.Dim;
            _count = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleRight };
            _count.normal.textColor = InspectorStyle.Accent;
        }

        #endregion
    }
}
