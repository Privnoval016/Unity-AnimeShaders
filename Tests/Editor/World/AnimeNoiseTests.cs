using System.IO;
using AnimeShaders.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AnimeShaders.Tests
{
    /** <summary>The baked noise: the same on every bake, every channel tiling, the committed file what the code bakes, held to its import settings.</summary> */
    public class AnimeNoiseTests
    {
        #region Private Fields

        private const int Small = 256;

        #endregion

        #region Helpers

        private static byte Channel(Color32 pixel, int channel)
        {
            return channel switch { 0 => pixel.r, 1 => pixel.g, 2 => pixel.b, _ => pixel.a };
        }

        private static string BakedPath()
        {
            string shader = AssetDatabase.GetAssetPath(Shader.Find(TestMaterials.CharacterShader));
            return Path.Combine(Path.GetDirectoryName(shader) ?? string.Empty, "Textures", AnimeNoiseBaker.FileName).Replace('\\', '/');
        }

        #endregion

        [Test]
        public void TwoBakes_AreTheSame()
        {
            CollectionAssert.AreEqual(AnimeNoiseBaker.Pixels(Small), AnimeNoiseBaker.Pixels(Small));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void EveryChannel_Tiles_TheWrapNoRougherThanInside(int channel)
        {
            Color32[] pixels = AnimeNoiseBaker.Pixels(Small);
            int worstInside = 0, worstWrap = 0;
            for (int i = 0; i < Small; i++)
            {
                for (int j = 0; j + 1 < Small; j++)
                {
                    worstInside = Mathf.Max(worstInside, Mathf.Abs(Channel(pixels[i * Small + j], channel) - Channel(pixels[i * Small + j + 1], channel)));
                    worstInside = Mathf.Max(worstInside, Mathf.Abs(Channel(pixels[j * Small + i], channel) - Channel(pixels[(j + 1) * Small + i], channel)));
                }
                worstWrap = Mathf.Max(worstWrap, Mathf.Abs(Channel(pixels[i * Small], channel) - Channel(pixels[i * Small + Small - 1], channel)));
                worstWrap = Mathf.Max(worstWrap, Mathf.Abs(Channel(pixels[i], channel) - Channel(pixels[(Small - 1) * Small + i], channel)));
            }
            Assert.LessOrEqual(worstWrap, worstInside, "a seam where the texture wraps");
        }

        [Test]
        public void TheCommittedFile_IsWhatTheCodeBakes()
        {
            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                Assert.IsTrue(decoded.LoadImage(File.ReadAllBytes(BakedPath())), "the baked file reads");
                Assert.AreEqual(AnimeNoiseBaker.Size, decoded.width);
                CollectionAssert.AreEqual(AnimeNoiseBaker.Pixels(), decoded.GetPixels32(), "bake again after changing the noise (and raise its Version)");
            }
            finally
            {
                Object.DestroyImmediate(decoded);
            }
        }

        [Test]
        public void TheCommittedFile_IsHeldToItsImportSettings()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(BakedPath());
            Assert.IsFalse(importer.sRGBTexture, "linear");
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
            Assert.IsTrue(importer.mipmapEnabled);
            Assert.AreEqual(TextureWrapMode.Repeat, importer.wrapMode);
            CollectionAssert.Contains(AssetDatabase.GetLabels(AssetDatabase.LoadMainAssetAtPath(BakedPath())), AnimeNoiseImport.Label);
        }
    }
}
