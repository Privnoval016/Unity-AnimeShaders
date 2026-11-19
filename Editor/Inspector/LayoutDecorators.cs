using UnityEditor;
using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>
     * <c>[AnimeSection(Name)]</c>: the property starts the inspector section <c>Name</c>. Draws nothing: the inspector
     * reads it from the shader (<see cref="InspectorLayout" />) and draws the section's bar itself. A decorator (its name
     * ends in Decorator), so it sits beside a property's one drawer, such as <c>[Toggle]</c>.
     * </summary>
     */
    public sealed class AnimeSectionDecorator : MaterialPropertyDrawer
    {
        /** <summary>Made by Unity from the shader's attribute.</summary> */
        public AnimeSectionDecorator(string name)
        {
        }

        /** <summary>Takes no room.</summary> */
        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor) => 0f;

        /** <summary>Draws nothing.</summary> */
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
        }
    }

    /**
     * <summary>
     * <c>[AnimeSectionSwitch]</c> on a toggle: it turns its whole section on or off, shown as a switch on the section's
     * bar. <c>[AnimeSectionSwitch(Pass, LightMode)]</c> on any property of a section: the section is switched by that
     * shader pass instead (the line). Draws nothing.
     * </summary>
     */
    public sealed class AnimeSectionSwitchDecorator : MaterialPropertyDrawer
    {
        /** <summary>The toggle it sits on is its section's switch.</summary> */
        public AnimeSectionSwitchDecorator()
        {
        }

        /** <summary>Its section is switched by the pass of <paramref name="lightMode" />.</summary> */
        public AnimeSectionSwitchDecorator(string kind, string lightMode)
        {
        }

        /** <summary>Takes no room.</summary> */
        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor) => 0f;

        /** <summary>Draws nothing.</summary> */
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
        }
    }

    /**
     * <summary>
     * <c>[AnimeShowWith(_Toggle)]</c>: the property is shown only while that toggle is on (and every toggle above it, so
     * settings nest). Draws nothing.
     * </summary>
     */
    public sealed class AnimeShowWithDecorator : MaterialPropertyDrawer
    {
        /** <summary>Made by Unity from the shader's attribute.</summary> */
        public AnimeShowWithDecorator(string toggle)
        {
        }

        /** <summary>Takes no room.</summary> */
        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor) => 0f;

        /** <summary>Draws nothing.</summary> */
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
        }
    }

    /**
     * <summary>
     * <c>[AnimeOwn]</c>: the property belongs to the material's model (its base colour, cull, line offset), never to a
     * look, so a look preset never stores or sets it. Draws nothing.
     * </summary>
     */
    public sealed class AnimeOwnDecorator : MaterialPropertyDrawer
    {
        /** <summary>Takes no room.</summary> */
        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor) => 0f;

        /** <summary>Draws nothing.</summary> */
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
        }
    }
}
