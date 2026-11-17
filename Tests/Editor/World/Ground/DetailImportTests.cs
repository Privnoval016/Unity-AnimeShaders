using System;
using System.IO;
using AnimeShaders.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>Packed detail maps on disk: the import rule holds a labelled map to a data texture's settings, puts a
     * changed one back on reimport, forces a normal-map import back to data and leaves unlabelled textures alone; a set
     * packs from its source files into a map beside it whose pixels carry each source in its channel, records it, and
     * reads as out of date once a source or setting changes; a set with Stones writes a point-sampled stone map beside
     * it, counts its stones and removes the map when Stones is off; the pack shaders' default textures. Everything is
     * written to a scratch folder deleted before and after.</summary>
     */
    public class DetailImportTests
    {
        #region Private Fields

        private const string Scratch = "Assets/AnimeDetailTestScratch";
        private const int Size = 8;

        private const string ColourField = "colour";
        private const string OcclusionField = "occlusion";
        private const string NormalField = "normal";
        private const string MaskField = "mask";
        private const string ConventionField = "normalConvention";
        private const string WeightField = "occlusionWeight";
        private const string SizeField = "size";
        private const string StonesField = "stones";
        private const int StoneTile = 64;
        private const int StoneCell = 16;

        #endregion

        #region Helpers

        private static string WritePng(string name, Color32 colour)
        {
            string path = $"{Scratch}/{name}.png";
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
            try
            {
                var pixels = new Color32[Size * Size];
                Array.Fill(pixels, colour);
                texture.SetPixels32(pixels);
                texture.Apply(false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return path;
        }

        /** <summary>An occlusion map of a 4 by 4 grid of bright stones on dark two-texel joints.</summary> */
        private static string WriteStoneGrid(string name)
        {
            string path = $"{Scratch}/{name}.png";
            var texture = new Texture2D(StoneTile, StoneTile, TextureFormat.RGBA32, false, true);
            try
            {
                var pixels = new Color32[StoneTile * StoneTile];
                for (int i = 0; i < pixels.Length; i++)
                {
                    bool joint = i % StoneTile % StoneCell < 2 || i / StoneTile % StoneCell < 2;
                    byte value = joint ? (byte)40 : (byte)230;
                    pixels[i] = new Color32(value, value, value, 255);
                }
                texture.SetPixels32(pixels);
                texture.Apply(false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return path;
        }

        /** <summary>A saved set with the stone grid as its occlusion, Stones on, its stone settings set for the grid.</summary> */
        private static AnimeDetailSet MakeStoneSet(string name)
        {
            AnimeDetailSet set = MakeSet(name, new Color32(128, 128, 255, 255));
            var serialized = new SerializedObject(set);
            serialized.FindProperty(OcclusionField).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(WriteStoneGrid(name + "Occlusion"));
            serialized.FindProperty(StonesField).boolValue = true;
            serialized.FindProperty("stoneBlur").floatValue = 100f / StoneTile;
            serialized.FindProperty("stoneThreshold").floatValue = 0.5f;
            serialized.FindProperty("stoneMinSize").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return set;
        }

        private static TextureImporter Importer(string path)
        {
            return (TextureImporter)AssetImporter.GetAtPath(path);
        }

        private static Color32 FirstPixelOnDisk(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path), false), path);
                return texture.GetPixels32()[0];
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        /** <summary>A saved set with a flat colour (120 grey), a flat normal and the given extra sources and settings.</summary> */
        private static AnimeDetailSet MakeSet(string name, Color32 normal, string maskPath = null, NormalConvention convention = NormalConvention.OpenGL)
        {
            string colour = WritePng(name + "Colour", new Color32(120, 120, 120, 255));
            string normalPath = WritePng(name + "Normal", normal);
            var set = ScriptableObject.CreateInstance<AnimeDetailSet>();
            AssetDatabase.CreateAsset(set, $"{Scratch}/{name}.asset");
            var serialized = new SerializedObject(set);
            serialized.FindProperty(ColourField).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(colour);
            serialized.FindProperty(NormalField).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            serialized.FindProperty(MaskField).objectReferenceValue = maskPath == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
            serialized.FindProperty(ConventionField).enumValueIndex = (int)convention;
            serialized.FindProperty(SizeField).intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return set;
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(Scratch);
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(Scratch));
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Scratch);
        }

        [Test]
        public void ALabelledMap_TakesTheSettings_AndKeepsThemOnReimport()
        {
            string path = $"{Scratch}/Packed.png";
            DetailImage neutral = DetailMath.Neutral(Size);
            DataTextures.Write(neutral.Pixels, Size, Size, path, AnimeDetailImport.Label);
            TextureImporter importer = Importer(path);
            Assert.IsFalse(importer.sRGBTexture, "linear");
            Assert.IsFalse(importer.alphaIsTransparency, "alpha is data");
            Assert.AreEqual(TextureImporterAlphaSource.FromInput, importer.alphaSource);
            Assert.IsTrue(importer.mipmapEnabled, "mipmapped");
            Assert.AreEqual(TextureWrapMode.Repeat, importer.wrapMode);
            Assert.AreEqual(FilterMode.Trilinear, importer.filterMode);
            Assert.AreEqual(TextureImporterCompression.CompressedHQ, importer.textureCompression);
            Assert.GreaterOrEqual(importer.maxTextureSize, AnimeDetailImport.MaxSize);

            importer.sRGBTexture = true;
            importer.SaveAndReimport();
            Assert.IsFalse(Importer(path).sRGBTexture, "put back on reimport");
        }

        [Test]
        public void ANormalMapImport_IsForcedBackToData_OnceLabelled()
        {
            string path = WritePng("WasNormal", new Color32(128, 128, 255, 255));
            TextureImporter importer = Importer(path);
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
            AssetDatabase.SetLabels(AssetDatabase.LoadMainAssetAtPath(path), new[] { AnimeDetailImport.Label });
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Assert.AreEqual(TextureImporterType.Default, Importer(path).textureType);
        }

        [Test]
        public void AnUnlabelledTexture_KeepsItsOwnSettings()
        {
            string path = WritePng("Plain", new Color32(10, 20, 30, 255));
            TextureImporter importer = Importer(path);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            Assert.AreEqual(TextureImporterCompression.Uncompressed, Importer(path).textureCompression, "the rule would have made it high quality");
        }

        [Test]
        public void ASet_PacksBesideItself_EachSourceInItsChannel()
        {
            AnimeDetailSet set = MakeSet("Flat", new Color32(128, 128, 255, 255));
            AnimeDetailPacker.Pack(set);
            Assert.AreEqual($"{Scratch}/Flat.png", AssetDatabase.GetAssetPath(set.Packed), "the map beside the set");
            CollectionAssert.Contains(AssetDatabase.GetLabels(set.Packed), AnimeDetailImport.Label);
            Assert.AreEqual(AnimeDetailPacker.HashOf(set), set.PackedHash, "up to date");
            Color32 flat = FirstPixelOnDisk($"{Scratch}/Flat.png");
            Assert.AreEqual((128, 255, 128, 128), ((int)flat.r, (int)flat.g, (int)flat.b, (int)flat.a), "middle shade, no mask, flat normal");

            string mask = WritePng("Mask", new Color32(64, 64, 64, 255));
            AnimeDetailSet masked = MakeSet("Masked", new Color32(128, 46, 230, 255), mask, NormalConvention.DirectX);
            AnimeDetailPacker.Pack(masked);
            Color32 pixel = FirstPixelOnDisk($"{Scratch}/Masked.png");
            Assert.AreEqual(64, pixel.g, "the mask");
            Assert.AreEqual(255 - 46, pixel.a, "the DirectX normal's green flipped");
        }

        [Test]
        public void ASetGoesOutOfDate_WhenASourceOrSettingChanges()
        {
            AnimeDetailSet set = MakeSet("Tracked", new Color32(128, 128, 255, 255));
            AnimeDetailPacker.Pack(set);
            Assert.AreEqual(AnimeDetailPacker.HashOf(set), set.PackedHash, "up to date");

            var serialized = new SerializedObject(set);
            serialized.FindProperty(WeightField).floatValue = 0.1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.AreNotEqual(AnimeDetailPacker.HashOf(set), set.PackedHash, "a setting changed");

            AnimeDetailPacker.Pack(set);
            WritePng("TrackedColour", new Color32(30, 30, 30, 255));
            Assert.AreNotEqual(AnimeDetailPacker.HashOf(set), set.PackedHash, "a source's pixels changed");
        }

        [Test]
        public void ASetWithStones_WritesAPointSampledStoneMap_CountsItsStones_AndRemovesItWhenOff()
        {
            AnimeDetailSet set = MakeStoneSet("Paving");
            AnimeDetailPacker.Pack(set);
            string path = $"{Scratch}/Paving{AnimeDetailPacker.StoneMapSuffix}.png";
            Assert.AreEqual(path, AssetDatabase.GetAssetPath(set.StoneMap), "the stone map beside the set");
            Assert.AreEqual(StoneTile / StoneCell * (StoneTile / StoneCell), set.StoneCount);
            CollectionAssert.Contains(AssetDatabase.GetLabels(set.StoneMap), AnimeDetailImport.StonesLabel);
            TextureImporter importer = Importer(path);
            Assert.IsFalse(importer.sRGBTexture, "linear");
            Assert.IsFalse(importer.mipmapEnabled, "no mips: values never blend");
            Assert.AreEqual(FilterMode.Point, importer.filterMode);
            Assert.AreEqual(TextureWrapMode.Repeat, importer.wrapMode);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
            Assert.GreaterOrEqual(importer.maxTextureSize, StoneMath.MapSize);
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            Assert.AreEqual(FilterMode.Point, Importer(path).filterMode, "put back on reimport");

            var written = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                Assert.IsTrue(written.LoadImage(File.ReadAllBytes(path), false));
                Assert.IsTrue(source.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(set.Occlusion)), false));
                StoneMap expected = StoneMath.Find(new DetailImage(source.width, source.height, source.GetPixels32()), set.StoneSettings);
                CollectionAssert.AreEqual(expected.Image.Pixels, written.GetPixels32(), "the file holds the stones found, value and code as written");
            }
            finally
            {
                Object.DestroyImmediate(written);
                Object.DestroyImmediate(source);
            }

            var serialized = new SerializedObject(set);
            serialized.FindProperty(StonesField).boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AnimeDetailPacker.Pack(set);
            Assert.IsNull(set.StoneMap, "no map recorded");
            Assert.IsFalse(File.Exists(path), "the old map removed");
        }

        [TestCase("stones", 0f)]
        [TestCase("stoneBlur", 1f)]
        [TestCase("stoneThreshold", 0.6f)]
        [TestCase("stoneMinSize", 2f)]
        public void AStoneSetting_PutsTheSetOutOfDate(string field, float value)
        {
            AnimeDetailSet set = MakeStoneSet("Tracked" + field);
            AnimeDetailPacker.Pack(set);
            Assert.AreEqual(AnimeDetailPacker.HashOf(set), set.PackedHash, "up to date");
            var serialized = new SerializedObject(set);
            SerializedProperty property = serialized.FindProperty(field);
            if (property.propertyType == SerializedPropertyType.Boolean)
                property.boolValue = value > 0f;
            else
                property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.AreNotEqual(AnimeDetailPacker.HashOf(set), set.PackedHash, $"{field} changed");
        }

        [Test]
        public void StonesWithoutOcclusion_WriteNoMap()
        {
            AnimeDetailSet set = MakeSet("NoStones", new Color32(128, 128, 255, 255));
            var serialized = new SerializedObject(set);
            serialized.FindProperty(StonesField).boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            LogAssert.Expect(LogType.Warning, "NoStones: stones need an occlusion map.");
            AnimeDetailPacker.Pack(set);
            Assert.IsNull(set.StoneMap);
            Assert.IsNotNull(set.Packed, "the detail map still packs");
        }

        [Test]
        public void ASetWithoutOcclusion_Packs()
        {
            AnimeDetailSet set = MakeSet("NoOcclusion", new Color32(128, 128, 255, 255));
            Assert.IsNull(new SerializedObject(set).FindProperty(OcclusionField).objectReferenceValue);
            AnimeDetailPacker.Pack(set);
            Assert.IsNotNull(set.Packed);
        }

        [Test]
        public void ASetMissingItsColour_SaysWhich()
        {
            var set = ScriptableObject.CreateInstance<AnimeDetailSet>();
            AssetDatabase.CreateAsset(set, $"{Scratch}/Empty.asset");
            var error = Assert.Throws<InvalidOperationException>(() => AnimeDetailPacker.Pack(set));
            StringAssert.Contains("colour", error.Message);
        }

        [Test]
        public void TheGround_DefaultsToTheNeutralMapAndTheNoise_AndSettingOneKeepsTheOther()
        {
            var importer = (ShaderImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(Shader.Find("Anime/Ground")));
            Texture noise = importer.GetDefaultTexture(AnimeShaderIds.Sky.NoiseTexName);
            Texture neutral = importer.GetDefaultTexture(AnimeShaderIds.MaterialProperties.FieldDetailName);
            Assert.AreEqual(AnimeNoiseBaker.PackPath(), AssetDatabase.GetAssetPath(noise), "the noise");
            StringAssert.EndsWith(AnimeDetailPacker.NeutralFileName, AssetDatabase.GetAssetPath(neutral), "the neutral detail map");

            Assert.Greater(ShaderDefaults.Set(new[] { AnimeShaderIds.Sky.NoiseTexName }, noise), 1, "the ground and the sky both take the noise");
            importer = (ShaderImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(Shader.Find("Anime/Ground")));
            Assert.AreEqual(neutral, importer.GetDefaultTexture(AnimeShaderIds.MaterialProperties.FieldDetailName), "the detail default survives");
            Assert.AreEqual(neutral, importer.GetDefaultTexture(AnimeShaderIds.MaterialProperties.CircleDetailName), "both");
        }
    }
}
