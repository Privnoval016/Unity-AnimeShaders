using System;
using System.Collections.Generic;
using System.Linq;
using AnimeShaders.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Keywords = AnimeShaders.AnimeShaderIds.Keywords;
using Switches = AnimeShaders.AnimeShaderIds.Switches;

namespace AnimeShaders.Tests
{
    /**
     * <summary>The inspector's layout as every pack shader declares it: sections cover each shown setting once, switches and
     * show-with marks name what the shader has, every shown setting has a tooltip, and a section's switch turns its feature
     * on and off on every selected material under one Undo.</summary>
     */
    public class InspectorLayoutTests
    {
        #region Private Fields

        private readonly List<Material> _made = new();

        #endregion

        [TearDown]
        public void TearDown()
        {
            foreach (Material material in _made)
                Object.DestroyImmediate(material);
            _made.Clear();
            Undo.ClearAll();
        }

        /** <summary>Every shader in the pack's Shaders folder laid out by sections (the pack's inspector shaders).</summary> */
        private static List<Shader> InspectorShaders()
        {
            var shaders = new List<Shader>();
            foreach (string guid in AssetDatabase.FindAssets("t:Shader", new[] { ShaderDefaults.PackShadersFolder() }))
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));
                if (shader == null || !PackSources.ShaderText(shader).Contains($"CustomEditor \"{typeof(AnimeShaderGUI).FullName}\""))
                    continue;
                Assert.IsTrue(InspectorLayout.For(shader).Sections.Any(section => section.Name.Length > 0), $"{shader.name} uses the inspector but declares no sections");
                shaders.Add(shader);
            }
            Assert.IsNotEmpty(shaders, "the pack has shaders using the inspector");
            return shaders;
        }

        private Material Made(Shader shader)
        {
            var material = new Material(shader);
            _made.Add(material);
            return material;
        }

        private Shader CharacterShader()
        {
            Material material = TestMaterials.Character(Color.white, Color.grey);
            _made.Add(material);
            return material.shader;
        }

        private static IEnumerable<int> ShownProperties(Shader shader)
        {
            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                if ((shader.GetPropertyFlags(i) & ShaderPropertyFlags.HideInInspector) == 0)
                    yield return i;
            }
        }

        [Test]
        public void EveryShownSetting_IsInExactlyOneSection_AndNoHeaderIsLeft()
        {
            foreach (Shader shader in InspectorShaders())
            {
                List<int> listed = InspectorLayout.For(shader).Sections.SelectMany(section => section.Properties).ToList();
                CollectionAssert.AreEquivalent(ShownProperties(shader).ToList(), listed, shader.name);
                for (int i = 0; i < shader.GetPropertyCount(); i++)
                    Assert.IsFalse(shader.GetPropertyAttributes(i).Any(attribute => attribute.StartsWith("Header(")), $"{shader.name}: {shader.GetPropertyName(i)} keeps a Header (its title would show twice)");
            }
        }

        [Test]
        public void EveryShowWithAndSwitch_NamesWhatTheShaderHas()
        {
            foreach (Shader shader in InspectorShaders())
            {
                InspectorLayout layout = InspectorLayout.For(shader);
                foreach (KeyValuePair<string, string[]> feature in layout.FeatureSettings)
                {
                    int toggle = shader.FindPropertyIndex(feature.Key);
                    Assert.GreaterOrEqual(toggle, 0, $"{shader.name}: shown with {feature.Key}, which it lacks");
                    Assert.AreEqual(ShaderPropertyType.Float, shader.GetPropertyType(toggle), $"{shader.name}: {feature.Key} is no toggle");
                }

                foreach (InspectorLayout.Section section in layout.Sections)
                {
                    if (section.SwitchProperty != null)
                        Assert.Contains(shader.FindPropertyIndex(section.SwitchProperty), section.Properties, $"{shader.name}: {section.Name}'s switch sits in another section");
                    if (section.SwitchPass != null)
                        Assert.IsTrue(AnimeMaterials.HasPass(shader, section.SwitchPass), $"{shader.name}: {section.Name} is switched by a pass it lacks");
                }
            }
        }

        [Test]
        public void TheMarks_SitBesideToggle_WithNoDrawerClash()
        {
            foreach (Shader shader in InspectorShaders())
                MaterialEditor.ApplyMaterialPropertyDrawers(Made(shader));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AToggleInsideAnother_ShowsWithBoth()
        {
            Shader shader = CharacterShader();
            InspectorLayout layout = InspectorLayout.For(shader);

            Assert.Contains(AnimeShaderIds.MaterialProperties.InsideFadeStartName, layout.FeatureSettings[Switches.InsideToneName], "nested under the inside tone");
            Assert.Contains(AnimeShaderIds.MaterialProperties.InsideFadeStartName, layout.FeatureSettings[Switches.InsideFadeName], "and under its own fade");
            Assert.AreEqual(Switches.InsideFadeName, layout.ShowWith(AnimeShaderIds.MaterialProperties.InsideFadeStartName), "its nearest toggle");
        }

        [Test]
        public void EveryShownSetting_HasATooltip()
        {
            foreach (Shader shader in InspectorShaders())
            {
                foreach (int i in ShownProperties(shader))
                    Assert.IsFalse(string.IsNullOrEmpty(Tooltips.For(shader, shader.GetPropertyName(i))), $"{shader.name}: {shader.GetPropertyName(i)} has no tooltip in Tooltips.txt");
            }
        }

        [Test]
        public void ASectionSwitch_TurnsItsKeywordOrPass_OnEverySelectedMaterial_AndUndoes()
        {
            Shader shader = CharacterShader();
            InspectorLayout layout = InspectorLayout.For(shader);
            InspectorLayout.Section band = layout.Sections.Single(section => section.SwitchProperty == Switches.Band2Name);
            InspectorLayout.Section line = layout.Sections.Single(section => section.SwitchPass != null);
            Material[] both = { Made(shader), Made(shader) };
            both[1].SetFloat(AnimeShaderIds.MaterialProperties.ShadowThreshold, 0.3f);
            AnimeShaderGUI.SetSwitch(new[] { both[1] }, band, true);
            Assume.That(InspectorLayout.IsOn(both[0], band), Is.False, "the two start apart (a mixed switch)");

            Undo.IncrementCurrentGroup();
            AnimeShaderGUI.SetSwitch(both, band, true);
            AnimeShaderGUI.SetSwitch(both, line, false);
            foreach (Material material in both)
            {
                Assert.IsTrue(material.IsKeywordEnabled(Keywords.Band2Name), "the toggle's keyword follows it");
                Assert.IsTrue(InspectorLayout.IsOn(material, band));
                Assert.IsFalse(material.GetShaderPassEnabled(line.SwitchPass), "the line's pass is off");
            }
            Assert.AreEqual(0.3f, both[1].GetFloat(AnimeShaderIds.MaterialProperties.ShadowThreshold), 1e-6f, "nothing else changes");

            Undo.PerformUndo();
            Assert.IsTrue(both[0].GetShaderPassEnabled(line.SwitchPass), "undo brings the line back");

            AnimeShaderGUI.SetSwitch(both, band, false);
            Assert.IsFalse(both[0].IsKeywordEnabled(Keywords.Band2Name), "off turns its keyword off");
        }

        [Test]
        public void AShowWithLoop_NeitherHangsNorHides()
        {
            Shader shader = ShaderUtil.CreateShaderAsset("Shader \"Hidden/AnimeLoopTest\" { Properties { [AnimeShowWith(_A)] _A(\"A\", Float) = 0 [AnimeShowWith(_C)] _B(\"B\", Float) = 0 [AnimeShowWith(_B)] _C(\"C\", Float) = 1 } SubShader { Pass { } } }");
            var material = new Material(shader);
            _made.Add(material);
            InspectorLayout layout = InspectorLayout.For(shader);

            Assert.IsTrue(layout.Shown(material, "_A"), "shown with itself: no toggle above it");
            CollectionAssert.AreEqual(new[] { "_C" }, layout.Above("_B"), "a loop stops where it repeats");
            Object.DestroyImmediate(shader);
        }

        [Test]
        public void ATooltip_ComesFromTheShadersOwnLineFirst_ThenTheShared()
        {
            var lines = Tooltips.Parse("# a comment\n_Tint: Shared.\nAnime/Hair/_Tint: Hair's own.\n\nbroken line");

            Assert.AreEqual("Hair's own.", Tooltips.Lookup(lines, "Anime/Hair", "_Tint"));
            Assert.AreEqual("Shared.", Tooltips.Lookup(lines, "Anime/Character", "_Tint"));
            Assert.IsNull(Tooltips.Lookup(lines, "Anime/Character", "_Missing"), "a project's own setting: none, no throw");
            Assert.AreEqual(2, lines.Count, "comments, blanks and broken lines are skipped");
        }

        [Test]
        public void TheModelsOwnSettings_AreMarked()
        {
            InspectorLayout layout = InspectorLayout.For(CharacterShader());

            Assert.Contains(AnimeShaderIds.MaterialProperties.BaseColorName, layout.Own.ToList());
            Assert.IsFalse(layout.Own.Contains(AnimeShaderIds.MaterialProperties.ShadowTintName), "a tone is the look's");
        }
    }
}
