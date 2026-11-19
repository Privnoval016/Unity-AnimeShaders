using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>
     * How a shader lays out its inspector, read from its own markers (<see cref="AnimeSectionDecorator" /> and the
     * rest): its sections in order, each section's switch, which toggle shows each setting, and which settings are the
     * material's own. The shader is the one owner; nothing here names a property.
     * </summary>
     */
    public sealed class InspectorLayout
    {
        #region Private Fields

        private const string SectionMark = "AnimeSection";
        private const string SwitchMark = "AnimeSectionSwitch";
        private const string ShowWithMark = "AnimeShowWith";
        private const string OwnMark = "AnimeOwn";

        private static readonly Dictionary<Shader, InspectorLayout> Cache = new();

        private readonly List<Section> _sections = new();
        private readonly Dictionary<string, string> _showWith = new();
        private readonly HashSet<string> _own = new();
        private readonly Dictionary<string, string[]> _featureSettings = new();

        #endregion

        /** <summary>One section: its name, its shown properties (indices into the shader) in order, and its switch, if any.</summary> */
        public sealed class Section
        {
            /** <summary>Its name, as its bar shows it; empty for the properties before the first section.</summary> */
            public string Name { get; internal set; }

            /** <summary>Its shown properties, as indices into the shader, in order.</summary> */
            public List<int> Properties { get; } = new();

            /** <summary>The toggle that turns it on or off; null when none does.</summary> */
            public string SwitchProperty { get; internal set; }

            /** <summary>The light mode of the pass that turns it on or off; null when none does.</summary> */
            public string SwitchPass { get; internal set; }

            /** <summary>Whether a switch turns the whole section on or off.</summary> */
            public bool HasSwitch => SwitchProperty != null || SwitchPass != null;
        }

        #region Properties

        /** <summary>The sections, in the shader's order.</summary> */
        public IReadOnlyList<Section> Sections => _sections;

        /** <summary>Each feature toggle and every setting it shows, those under a nested toggle included.</summary> */
        public IReadOnlyDictionary<string, string[]> FeatureSettings => _featureSettings;

        /** <summary>Settings that belong to the material's model, never to a look.</summary> */
        public IReadOnlyCollection<string> Own => _own;

        #endregion

        #region Public API

        /** <summary>The layout of <paramref name="shader" />, read once and kept until its shader is imported again.</summary> */
        public static InspectorLayout For(Shader shader)
        {
            if (!Cache.TryGetValue(shader, out InspectorLayout layout))
                Cache[shader] = layout = new InspectorLayout(shader);
            return layout;
        }

        /** <summary>Forgets every layout read, so the next is read afresh (a shader was imported).</summary> */
        public static void Forget() => Cache.Clear();

        /** <summary>Whether <paramref name="property" /> belongs to the material's model (<c>[AnimeOwn]</c>), never to a look.</summary> */
        public bool IsOwn(string property) => _own.Contains(property);

        /** <summary>The toggle that shows <paramref name="property" />; null when it always shows.</summary> */
        public string ShowWith(string property) => _showWith.TryGetValue(property, out string toggle) ? toggle : null;

        /** <summary>Whether <paramref name="property" /> shows on <paramref name="material" />: every toggle above it is on.</summary> */
        public bool Shown(Material material, string property)
        {
            foreach (string toggle in Above(property))
            {
                if (material.HasFloat(toggle) && material.GetFloat(toggle) < 0.5f)
                    return false;
            }
            return true;
        }

        /** <summary>The toggles above <paramref name="property" />, nearest first; a loop (a toggle shown with itself) stops where it repeats.</summary> */
        public List<string> Above(string property)
        {
            var above = new List<string>();
            for (string toggle = ShowWith(property); toggle != null && toggle != property && !above.Contains(toggle); toggle = ShowWith(toggle))
                above.Add(toggle);
            return above;
        }

        /** <summary>Whether <paramref name="section" /> is on for <paramref name="material" />: its switch is on, or it has none.</summary> */
        public static bool IsOn(Material material, Section section)
        {
            if (section.SwitchPass != null)
                return material.GetShaderPassEnabled(section.SwitchPass);
            return section.SwitchProperty == null || !material.HasFloat(section.SwitchProperty) || material.GetFloat(section.SwitchProperty) > 0.5f;
        }

        #endregion

        #region Helpers

        private InspectorLayout(Shader shader)
        {
            var current = new Section { Name = string.Empty };
            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                string name = shader.GetPropertyName(i);
                foreach (string attribute in shader.GetPropertyAttributes(i))
                {
                    string mark = MarkOf(attribute, out string[] args);
                    if (mark == SectionMark && args.Length > 0)
                    {
                        if (current.Properties.Count > 0 || current.HasSwitch)
                            _sections.Add(current);
                        current = new Section { Name = args[0] };
                    }
                    else if (mark == SwitchMark)
                    {
                        if (args.Length >= 2)
                            current.SwitchPass = args[1];
                        else
                            current.SwitchProperty = name;
                    }
                    else if (mark == ShowWithMark && args.Length > 0)
                    {
                        _showWith[name] = args[0];
                    }
                    else if (mark == OwnMark)
                    {
                        _own.Add(name);
                    }
                }

                if ((shader.GetPropertyFlags(i) & ShaderPropertyFlags.HideInInspector) == 0)
                    current.Properties.Add(i);
            }
            if (current.Properties.Count > 0 || current.HasSwitch)
                _sections.Add(current);

            var governed = new Dictionary<string, List<string>>();
            foreach (string property in _showWith.Keys)
            {
                foreach (string toggle in Above(property))
                {
                    if (!governed.TryGetValue(toggle, out List<string> list))
                        governed[toggle] = list = new List<string>();
                    list.Add(property);
                }
            }
            foreach (KeyValuePair<string, List<string>> pair in governed)
                _featureSettings[pair.Key] = pair.Value.ToArray();
        }

        /** <summary>An attribute's name and its arguments ("AnimeShowWith(_Band2)": AnimeShowWith, [_Band2]).</summary> */
        private static string MarkOf(string attribute, out string[] args)
        {
            int open = attribute.IndexOf('(');
            if (open < 0)
            {
                args = Array.Empty<string>();
                return attribute.Trim();
            }
            int close = attribute.LastIndexOf(')');
            string inner = close > open ? attribute.Substring(open + 1, close - open - 1) : attribute.Substring(open + 1);
            args = inner.Split(',');
            for (int i = 0; i < args.Length; i++)
                args[i] = args[i].Trim();
            return attribute.Substring(0, open).Trim();
        }

        #endregion
    }
}
