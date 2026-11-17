using AnimeShaders.EditorTools;
using NUnit.Framework;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;
using Keywords = AnimeShaders.AnimeShaderIds.Keywords;
using Switches = AnimeShaders.AnimeShaderIds.Switches;

namespace AnimeShaders.Tests
{
    /**
     * <summary>The inspector shows each feature's settings only while it is on, and its ramp preview matches what the
     * shader draws; the line's cull follows Line On Open Edges, set by script or the inspector, on every material in the
     * project.</summary>
     */
    public class AnimeShaderGUITests
    {
        #region Private Fields

        private static readonly Color Base = new(0.8f, 0.6f, 0.4f);
        private static readonly Color Tint = new(0.5f, 0.4f, 0.7f);

        private Material _material;

        #endregion

        [SetUp]
        public void SetUp()
        {
            _material = TestMaterials.Character(Base, Tint);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_material);
        }

        [Test]
        public void EveryNameInTheFeatureMap_IsAPropertyOfTheShader()
        {
            Shader shader = _material.shader;
            foreach (var feature in AnimeShaderGUI.FeatureSettings)
            {
                Assert.GreaterOrEqual(shader.FindPropertyIndex(feature.Key), 0, feature.Key);
                foreach (string setting in feature.Value)
                    Assert.GreaterOrEqual(shader.FindPropertyIndex(setting), 0, $"{feature.Key}: {setting}");
            }
        }

        [Test]
        public void ASettingShows_OnlyWhileEveryToggleAboveItIsOn()
        {
            AnimeMaterials.SetFeature(_material, Keywords.InsideToneName, Switches.InsideTone, false);
            AnimeMaterials.SetFeature(_material, Keywords.InsideFadeName, Switches.InsideFade, true);
            Assert.IsFalse(AnimeShaderGUI.Shown(_material, Ids.InsideTintName), "the inside tone is off");
            Assert.IsFalse(AnimeShaderGUI.Shown(_material, Ids.InsideFadeStartName), "its parent is off");

            AnimeMaterials.SetFeature(_material, Keywords.InsideToneName, Switches.InsideTone, true);
            Assert.IsTrue(AnimeShaderGUI.Shown(_material, Ids.InsideTintName));
            Assert.IsTrue(AnimeShaderGUI.Shown(_material, Ids.InsideFadeStartName));

            AnimeMaterials.SetFeature(_material, Keywords.InsideFadeName, Switches.InsideFade, false);
            Assert.IsFalse(AnimeShaderGUI.Shown(_material, Ids.InsideFadeStartName), "the fade itself is off");
            Assert.IsTrue(AnimeShaderGUI.Shown(_material, Ids.ShadowThresholdName), "a setting with no toggle always shows");
        }

        [TestCase(-0.6f)]
        [TestCase(0.6f)]
        [TestCase(0.02f)]
        public void TheRampPreview_IsWhatTheShaderDraws(float worldX)
        {
            _material.SetFloat(Ids.ShadowSoftness, 0.2f);
            using var stage = new LitStage();
            stage.LightFrom(Vector3.right);
            stage.Add(TestMeshes.Sphere(), _material, Vector3.zero, Quaternion.identity, Vector3.one * 2f);
            Color rendered = FrameReads.At(stage.Render(), stage.Column(worldX), stage.Size / 2);
            float column = stage.Column(worldX);
            float pixelCentreX = (column + 0.5f) / stage.Size * 2f * stage.Camera.orthographicSize - stage.Camera.orthographicSize;
            Color preview = AnimeShaderGUI.RampColour(_material, pixelCentreX * 0.5f + 0.5f);
            Assert.IsTrue(FrameReads.Near(rendered, preview, 0.02f), $"rendered {rendered}, preview {preview}");
        }

        [Test]
        public void OpenEdges_DrawTheLineFromBothSides_AndOffPutsTheCullBack()
        {
            AnimeMaterials.SetOpenEdges(_material, 1f);
            Assert.AreEqual((float)UnityEngine.Rendering.CullMode.Off, _material.GetFloat(Switches.OutlineCull));
            AnimeMaterials.SetOpenEdges(_material, 0f);
            Assert.AreEqual((float)UnityEngine.Rendering.CullMode.Front, _material.GetFloat(Switches.OutlineCull));
        }

        [Test]
        public void AScriptsEdit_IsPutInStep_AsTheInspectorValidates()
        {
            _material.SetFloat(Ids.OutlineOpenEdges, 0.5f);
            new AnimeShaderGUI().ValidateMaterial(_material);
            Assert.AreEqual((float)UnityEngine.Rendering.CullMode.Off, _material.GetFloat(Switches.OutlineCull));
        }

        [Test]
        public void EveryMaterialInTheProject_HasItsLineCullInStep()
        {
            var outOfStep = new System.Collections.Generic.List<string>();
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Material"))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || !material.HasFloat(Ids.OutlineOpenEdges) || !material.HasFloat(Switches.OutlineCull))
                    continue;
                float expected = (float)(material.GetFloat(Ids.OutlineOpenEdges) > 0f ? UnityEngine.Rendering.CullMode.Off : UnityEngine.Rendering.CullMode.Front);
                if (!Mathf.Approximately(material.GetFloat(Switches.OutlineCull), expected))
                    outOfStep.Add(path);
            }
            CollectionAssert.IsEmpty(outOfStep, "set Line On Open Edges through AnimeMaterials.SetOpenEdges or the inspector");
        }
    }
}
