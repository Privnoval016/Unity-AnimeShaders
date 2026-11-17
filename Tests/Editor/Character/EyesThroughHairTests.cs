using NUnit.Framework;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>An eye on Anime/Eyes just in front of a face, its fringe on Anime/Hair just in front of it, flat colours (red
     * eye, green hair): with the hair depth feature the eye shows over its fringe by Hair Over Eyes (fully at 0, half at
     * 0.5, not at 1); without the feature, through hair a metre in front (another character's), or behind anything else in
     * front of it, it stays hidden. With a head frame, the fringe closes over it as the head turns, by the yaw and pitch
     * limits, either way; it shows fully short of the distance limit's fade, partly within it and not past it; with the
     * thin-hair rule it shows through a single strand, not a solid fringe; the tint colours it as it shows; where two
     * eye layers show through, the nearer is on top.</summary>
     */
    public class EyesThroughHairTests
    {
        #region Private Fields

        private const float Close = 0.06f;
        private const float EyeDepth = -0.01f;
        private const float FringeDepth = -0.04f;
        private const float YawLimit = 70f;
        private const float PitchLimit = 45f;
        private const float Distance = 0.15f;
        private const float Fade = 0.03f;
        private const float FarLayer = 0.05f;

        private MadePipeline _pipeline;
        private LitStage _stage;
        private Material _face;
        private Material _eye;
        private Material _hair;
        private Material _blocker;
        private Material _farEye;
        private Mesh _quad;
        private MeshRenderer _fringe;
        private MeshRenderer _eyeRenderer;

        #endregion

        #region Helpers

        /** <summary>A material showing exactly its colour, lit or not.</summary> */
        private static Material Flat(string shader, Color colour)
        {
            var material = new Material(Shader.Find(shader));
            TestMaterials.SetLinear(material, Ids.BaseColor, colour);
            TestMaterials.SetLinear(material, Ids.ShadowTint, Color.white);
            material.SetFloat(Ids.LightInfluence, 0f);
            material.SetFloat(Ids.AmbientStrength, 0f);
            material.SetFloat(Ids.CastShadowStrength, 0f);
            material.SetFloat(Ids.ContactShading, 0f);
            if (material.HasFloat(Ids.RingStrength))
                material.SetFloat(Ids.RingStrength, 0f);
            material.SetShaderPassEnabled(AnimeShaderIds.OutlineLightMode, material.shader.name == AnimeShaderIds.EyesShader);
            return material;
        }

        private void Build(bool withFeature)
        {
            _pipeline = withFeature ? new MadePipeline(ScriptableObject.CreateInstance<AnimeHairDepthFeature>()) : new MadePipeline();
            _stage = new LitStage();
            _stage.LightFrom(Vector3.back);
            _quad = TestMeshes.Quad();
            _face = Flat("Anime/Character", Color.grey);
            _eye = Flat(AnimeShaderIds.EyesShader, Color.red);
            _eye.SetFloat(Ids.EyeYawLimit, YawLimit);
            _eye.SetFloat(Ids.EyePitchLimit, PitchLimit);
            _eye.SetFloat(Ids.EyeHairDistance, Distance);
            _eye.SetFloat(Ids.EyeHairFade, Fade);
            _eye.SetFloat(Ids.EyeThinHair, 0f);
            TestMaterials.SetLinear(_eye, Ids.EyeThroughTint, Color.white);
            _hair = Flat(AnimeShaderIds.HairShader, Color.green);
            _stage.Add(_quad, _face, Vector3.zero, Quaternion.identity, Vector3.one * 2f);
            _eyeRenderer = _stage.Add(_quad, _eye, new Vector3(0f, 0f, EyeDepth), Quaternion.identity, Vector3.one * 0.4f);
            _fringe = _stage.Add(_quad, _hair, new Vector3(0f, 0.8f, FringeDepth), Quaternion.identity, Vector3.one * 2f);
        }

        private Color Centre()
        {
            return FrameReads.Centre(_stage.Render());
        }

        /** <summary>The eye's head frame, facing the camera turned by <paramref name="turn" />.</summary> */
        private void Facing(Quaternion turn)
        {
            var block = new MaterialPropertyBlock();
            block.SetVector(AnimeShaderIds.Head.Forward, turn * Vector3.back);
            block.SetVector(AnimeShaderIds.Head.Right, turn * Vector3.left);
            block.SetVector(AnimeShaderIds.Head.Up, turn * Vector3.up);
            _eyeRenderer.SetPropertyBlock(block);
        }

        #endregion

        [TearDown]
        public void TearDown()
        {
            _stage?.Dispose();
            _pipeline?.Dispose();
            Object.DestroyImmediate(_face);
            Object.DestroyImmediate(_eye);
            Object.DestroyImmediate(_hair);
            Object.DestroyImmediate(_blocker);
            Object.DestroyImmediate(_farEye);
            Object.DestroyImmediate(_quad);
        }

        [Test]
        public void TheEye_ShowsOverItsFringe_ByHairOverEyes()
        {
            Build(true);
            _eye.SetFloat(Ids.HairOverEyes, 0f);
            Assert.IsTrue(FrameReads.Near(Centre(), Color.red, Close), $"0: the eye, fully: {Centre()}");
            _eye.SetFloat(Ids.HairOverEyes, 0.5f);
            Color half = Centre();
            Assert.AreEqual(0.5f, half.r, Close, $"0.5: half eye: {half}");
            Assert.AreEqual(0.5f, half.g, Close, $"0.5: half hair: {half}");
            _eye.SetFloat(Ids.HairOverEyes, 1f);
            Assert.IsTrue(FrameReads.Near(Centre(), Color.green, Close), $"1: the hair, as without: {Centre()}");
        }

        [Test]
        public void WithoutTheFeature_TheHairHidesTheEye()
        {
            Build(false);
            _eye.SetFloat(Ids.HairOverEyes, 0f);
            Assert.IsTrue(FrameReads.Near(Centre(), Color.green, Close), $"{Centre()}");
        }

        [Test]
        public void HairFarInFront_HidesTheEye()
        {
            Build(true);
            _eye.SetFloat(Ids.HairOverEyes, 0f);
            _fringe.transform.position = new Vector3(0f, 0.8f, -1f);
            Assert.IsTrue(FrameReads.Near(Centre(), Color.green, Close), $"another's hair a metre away: {Centre()}");
        }

        [Test]
        public void SomethingElseInFront_HidesTheEye()
        {
            Build(true);
            _eye.SetFloat(Ids.HairOverEyes, 0f);
            _blocker = Flat("Anime/Character", Color.blue);
            _stage.Add(_quad, _blocker, new Vector3(0f, 0f, -0.02f), Quaternion.identity, Vector3.one * 0.6f);
            Assert.IsTrue(FrameReads.Near(Centre(), Color.green, Close), $"a hand between eye and fringe: the fringe, not the eye: {Centre()}");
        }

        [Test]
        public void AsTheHeadTurns_TheFringeClosesOverTheEye()
        {
            Build(true);
            _eye.SetFloat(Ids.HairOverEyes, 0f);
            Facing(Quaternion.identity);
            Assert.IsTrue(FrameReads.Near(Centre(), Color.red, Close), $"facing: fully through: {Centre()}");
            foreach (float side in new[] { 1f, -1f })
            {
                Facing(Quaternion.Euler(0f, side * YawLimit * 0.5f, 0f));
                Color half = Centre();
                Assert.That(half.r, Is.InRange(0.15f, 0.85f), $"half way to the yaw limit ({side}): partly: {half}");
                Facing(Quaternion.Euler(0f, side * (YawLimit + 5f), 0f));
                Assert.IsTrue(FrameReads.Near(Centre(), Color.green, Close), $"past the yaw limit ({side}): hidden: {Centre()}");
                Facing(Quaternion.Euler(side * (PitchLimit + 5f), 0f, 0f));
                Assert.IsTrue(FrameReads.Near(Centre(), Color.green, Close), $"tipped past the pitch limit ({side}): hidden: {Centre()}");
            }
        }

        [Test]
        public void NearTheDistanceLimit_ItFadesOut()
        {
            Build(true);
            _eye.SetFloat(Ids.HairOverEyes, 0f);
            _eye.SetFloat(Ids.EyeHairFade, 0.01f);
            _eye.SetFloat(Ids.EyeHairDistance, 0.05f);
            Assert.IsTrue(FrameReads.Near(Centre(), Color.red, Close), $"the fringe 3 cm in front, fading from 4: fully: {Centre()}");
            _eye.SetFloat(Ids.EyeHairDistance, 0.035f);
            Color faded = Centre();
            Assert.That(faded.r, Is.InRange(0.2f, 0.8f), $"the limit 3.5 with 1 of fade: partly: {faded}");
            _eye.SetFloat(Ids.EyeHairDistance, 0.025f);
            Assert.IsTrue(FrameReads.Near(Centre(), Color.green, Close), $"past the limit: hidden: {Centre()}");
        }

        [Test]
        public void ThroughThinHairOnly_AStrandShowsIt_ASolidFringeDoesNot()
        {
            Build(true);
            _eye.SetFloat(Ids.HairOverEyes, 0f);
            _eye.SetFloat(Ids.EyeThinHair, 6f);
            _fringe.transform.SetPositionAndRotation(new Vector3(0f, 0f, FringeDepth), Quaternion.identity);
            _fringe.transform.localScale = Vector3.one * 2f;
            Assert.IsTrue(FrameReads.Near(Centre(), Color.green, Close), $"a solid fringe: hidden: {Centre()}");
            _fringe.transform.localScale = new Vector3(0.04f, 2f, 1f);
            Assert.Greater(Centre().r, 0.5f, $"a thin strand: the eye through it: {Centre()}");
        }

        [Test]
        public void TheTint_ColoursTheEyeAsItShows()
        {
            Build(true);
            _eye.SetFloat(Ids.HairOverEyes, 0f);
            TestMaterials.SetLinear(_eye, Ids.EyeThroughTint, new Color(0.5f, 1f, 1f));
            Assert.AreEqual(0.5f, Centre().r, Close, $"red halved: {Centre()}");
        }

        [Test]
        public void TwoLayersThrough_TheNearerIsOnTop()
        {
            Build(true);
            _eye.SetFloat(Ids.HairOverEyes, 0f);
            _farEye = Flat(AnimeShaderIds.EyesShader, Color.blue);
            _farEye.SetFloat(Ids.HairOverEyes, 0f);
            _farEye.SetFloat(Ids.EyeHairDistance, Distance);
            _farEye.SetFloat(Ids.EyeThinHair, 0f);
            TestMaterials.SetLinear(_farEye, Ids.EyeThroughTint, Color.white);
            MeshRenderer far = _stage.Add(_quad, _farEye, new Vector3(0f, 0f, FarLayer), Quaternion.identity, Vector3.one * 0.4f);
            Assert.IsTrue(FrameReads.Near(Centre(), Color.red, Close), $"the nearer (red) over the farther (blue): {Centre()}");
            far.sharedMaterial = _eye;
            _eyeRenderer.sharedMaterial = _farEye;
            Assert.IsTrue(FrameReads.Near(Centre(), Color.blue, Close), $"swapped, the nearer (blue) over the farther (red): {Centre()}");
        }
    }
}
