using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeShaders
{
    /**
     * <summary>
     * A look for pack materials: the shader it was taken from, the inspector sections it covers, and the values of those
     * sections' settings (floats, colours, vectors) and section switches (a pass, the line). Never a texture, the render
     * queue, or a setting the shader marks as the material's own (<c>[AnimeOwn]</c>: base colour, cull, line offset).
     * Applied to any pack material, each takes the values its shader has (<see cref="AnimeMaterials.ApplyLook" />).
     * </summary>
     */
    [CreateAssetMenu(menuName = "Anime Shaders/Look Preset", fileName = "LookPreset")]
    public sealed class AnimeLookPreset : ScriptableObject
    {
        /** <summary>What kind of value a setting holds.</summary> */
        public enum ValueKind
        {
            Float = 0,
            Color = 1,
            Vector = 2
        }

        /** <summary>One setting's value and the section it belongs to.</summary> */
        [Serializable]
        public struct Setting
        {
            [Tooltip("The shader property's name.")]
            public string property;
            [Tooltip("The inspector section it belongs to.")]
            public string section;
            [Tooltip("What kind of value it holds.")]
            public ValueKind kind;
            [Tooltip("Its value, for a float (a toggle is 0 or 1).")]
            public float number;
            [Tooltip("Its value, for a colour or a vector.")]
            public Vector4 vector;

            /** <summary>Whether <paramref name="shader" /> has this setting with the same kind of value.</summary> */
            public bool Fits(Shader shader, out int index)
            {
                index = shader.FindPropertyIndex(property);
                return index >= 0 && KindOf(shader.GetPropertyType(index), out ValueKind found) && found == kind;
            }

            /** <summary>Its value as <paramref name="shader" />'s property <paramref name="index" /> takes it: a range held to its limits.</summary> */
            public Vector4 For(Shader shader, int index)
            {
                if (kind != ValueKind.Float)
                    return vector;
                float value = number;
                if (shader.GetPropertyType(index) == ShaderPropertyType.Range)
                {
                    Vector2 limits = shader.GetPropertyRangeLimits(index);
                    value = Mathf.Clamp(value, limits.x, limits.y);
                }
                return new Vector4(value, 0f, 0f, 0f);
            }

            /** <summary>Sets it on <paramref name="material" /> (whose shader it fits, at <paramref name="index" />).</summary> */
            public void ApplyTo(Material material, int index)
            {
                Vector4 value = For(material.shader, index);
                if (kind == ValueKind.Float)
                    material.SetFloat(property, value.x);
                else if (kind == ValueKind.Color)
                    material.SetColor(property, value);
                else
                    material.SetVector(property, value);
            }

            /** <summary>Whether <paramref name="material" />'s value differs from it (as applied there), by more than <paramref name="tolerance" /> in any part.</summary> */
            public bool Differs(Material material, int index, float tolerance)
            {
                Vector4 want = For(material.shader, index);
                Vector4 has = Read(material, property, kind);
                for (int i = 0; i < (kind == ValueKind.Float ? 1 : 4); i++)
                {
                    if (Mathf.Abs(has[i] - want[i]) > tolerance)
                        return true;
                }
                return false;
            }

            /** <summary>A setting holding <paramref name="material" />'s value of <paramref name="property" />, in <paramref name="section" />; false for a kind a look can't hold (a texture).</summary> */
            public static bool Take(Material material, int index, string section, out Setting setting)
            {
                Shader shader = material.shader;
                setting = default;
                if (!KindOf(shader.GetPropertyType(index), out ValueKind kind))
                    return false;
                string name = shader.GetPropertyName(index);
                Vector4 value = Read(material, name, kind);
                setting = new Setting { property = name, section = section, kind = kind, number = value.x, vector = value };
                return true;
            }

            /** <summary>The kind of value a property of <paramref name="type" /> holds; false for one a look can't hold.</summary> */
            public static bool KindOf(ShaderPropertyType type, out ValueKind kind)
            {
                kind = type switch
                {
                    ShaderPropertyType.Color => ValueKind.Color,
                    ShaderPropertyType.Vector => ValueKind.Vector,
                    _ => ValueKind.Float
                };
                return type is ShaderPropertyType.Float or ShaderPropertyType.Range or ShaderPropertyType.Color or ShaderPropertyType.Vector;
            }

            private static Vector4 Read(Material material, string name, ValueKind kind) => kind switch
            {
                ValueKind.Float => new Vector4(material.GetFloat(name), 0f, 0f, 0f),
                ValueKind.Color => material.GetColor(name),
                _ => material.GetVector(name)
            };
        }

        /** <summary>A section switched by a shader pass (the line), and whether it is on.</summary> */
        [Serializable]
        public struct PassSwitch
        {
            [Tooltip("The pass's light mode.")]
            public string lightMode;
            [Tooltip("The inspector section it switches.")]
            public string section;
            [Tooltip("Whether the pass draws.")]
            public bool on;
        }

        #region Inspector Fields

        [Tooltip("The shader it was taken from. It applies to any pack material: each takes the settings its shader has.")]
        [SerializeField] private Shader madeFrom;
        [Tooltip("The sections it sets; untick one to leave that section as each material has it (a line-only look).")]
        [SerializeField] private List<string> covers = new();
        [Tooltip("The settings it holds, taken by Extract or Update in a material's inspector.")]
        [SerializeField] private List<Setting> settings = new();
        [Tooltip("The sections switched by a pass, and whether each is on.")]
        [SerializeField] private List<PassSwitch> passes = new();

        #endregion

        #region Properties

        /** <summary>The shader it was taken from.</summary> */
        public Shader MadeFrom => madeFrom;

        /** <summary>The sections it sets (ticked).</summary> */
        public IReadOnlyList<string> Covers => covers;

        /** <summary>Every setting it holds, covered or not.</summary> */
        public IReadOnlyList<Setting> Settings => settings;

        /** <summary>Every pass switch it holds, covered or not.</summary> */
        public IReadOnlyList<PassSwitch> Passes => passes;

        #endregion

        #region Public API

        /** <summary>Whether it sets <paramref name="section" />.</summary> */
        public bool Sets(string section) => covers.Contains(section);

        /** <summary>Replaces what it holds: the shader, every section covered, and these settings and switches.</summary> */
        internal void Hold(Shader shader, IEnumerable<Setting> taken, IEnumerable<PassSwitch> switches)
        {
            madeFrom = shader;
            settings = new List<Setting>(taken);
            passes = new List<PassSwitch>(switches);
            covers = new List<string>();
            foreach (Setting setting in settings)
            {
                if (!covers.Contains(setting.section))
                    covers.Add(setting.section);
            }
            foreach (PassSwitch pass in passes)
            {
                if (!covers.Contains(pass.section))
                    covers.Add(pass.section);
            }
        }

        /** <summary>Ticks or unticks <paramref name="section" />.</summary> */
        internal void Cover(string section, bool covered)
        {
            if (covered && !covers.Contains(section))
                covers.Add(section);
            else if (!covered)
                covers.Remove(section);
        }

        #endregion
    }
}
