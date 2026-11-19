using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>
     * The pack shaders' material inspector: a header card (the look the material follows, a search field, the ramp and
     * lit ball), then the shader's sections as foldout bars, a feature's switch on its bar, each setting shown only while
     * the toggles above it are on, with its tooltip and a dot when it differs from the look. The layout is the shader's
     * own (<see cref="InspectorLayout" />); every change applies to every selected material under one Undo.
     * </summary>
     */
    public class AnimeShaderGUI : ShaderGUI
    {
        #region Private Fields

        private const int OpenAtFirst = 2;
        private const string FoldKey = "AnimeShaders.Inspector.Open.";
        private const string SearchKey = "AnimeShaders.Inspector.Search";
        private const string SearchTip = "Shows only the settings whose name or tooltip holds these words, their sections opened (a setting hidden by a toggle shows greyed).";

        /** <summary>What one draw of the inspector works from.</summary> */
        private sealed class Drawing
        {
            public MaterialEditor Editor;
            public Material[] Materials;
            public Material First;
            public InspectorLayout Layout;
            public Dictionary<string, MaterialProperty> ByName;
            public AnimeLookPreset Look;
            public HashSet<string> Differ;
            public string Search;
        }

        #endregion

        #region Public API

        /** <summary>Draws the header card, then each section.</summary> */
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            Drawing drawing = Begin(materialEditor, properties);
            DrawCard(drawing);

            int named = 0;
            foreach (InspectorLayout.Section section in drawing.Layout.Sections)
            {
                if (section.Name.Length == 0)
                    DrawRows(drawing, section, false);
                else
                    DrawSection(drawing, section, named++);
            }

            EditorGUILayout.Space();
            materialEditor.RenderQueueField();
        }

        /** <summary>Keeps what follows from the settings (each toggle's keyword, the depth rim's pass, the line's cull) in step whenever the material changes.</summary> */
        public override void ValidateMaterial(Material material) => AnimeMaterials.SyncDerived(material);

        /** <summary>Whether a property is shown: every toggle above it (in its shader's layout) is on.</summary> */
        public static bool Shown(Material material, string property) => InspectorLayout.For(material.shader).Shown(material, property);

        /** <summary>The colour the ramp gives at <paramref name="x" />, as the preview draws it (see <see cref="RampPreview.Colour" />).</summary> */
        public static Color RampColour(Material material, float x) => RampPreview.Colour(material, x);

        /** <summary>Turns <paramref name="section" />'s switch on or off on every one of <paramref name="materials" />: its toggle (and the keyword it drives) or its pass, under one Undo.</summary> */
        public static void SetSwitch(Material[] materials, InspectorLayout.Section section, bool on)
        {
            Undo.RecordObjects(materials, $"{(on ? "Turn On" : "Turn Off")} {section.Name}");
            foreach (Material material in materials)
            {
                if (section.SwitchPass != null)
                    material.SetShaderPassEnabled(section.SwitchPass, on);
                else if (material.HasFloat(section.SwitchProperty))
                {
                    material.SetFloat(section.SwitchProperty, on ? 1f : 0f);
                    MaterialEditor.ApplyMaterialPropertyDrawers(material);
                }
                AnimeMaterials.SyncDerived(material);
                EditorUtility.SetDirty(material);
            }
        }

        #endregion

        #region Helpers

        private static Drawing Begin(MaterialEditor editor, MaterialProperty[] properties)
        {
            var materials = new Material[editor.targets.Length];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = (Material)editor.targets[i];
            var drawing = new Drawing
            {
                Editor = editor,
                Materials = materials,
                First = materials[0],
                Layout = InspectorLayout.For(materials[0].shader),
                ByName = new Dictionary<string, MaterialProperty>(properties.Length),
                Search = SessionState.GetString(SearchKey, string.Empty)
            };
            foreach (MaterialProperty property in properties)
                drawing.ByName[property.name] = property;
            return drawing;
        }

        /** <summary>The header card: the look row, the search field, the ramp and ball, and a rule under them.</summary> */
        private static void DrawCard(Drawing drawing)
        {
            Material first = drawing.First;
            drawing.Look = AnimeLooks.Followed(first, out bool gone);
            bool mixed = false;
            for (int i = 1; i < drawing.Materials.Length; i++)
                mixed |= AnimeLooks.Followed(drawing.Materials[i], out _) != drawing.Look;
            List<string> differ = drawing.Look != null ? AnimeLooks.Differences(first, drawing.Look) : new List<string>();
            drawing.Differ = new HashSet<string>(differ);

            Rect start = EditorGUILayout.GetControlRect(false, 0f);
            LookRow.Draw(drawing.Materials, drawing.Look, gone, mixed, differ.Count);
            EditorGUILayout.Space(InspectorStyle.Gap);
            DrawSearch(drawing);
            EditorGUILayout.Space(InspectorStyle.SmallGap);
            RampPreview.Draw(first);
            Rect end = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(new Rect(start.x - InspectorStyle.BarReach, end.yMax + InspectorStyle.SmallGap, start.width + InspectorStyle.BarReach * 2f, 1f), InspectorStyle.Edge);
            EditorGUILayout.Space(InspectorStyle.Gap);
        }

        private static void DrawSearch(Drawing drawing)
        {
            string search = EditorGUILayout.TextField(new GUIContent(string.Empty, SearchTip), drawing.Search, EditorStyles.toolbarSearchField);
            if (search == drawing.Search)
                return;
            drawing.Search = search;
            SessionState.SetString(SearchKey, search);
        }

        private static void DrawSection(Drawing drawing, InspectorLayout.Section section, int index)
        {
            Material first = drawing.First;
            bool searching = drawing.Search.Length > 0;
            bool nameMatches = searching && Matches(section.Name, null, drawing.Search);
            if (searching && !nameMatches && !AnyMatch(drawing, section))
                return;

            bool on = InspectorLayout.IsOn(first, section);
            bool mixed = false;
            for (int i = 1; i < drawing.Materials.Length; i++)
                mixed |= InspectorLayout.IsOn(drawing.Materials[i], section) != on;

            string key = FoldKey + first.shader.name + "." + section.Name;
            bool open = searching || EditorPrefs.GetBool(key, index < OpenAtFirst);
            bool switchOn = on;
            string switchTip = section.SwitchProperty != null ? Tooltips.For(first.shader, section.SwitchProperty) : null;
            bool shown = SectionBar.Draw(section.Name, open, section.HasSwitch, ref switchOn, mixed, switchTip ?? $"Turns {section.Name} on or off.", Differing(drawing, section), out bool switched);
            if (switched)
            {
                SetSwitch(drawing.Materials, section, switchOn);
                GUIUtility.ExitGUI();
            }
            if (!searching && shown != open)
                EditorPrefs.SetBool(key, shown);
            if (!shown)
                return;

            EditorGUILayout.Space(InspectorStyle.SmallGap);
            using (new EditorGUI.DisabledScope(section.HasSwitch && !switchOn && !mixed))
                DrawRows(drawing, section, searching && !nameMatches);
            EditorGUILayout.Space(InspectorStyle.Gap);
        }

        /** <summary>The section's rows; with <paramref name="filtered" />, only those matching the search, a hidden one shown greyed.</summary> */
        private static void DrawRows(Drawing drawing, InspectorLayout.Section section, bool filtered)
        {
            Material material = drawing.First;
            Shader shader = material.shader;
            foreach (int index in section.Properties)
            {
                string name = shader.GetPropertyName(index);
                if (name == section.SwitchProperty || !drawing.ByName.TryGetValue(name, out MaterialProperty property))
                    continue;
                string tip = Tooltips.For(shader, name);
                bool shown = drawing.Layout.Shown(material, name);
                if (filtered ? !Matches(property.displayName, tip, drawing.Search) : !shown)
                    continue;

                using (new EditorGUI.DisabledScope(!shown))
                    drawing.Editor.ShaderProperty(property, new GUIContent(property.displayName, tip));
                if (drawing.Differ.Contains(name))
                    MarkDiffering(GUILayoutUtility.GetLastRect(), drawing, name);
                InspectorWarnings.Under(property, material);
            }
        }

        private static bool AnyMatch(Drawing drawing, InspectorLayout.Section section)
        {
            Shader shader = drawing.First.shader;
            foreach (int index in section.Properties)
            {
                string name = shader.GetPropertyName(index);
                if (drawing.ByName.TryGetValue(name, out MaterialProperty property) && Matches(property.displayName, Tooltips.For(shader, name), drawing.Search))
                    return true;
            }
            return false;
        }

        /** <summary>Whether every word of <paramref name="search" /> is in <paramref name="label" /> or <paramref name="tip" />, ignoring case.</summary> */
        private static bool Matches(string label, string tip, string search)
        {
            foreach (string word in search.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (label.IndexOf(word, StringComparison.OrdinalIgnoreCase) < 0 && (tip == null || tip.IndexOf(word, StringComparison.OrdinalIgnoreCase) < 0))
                    return false;
            }
            return true;
        }

        /** <summary>How many of <paramref name="section" />'s settings (and its pass switch) differ from the look.</summary> */
        private static int Differing(Drawing drawing, InspectorLayout.Section section)
        {
            int count = section.SwitchPass != null && drawing.Differ.Contains(section.SwitchPass) ? 1 : 0;
            foreach (int index in section.Properties)
            {
                if (drawing.Differ.Contains(drawing.First.shader.GetPropertyName(index)))
                    count++;
            }
            return count;
        }

        /** <summary>A dot left of a row whose value differs from the look; a right-click offers to revert it alone.</summary> */
        private static void MarkDiffering(Rect row, Drawing drawing, string property)
        {
            if (Event.current.type == EventType.Repaint)
            {
                float half = InspectorStyle.DotSize * 0.5f;
                var dot = new Rect(row.x - InspectorStyle.DotInset, row.center.y - half, InspectorStyle.DotSize, InspectorStyle.DotSize);
                GUI.DrawTexture(dot, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, InspectorStyle.Accent, 0f, half);
            }
            if (Event.current.type != EventType.ContextClick || !row.Contains(Event.current.mousePosition))
                return;
            AnimeLookPreset look = drawing.Look;
            Material[] materials = drawing.Materials;
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent($"Revert to {look.name}"), false, () => AnimeLooks.Revert(materials, look, property));
            menu.ShowAsContext();
            Event.current.Use();
        }

        #endregion
    }
}
