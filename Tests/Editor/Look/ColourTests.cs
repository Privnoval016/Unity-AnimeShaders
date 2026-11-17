using NUnit.Framework;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;

namespace AnimeShaders.Tests
{
    /**
     * <summary>The Colour group on a lit quad facing the camera: at Brightness and Saturation 1 the shaded colour is
     * unchanged; Brightness scales it; Saturation 0 turns it to its own grey, above 1 moves it away from that grey by as
     * much more; every shader it is meant for takes it.</summary>
     */
    public class ColourTests
    {
        #region Private Fields

        private const float Close = 0.01f;

        private static readonly Color Base = new(0.6f, 0.3f, 0.2f);

        private LitStage _stage;
        private Material _material;
        private Mesh _quad;
        private DirectorGlobals _director;

        #endregion

        #region Helpers

        private Color Shaded()
        {
            return FrameReads.Centre(_stage.Render());
        }

        private static float Grey(Color colour)
        {
            return 0.2126f * colour.r + 0.7152f * colour.g + 0.0722f * colour.b;
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _director = new DirectorGlobals();
            _stage = new LitStage();
            _stage.LightFrom(Vector3.back);
            _material = TestMaterials.Character(Base, new Color(0.5f, 0.5f, 0.5f));
            _material.SetFloat(Ids.ColourBrightness, 1f);
            _material.SetFloat(Ids.ColourSaturation, 1f);
            _quad = TestMeshes.Quad();
            _stage.Add(_quad, _material, Vector3.zero, Quaternion.identity, Vector3.one * 2f);
        }

        [TearDown]
        public void TearDown()
        {
            _stage.Dispose();
            _director.Dispose();
            Object.DestroyImmediate(_material);
            Object.DestroyImmediate(_quad);
        }

        [Test]
        public void AtOne_TheColourIsAsShaded()
        {
            Assert.IsTrue(FrameReads.Near(Shaded(), Base, Close), $"{Shaded()}");
        }

        [Test]
        public void Brightness_ScalesIt()
        {
            Color plain = Shaded();
            _material.SetFloat(Ids.ColourBrightness, 1.5f);
            Color bright = Shaded();
            Assert.AreEqual(1.5f, bright.r / plain.r, Close, "red");
            Assert.AreEqual(1.5f, bright.g / plain.g, Close, "green");
            Assert.AreEqual(1.5f, bright.b / plain.b, Close * 2f, "blue");
        }

        [Test]
        public void Saturation_MovesItFromItsOwnGrey()
        {
            Color plain = Shaded();
            float grey = Grey(plain);
            _material.SetFloat(Ids.ColourSaturation, 0f);
            Color none = Shaded();
            Assert.IsTrue(FrameReads.Near(none, new Color(grey, grey, grey), Close), $"0: its own grey: {none}");
            _material.SetFloat(Ids.ColourSaturation, 1.5f);
            Color rich = Shaded();
            Assert.AreEqual(grey + 1.5f * (plain.r - grey), rich.r, Close, "red farther from grey");
            Assert.AreEqual(grey + 1.5f * (plain.b - grey), rich.b, Close, "blue farther from grey");
            Assert.AreEqual(grey, Grey(rich), Close, "its grey kept");
        }

        [TestCase("Anime/Character")]
        [TestCase(AnimeShaderIds.FaceShader)]
        [TestCase(AnimeShaderIds.HairShader)]
        [TestCase(AnimeShaderIds.EyesShader)]
        public void EveryCharacterShader_TakesIt(string shader)
        {
            Assert.IsTrue(Shader.Find(shader).FindPropertyIndex(Ids.ColourBrightnessName) >= 0, "Brightness");
            Assert.IsTrue(Shader.Find(shader).FindPropertyIndex(Ids.ColourSaturationName) >= 0, "Saturation");
        }
    }
}
