using System.Collections.Generic;
using System.Linq;
using AnimeShaders.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;
using Keywords = AnimeShaders.AnimeShaderIds.Keywords;
using Switches = AnimeShaders.AnimeShaderIds.Switches;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>Looks: taken from one material and applied to another they give the same look (values, keywords, passes),
     * never touching the model's own settings or textures; across shaders each takes what it has; what differs is counted
     * from the shown, covered settings only; Apply undoes; the link to a look lives in the material's importer and only
     * where a material can keep one.</summary>
     */
    public class LookPresetTests
    {
        #region Private Fields

        private const string Folder = "Assets/AnimeLookTests";

        private readonly List<Object> _made = new();

        #endregion

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", "AnimeLookTests");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object made in _made)
            {
                if (made != null && !AssetDatabase.Contains(made))
                    Object.DestroyImmediate(made);
            }
            _made.Clear();
            AssetDatabase.DeleteAsset(Folder);
            AnimeLooks.Forget();
            Undo.ClearAll();
        }

        private Material Made(string shader)
        {
            var material = new Material(Shader.Find(shader));
            _made.Add(material);
            return material;
        }

        private AnimeLookPreset LookOf(Material material)
        {
            var look = ScriptableObject.CreateInstance<AnimeLookPreset>();
            _made.Add(look);
            AnimeLooks.Take(material, out List<AnimeLookPreset.Setting> settings, out List<AnimeLookPreset.PassSwitch> passes);
            look.Hold(material.shader, settings, passes);
            return look;
        }

        /** <summary>A material styled away from the shader's defaults: a second band, a rim, the line off.</summary> */
        private Material Styled()
        {
            Material material = Made(AnimeShaderIds.CharacterShader);
            material.SetFloat(Ids.ShadowThreshold, 0.31f);
            material.SetColor(Ids.ShadowTint, new Color(0.2f, 0.3f, 0.9f));
            material.SetFloat(Switches.Band2, 1f);
            material.SetFloat(Ids.Band2Offset, 0.17f);
            material.SetColor(Ids.RimColor, new Color(3f, 2f, 1f));
            AnimeMaterials.SyncToggleKeywords(material);
            AnimeMaterials.SetLine(material, false);
            return material;
        }

        private static void AssertClose(Color expected, Color actual, string what)
        {
            for (int i = 0; i < 4; i++)
                Assert.AreEqual(expected[i], actual[i], 1e-4f, $"{what}, channel {i}");
        }

        [Test]
        public void ALookTakenAndApplied_GivesTheSameValuesKeywordsAndPasses()
        {
            Material source = Styled();
            Material target = Made(AnimeShaderIds.CharacterShader);

            AnimeMaterials.ApplyLook(target, LookOf(source));

            Assert.AreEqual(0.31f, target.GetFloat(Ids.ShadowThreshold), 1e-6f);
            AssertClose(source.GetColor(Ids.ShadowTint), target.GetColor(Ids.ShadowTint), "the shadow tint");
            AssertClose(source.GetColor(Ids.RimColor), target.GetColor(Ids.RimColor), "an HDR colour, whole");
            Assert.IsTrue(target.IsKeywordEnabled(Keywords.Band2Name), "the toggle's keyword follows its float");
            Assert.IsFalse(target.GetShaderPassEnabled(AnimeShaderIds.OutlineLightMode), "the line's switch, a pass");
            Assert.IsEmpty(AnimeLooks.Differences(target, LookOf(source)), "nothing differs after Apply");
        }

        [Test]
        public void ApplyLeavesTheModelsOwnSettingsAndTextures()
        {
            Material source = Styled();
            source.SetColor(Ids.BaseColor, Color.red);
            source.SetFloat(Ids.OutlineZOffset, 0.4f);
            Material target = Made(AnimeShaderIds.CharacterShader);
            var texture = new Texture2D(2, 2);
            _made.Add(texture);
            target.SetTexture(Ids.BaseMap, texture);
            Color colourBefore = target.GetColor(Ids.BaseColor);
            float offsetBefore = target.GetFloat(Ids.OutlineZOffset);
            Assume.That(colourBefore, Is.Not.EqualTo(Color.red));
            Assume.That(offsetBefore, Is.Not.EqualTo(0.4f));

            AnimeMaterials.ApplyLook(target, LookOf(source));

            Assert.AreEqual(colourBefore, target.GetColor(Ids.BaseColor), "base colour is the model's");
            Assert.AreEqual(offsetBefore, target.GetFloat(Ids.OutlineZOffset), "the line's offset is the model's");
            Assert.AreSame(texture, target.GetTexture(Ids.BaseMap), "a look holds no texture");
        }

        [Test]
        public void ACharacterLookOnHair_SetsOnlyWhatTheyShare()
        {
            Material hair = Made(AnimeShaderIds.HairShader);
            float ringBefore = hair.GetFloat(Ids.RingWidth);

            int set = AnimeMaterials.ApplyLook(hair, LookOf(Styled()));

            Assert.Greater(set, 0);
            Assert.AreEqual(0.31f, hair.GetFloat(Ids.ShadowThreshold), 1e-6f, "a shared tone");
            Assert.AreEqual(ringBefore, hair.GetFloat(Ids.RingWidth), "hair's own ring is no Character setting");
        }

        [Test]
        public void ApplySkipsWhatDoesntFit_AndHoldsARangeToItsLimits()
        {
            Material target = Made(AnimeShaderIds.CharacterShader);
            var look = ScriptableObject.CreateInstance<AnimeLookPreset>();
            _made.Add(look);
            look.Hold(target.shader, new[]
            {
                new AnimeLookPreset.Setting { property = "_NoSuchSetting", section = "Tones", kind = AnimeLookPreset.ValueKind.Float, number = 3f },
                new AnimeLookPreset.Setting { property = Ids.ShadowTintName, section = "Tones", kind = AnimeLookPreset.ValueKind.Float, number = 3f },
                new AnimeLookPreset.Setting { property = Ids.ShadowThresholdName, section = "Tones", kind = AnimeLookPreset.ValueKind.Float, number = 7f },
            }, new AnimeLookPreset.PassSwitch[0]);
            Color tint = target.GetColor(Ids.ShadowTint);

            int set = AnimeMaterials.ApplyLook(target, look);

            Assert.AreEqual(1, set, "only the threshold fits");
            Assert.AreEqual(tint, target.GetColor(Ids.ShadowTint), "a float for a colour is skipped");
            Assert.AreEqual(target.shader.GetPropertyRangeLimits(target.shader.FindPropertyIndex(Ids.ShadowThresholdName)).y, target.GetFloat(Ids.ShadowThreshold), 1e-6f, "held to its range");
        }

        [Test]
        public void AnUncoveredSection_IsLeftAsItIs()
        {
            AnimeLookPreset look = LookOf(Styled());
            look.Cover("Tones", false);
            Material target = Made(AnimeShaderIds.CharacterShader);
            float before = target.GetFloat(Ids.ShadowThreshold);

            AnimeMaterials.ApplyLook(target, look);

            Assert.AreEqual(before, target.GetFloat(Ids.ShadowThreshold), "Tones unticked");
            Assert.IsTrue(target.IsKeywordEnabled(Keywords.Band2Name), "the second band still set");
        }

        [Test]
        public void Differences_CountOnlyShownCoveredSettings_WithinTolerance()
        {
            Material source = Styled();
            AnimeLookPreset look = LookOf(source);
            Material target = Made(AnimeShaderIds.CharacterShader);
            AnimeMaterials.ApplyLook(target, look);

            target.SetFloat(Ids.ShadowThreshold, 0.31f + 1e-6f);
            Assert.IsEmpty(AnimeLooks.Differences(target, look), "within tolerance");

            target.SetFloat(Ids.ShadowThreshold, 0.5f);
            target.SetFloat(Switches.Band2, 0f);
            AnimeMaterials.SyncToggleKeywords(target);
            target.SetFloat(Ids.Band2Offset, 0.4f);
            List<string> differ = AnimeLooks.Differences(target, look);
            CollectionAssert.Contains(differ, Ids.ShadowThresholdName);
            CollectionAssert.Contains(differ, Switches.Band2Name);
            CollectionAssert.DoesNotContain(differ, Ids.Band2OffsetName, "hidden while its band is off");
            target.SetFloat(Switches.OutlineCull, target.GetFloat(Switches.OutlineCull) + 1f);
            CollectionAssert.DoesNotContain(AnimeLooks.Differences(target, look), Switches.OutlineCullName, "a hidden setting never counts, even when it differs");

            AnimeLooks.Apply(new[] { target }, look);
            Assert.IsEmpty(AnimeLooks.Differences(target, look), "Revert (Apply again) leaves none");
        }

        [Test]
        public void ApplyUndoes_ValuesKeywordsAndPasses()
        {
            Material target = Made(AnimeShaderIds.CharacterShader);
            float before = target.GetFloat(Ids.ShadowThreshold);

            Undo.IncrementCurrentGroup();
            AnimeLooks.Apply(new[] { target }, LookOf(Styled()));
            Undo.PerformUndo();

            Assert.AreEqual(before, target.GetFloat(Ids.ShadowThreshold), 1e-6f);
            Assert.IsFalse(target.IsKeywordEnabled(Keywords.Band2Name));
            Assert.IsTrue(target.GetShaderPassEnabled(AnimeShaderIds.OutlineLightMode));
        }

        [Test]
        public void TheLink_KeepsOtherUserData_AndReadsBack()
        {
            string data = AnimeLooks.WithLink("other.tool=1", "abc");
            Assert.AreEqual("abc", AnimeLooks.LinkIn(data));
            StringAssert.Contains("other.tool=1", data);
            Assert.AreEqual("other.tool=1", AnimeLooks.WithLink(data, null), "none removes only the link");
            Assert.AreEqual("def", AnimeLooks.LinkIn(AnimeLooks.WithLink(data, "def")), "a new link replaces the old");
            Assert.AreEqual("{\n\n  \"x\": 1\n}", AnimeLooks.WithLink(AnimeLooks.WithLink("{\n\n  \"x\": 1\n}", "abc"), null), "another tool's blank lines kept");
            Assert.AreEqual(string.Empty, AnimeLooks.WithLink(null, null));
            Assert.IsNull(AnimeLooks.LinkIn(null));
        }

        [Test]
        public void RevertingOneSetting_SetsOnlyIt_OnEverySelectedMaterial_AndUndoes()
        {
            Material source = Styled();
            AnimeLookPreset look = LookOf(source);
            Material[] both = { Made(AnimeShaderIds.CharacterShader), Made(AnimeShaderIds.CharacterShader) };
            Material hair = Made(AnimeShaderIds.HairShader);
            foreach (Material material in both)
            {
                AnimeMaterials.ApplyLook(material, look);
                material.SetFloat(Ids.ShadowThreshold, 0.8f);
                material.SetFloat(Ids.ShadowSoftness, 0.5f);
            }

            Undo.IncrementCurrentGroup();
            AnimeLooks.Revert(new[] { both[0], both[1], hair }, look, Ids.ShadowThresholdName);

            foreach (Material material in both)
            {
                Assert.AreEqual(0.31f, material.GetFloat(Ids.ShadowThreshold), 1e-6f, "back to the look");
                Assert.AreEqual(0.5f, material.GetFloat(Ids.ShadowSoftness), 1e-6f, "the other edit kept");
            }
            Undo.PerformUndo();
            Assert.AreEqual(0.8f, both[0].GetFloat(Ids.ShadowThreshold), 1e-6f, "undo brings the edit back");
        }

        [Test]
        public void RevertingAToggle_KeepsItsKeywordInStep()
        {
            Material source = Styled();
            AnimeLookPreset look = LookOf(source);
            Material target = Made(AnimeShaderIds.CharacterShader);
            AnimeMaterials.ApplyLook(target, look);
            target.SetFloat(Switches.Band2, 0f);
            AnimeMaterials.SyncToggleKeywords(target);

            AnimeLooks.Revert(new[] { target }, look, Switches.Band2Name);

            Assert.IsTrue(target.IsKeywordEnabled(Keywords.Band2Name));
        }

        [Test]
        public void Update_TakesTheMaterialsValues_KeepsWhatItsShaderLacks_AndItsTicks()
        {
            AnimeLookPreset look = LookOf(Styled());
            look.Cover("Tones", false);
            Material eyes = Made(AnimeShaderIds.EyesShader);
            Assume.That(eyes.HasFloat(Ids.Band2Offset), Is.False, "the eyes have no second band");
            eyes.SetFloat(Ids.ShadowSoftness, 0.42f);

            Undo.IncrementCurrentGroup();
            AnimeLooks.Update(look, eyes);

            Assert.AreEqual(0.42f, look.Settings.First(setting => setting.property == Ids.ShadowSoftnessName).number, 1e-6f, "the eyes' value taken");
            Assert.AreEqual(0.17f, look.Settings.First(setting => setting.property == Ids.Band2OffsetName).number, 1e-6f, "a setting the eyes lack kept");
            Assert.IsFalse(look.Sets("Tones"), "an unticked section stays unticked");
            Assert.IsTrue(look.Sets("Through Hair"), "a section new to it is covered");
            Assert.AreEqual(AnimeShaderIds.CharacterShader, look.MadeFrom.name, "still the shader it was taken from");
        }

        [Test]
        public void AMaterialVariant_CantFollowALook()
        {
            Material parent = Made(AnimeShaderIds.CharacterShader);
            Material variant = Made(AnimeShaderIds.CharacterShader);
            variant.parent = parent;

            Assert.IsFalse(AnimeLooks.CanFollow(variant, out string reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void EachDefaultLook_HoldsItsShadersDefaults()
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(AnimeLookPreset)}", new[] { System.IO.Path.GetDirectoryName(ShaderDefaults.PackShadersFolder()).Replace('\\', '/') + "/Looks" }))
            {
                var look = AssetDatabase.LoadAssetAtPath<AnimeLookPreset>(AssetDatabase.GUIDToAssetPath(guid));
                Material fresh = Made(look.MadeFrom.name);
                Assert.IsEmpty(AnimeLooks.Differences(fresh, look), $"{look.name} matches a new {look.MadeFrom.name} material");
            }
        }

        [Test]
        public void AMaterialSavedAsAnAsset_FollowsItsLook_ThroughAReimport_AndItsCopyFollowsNone()
        {
            Material material = Styled();
            AssetDatabase.CreateAsset(material, $"{Folder}/Followed.mat");
            AnimeLookPreset look = AnimeLooks.Extract(material);

            AssetDatabase.ImportAsset($"{Folder}/Followed.mat", ImportAssetOptions.ForceUpdate);
            AnimeLooks.Forget();
            Assert.AreSame(look, AnimeLooks.Followed(material, out bool gone));
            Assert.IsFalse(gone);

            AssetDatabase.CopyAsset($"{Folder}/Followed.mat", $"{Folder}/Copy.mat");
            var copy = AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/Copy.mat");
            Assert.IsNull(AnimeLooks.Followed(copy, out _), "a copy follows nothing until applied");

            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(look));
            AnimeLooks.Forget();
            Assert.IsNull(AnimeLooks.Followed(material, out gone));
            Assert.IsTrue(gone, "its look was deleted");
        }

        [Test]
        public void AMaterialNotSavedAsAnAsset_CantFollowALook()
        {
            Assert.IsFalse(AnimeLooks.CanFollow(Made(AnimeShaderIds.CharacterShader), out string reason));
            Assert.IsNotEmpty(reason);
        }
    }
}
