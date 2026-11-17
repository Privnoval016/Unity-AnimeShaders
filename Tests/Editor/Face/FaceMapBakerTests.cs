using System;
using AnimeShaders.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>A face map on disk: the import rule holds a labelled EXR to linear, uncompressed, unmipmapped, bilinear,
     * clamped settings and its values come back within a half float's step, and an unlabelled one is left alone; the
     * out-of-date hash changes with every setting and only then; a mesh that can't be baked says why. Written to a
     * scratch folder deleted before and after.</summary>
     */
    public class FaceMapBakerTests
    {
        #region Private Fields

        private const string Scratch = "Assets/AnimeFaceMapTestScratch";
        private const int Size = 8;
        private const float HalfStep = 1e-3f;

        private AnimeFaceMap _faceMap;

        #endregion

        #region Helpers

        private static Color[] Ramp()
        {
            var pixels = new Color[Size * Size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(i / (float)pixels.Length, 1f - i / (float)pixels.Length, 0.3f, 1f);
            return pixels;
        }

        /** <summary>The texture's values as the GPU holds them (an imported map isn't readable on the CPU).</summary> */
        private static Color[] ReadBack(Texture2D texture)
        {
            RenderTexture target = RenderTexture.GetTemporary(Size, Size, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var copy = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            RenderTexture was = RenderTexture.active;
            try
            {
                Graphics.Blit(texture, target);
                RenderTexture.active = target;
                copy.ReadPixels(new Rect(0, 0, Size, Size), 0, 0, false);
                copy.Apply(false);
                return copy.GetPixels();
            }
            finally
            {
                RenderTexture.active = was;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(copy);
            }
        }

        private void Change(string field, float value)
        {
            var serialized = new SerializedObject(_faceMap);
            SerializedProperty property = serialized.FindProperty(field);
            if (property.propertyType == SerializedPropertyType.Integer)
                property.intValue = (int)value;
            else
                property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(Scratch);
            AssetDatabase.CreateFolder("Assets", Scratch.Substring("Assets/".Length));
            _faceMap = ScriptableObject.CreateInstance<AnimeFaceMap>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_faceMap);
            AssetDatabase.DeleteAsset(Scratch);
        }

        [Test]
        public void ALabelledMap_IsHeldToLinearClampedData_AndKeepsItsValues()
        {
            string path = $"{Scratch}/Face.exr";
            Color[] written = Ramp();
            DataTextures.WriteHalf(written, Size, Size, path, AnimeFaceMapImport.Label);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.IsFalse(importer.sRGBTexture, "linear");
            Assert.IsFalse(importer.mipmapEnabled, "no mips");
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode, "clamped");
            Assert.AreEqual(FilterMode.Bilinear, importer.filterMode, "bilinear");
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, "uncompressed");

            Color[] read = ReadBack(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            for (int i = 0; i < written.Length; i++)
                Assert.Less(Vector4.Distance(written[i], read[i]), HalfStep, $"texel {i}");
        }

        [Test]
        public void AnUnlabelledEXR_IsLeftAlone()
        {
            string path = $"{Scratch}/Other.exr";
            DataTextures.WriteHalf(Ramp(), Size, Size, path, "SomeOtherLabel");
            Assert.IsTrue(((TextureImporter)AssetImporter.GetAtPath(path)).mipmapEnabled, "an import's own default");
        }

        [Test]
        public void TheHash_ChangesWithEverySetting_AndOnlyThen()
        {
            string first = AnimeFaceMapBaker.HashOf(_faceMap);
            Assert.AreEqual(first, AnimeFaceMapBaker.HashOf(_faceMap), "unchanged: the same");
            string last = first;
            foreach ((string field, float value) in new[] { ("size", 512f), ("smoothing", 1.5f), ("sphereBlend", 0.4f), ("gutter", 3f) })
            {
                Change(field, value);
                string now = AnimeFaceMapBaker.HashOf(_faceMap);
                Assert.AreNotEqual(last, now, field);
                last = now;
            }
        }

        [Test]
        public void AMeshThatCantBeBaked_SaysWhy()
        {
            StringAssert.Contains("mesh", AnimeFaceMapBaker.Problem(null));
            var made = new Mesh { name = "Made" };
            try
            {
                StringAssert.Contains("not inside a model", AnimeFaceMapBaker.Problem(made));
                Assert.Throws<InvalidOperationException>(() => AnimeFaceMapBaker.Source(made));
            }
            finally
            {
                Object.DestroyImmediate(made);
            }
        }

        [Test]
        public void ABuiltInMesh_HashesAndSaysItIsNoModel()
        {
            var sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            var serialized = new SerializedObject(_faceMap);
            serialized.FindProperty("mesh").objectReferenceValue = sphere;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.DoesNotThrow(() => AnimeFaceMapBaker.HashOf(_faceMap), "no model file to hash: no exception");
            Assert.IsNotNull(AnimeFaceMapBaker.Problem(sphere), "what stops it baking is said");
        }
    }
}
