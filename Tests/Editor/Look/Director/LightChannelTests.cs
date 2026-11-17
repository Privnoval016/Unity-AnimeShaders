using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>The director's two channels in render: a character quad (Anime/Character) and a world quad (Anime/World)
     * side by side in shade; each channel's shadow colour, set to different values, reaches only its own shader; the
     * characters' direction turns the characters' tones and not the world's.</summary>
     */
    public class LightChannelTests
    {
        #region Private Fields

        private const float Close = 0.02f;
        private const float Apart = 0.6f;

        private static readonly Color CharacterShade = new(0.5f, 0.5f, 0.5f);
        private static readonly Color WorldShade = new(0.8f, 0.8f, 0.8f);

        private LitStage _stage;
        private DirectorGlobals _director;
        private IDisposable _isolated;
        private Material _character;
        private Material _world;
        private Mesh _made;
        private LightSet _set;

        #endregion

        #region Helpers

        private void Build(Mesh mesh)
        {
            _stage.Add(mesh, _character, new Vector3(-Apart, 0f, 0f), Quaternion.identity, Vector3.one);
            _stage.Add(mesh, _world, new Vector3(Apart, 0f, 0f), Quaternion.identity, Vector3.one);
        }

        private Color At(Color[] frame, float x)
        {
            return FrameReads.At(frame, _stage.Column(x), _stage.Size / 2);
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _director = new DirectorGlobals();
            _isolated = AnimeLightDirector.Isolate();
            _stage = new LitStage();
            _character = TestMaterials.Character(Color.white, new Color(0.5f, 0.5f, 0.5f));
            _world = TestMaterials.WorldLit("Anime/World");
            _set = ScriptableObject.CreateInstance<LightSet>();
        }

        [TearDown]
        public void TearDown()
        {
            _isolated.Dispose();
            _stage.Dispose();
            _director.Dispose();
            Object.DestroyImmediate(_character);
            Object.DestroyImmediate(_world);
            Object.DestroyImmediate(_made);
            Object.DestroyImmediate(_set);
        }

        [Test]
        public void EachChannelsShadowColour_ReachesOnlyItsOwnShaders()
        {
            _made = TestMeshes.Quad();
            Build(_made);
            _stage.LightFrom(Vector3.forward);
            Color[] neutral = _stage.Render();
            _set.Characters.Colours.Overrides = true;
            _set.Characters.Colours.Shadow = CharacterShade;
            _set.World.Colours.Overrides = true;
            _set.World.Colours.Shadow = WorldShade;
            AnimeLightDirector.Push(_set, LightLevel.Base);
            Color[] lit = _stage.Render();
            Assert.AreEqual(CharacterShade.linear.r, At(lit, -Apart).r / At(neutral, -Apart).r, Close, "the character takes the characters' shade");
            Assert.AreEqual(WorldShade.linear.r, At(lit, Apart).r / At(neutral, Apart).r, Close, "the world takes the world's");
        }

        [Test]
        public void TheCharactersDirection_TurnsTheCharactersAndNotTheWorld()
        {
            Build(TestMeshes.Sphere());
            _stage.LightFrom(Vector3.right);
            Color[] before = _stage.Render();
            _set.Characters.Direction.Overrides = true;
            _set.Characters.Direction.Yaw = -90f;
            _set.Characters.Direction.Pitch = 0f;
            AnimeLightDirector.Push(_set, LightLevel.Base);
            Color[] after = _stage.Render();
            const float side = 0.35f;
            Assert.Greater(At(before, -Apart + side).r, At(before, -Apart - side).r + 0.1f, "the character lit from the right");
            Assert.Greater(At(after, -Apart - side).r, At(after, -Apart + side).r + 0.1f, "turned: lit from the left");
            Assert.AreEqual(At(before, Apart + side).r, At(after, Apart + side).r, Close, "the world as it was, right");
            Assert.AreEqual(At(before, Apart - side).r, At(after, Apart - side).r, Close, "and left");
        }
    }
}
