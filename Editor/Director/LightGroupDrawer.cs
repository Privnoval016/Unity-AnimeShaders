using UnityEditor;
using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>Draws a light's group (<see cref="LightGroup" />) as a foldout with its Overrides switch first and its
     * values greyed while the switch is off, so a value that would do nothing never looks live.</summary>
     */
    [CustomPropertyDrawer(typeof(LightGroup), true)]
    public sealed class LightGroupDrawer : PropertyDrawer
    {
        #region Private Fields

        private const string OverridesField = "overrides";

        #endregion

        #region Public API

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                bool on = property.FindPropertyRelative(OverridesField).boolValue;
                float y = line.yMax + EditorGUIUtility.standardVerticalSpacing;
                SerializedProperty end = property.GetEndProperty();
                SerializedProperty child = property.Copy();
                bool more = child.NextVisible(true);
                while (more && !SerializedProperty.EqualContents(child, end))
                {
                    float height = EditorGUI.GetPropertyHeight(child, true);
                    using (new EditorGUI.DisabledScope(child.name != OverridesField && !on))
                        EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), child, true);
                    y += height + EditorGUIUtility.standardVerticalSpacing;
                    more = child.NextVisible(false);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndProperty();
        }

        #endregion
    }
}
