using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;
using Switches = AnimeShaders.AnimeShaderIds.Switches;

namespace AnimeShaders.Tests
{
    /**
     * <summary>The depth-edge rim, drawn by <see cref="AnimeRimFeature" /> in a made pipeline: a band inside a
     * sphere's silhouette where what lies behind is farther than the threshold, of the width asked, none while fading,
     * none on a renderer without the feature; Brighten lifts the part's own colour (its channels in proportion) where Add
     * adds the same light to each. Every check compares the frame with the rim on to the same frame with it off.</summary>
     */
    public class DepthRimTests
    {
        #region Private Fields

        private const int Size = 512;
        private const float FieldOfView = 30f;
        private const float Distance = 6f;
        private const float Threshold = 1f;
        private const float ReferenceHeight = 1080f;

        private static readonly Color Rim = new(0f, 0.6f, 0f);

        private MadePipeline _pipeline;
        private LitStage _stage;
        private Material _material;
        private MeshRenderer _sphere;
        private DirectorGlobals _director;

        #endregion

        #region Helpers

        private void MakeStage(bool withFeature)
        {
            _pipeline = withFeature ? new MadePipeline(ScriptableObject.CreateInstance<AnimeRimFeature>()) : new MadePipeline();
            _stage = new LitStage(Size);
            _stage.LightFrom(Vector3.back);
            _stage.Perspective(FieldOfView, Distance);
            _sphere = _stage.Add(TestMeshes.Sphere(), _material, Vector3.zero, Quaternion.identity, Vector3.one * 2f);
        }

        /** <summary>Which pixels the rim adds to: the frame with the rim on against the same frame with it off.</summary> */
        private bool[] RimPixels()
        {
            AnimeMaterials.SetRimDepth(_material, false);
            Color[] off = _stage.Render();
            AnimeMaterials.SetRimDepth(_material, true);
            Color[] on = _stage.Render();
            var rim = new bool[on.Length];
            for (int i = 0; i < on.Length; i++)
                rim[i] = on[i].g - off[i].g > Rim.g * 0.25f;
            return rim;
        }

        private static int Count(bool[] mask)
        {
            int count = 0;
            foreach (bool pixel in mask)
            {
                if (pixel)
                    count++;
            }
            return count;
        }

        /** <summary>The band's thickness in pixels on the middle row, from the sphere's left edge.</summary> */
        private int BandThickness()
        {
            bool[] rim = RimPixels();
            int row = Size / 2;
            int x = 0;
            while (x < Size && !rim[row * Size + x])
                x++;
            int run = 0;
            while (x + run < Size && rim[row * Size + x + run])
                run++;
            return run;
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _director = new DirectorGlobals();
            _material = TestMaterials.Character(new Color(0.5f, 0.4f, 0.6f), new Color(0.6f, 0.6f, 0.7f));
            _material.SetFloat(Ids.OutlineWidth, 0f);
            TestMaterials.SetLinear(_material, Ids.RimColor, Rim);
            _material.SetFloat(Ids.RimStrength, 1f);
            _material.SetFloat(Ids.RimLightMask, 0f);
            _material.SetFloat(Ids.RimSoftness, 0.01f);
            _material.SetFloat(Ids.RimDepthWidth, 12f);
            _material.SetFloat(Ids.RimDepthThreshold, Threshold);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _stage?.Dispose();
                Object.DestroyImmediate(_material);
                _director.Dispose();
            }
            finally
            {
                _pipeline?.Dispose();
            }
            Assert.IsFalse(MadePipeline.Active, "the project's pipeline is back");
        }

        [Test]
        public void AgainstNothing_TheBandRunsInsideTheEdge_AndNeverDeeper()
        {
            MakeStage(withFeature: true);
            AnimeMaterials.SetRimDepth(_material, false);
            int covered = FrameReads.Count(_stage.Render(), FrameReads.Covered);
            float radius = Mathf.Sqrt(covered / Mathf.PI);
            bool[] rim = RimPixels();
            Assert.Greater(Count(rim), 0, "the edge takes the rim");

            int band = BandThickness();
            float asked = _material.GetFloat(Ids.RimDepthWidth) * Size / ReferenceHeight;
            Assert.Less(band, radius * 0.5f, "a band, not the whole disc");
            Assert.That(band, Is.InRange(asked - 2f, asked + 2f), $"the asked width in pixels at {ReferenceHeight}p");
            Vector2 centre = new(Size / 2f, Size / 2f);
            for (int i = 0; i < rim.Length; i++)
            {
                if (!rim[i])
                    continue;
                float fromCentre = Vector2.Distance(new Vector2(i % Size + 0.5f, i / Size + 0.5f), centre);
                Assert.Greater(fromCentre, radius - band - 3f, $"rim at {i % Size},{i / Size}, inside the band");
            }
        }

        [Test]
        public void SomethingCloserBehindThanTheThreshold_TakesNoRim_FartherDoes()
        {
            MakeStage(withFeature: true);
            Material wallMaterial = TestMaterials.Character(Color.white, Color.gray);
            Mesh wall = TestMeshes.Quad();
            try
            {
                MeshRenderer wallRenderer = _stage.Add(wall, wallMaterial, new Vector3(0f, 0f, Threshold * 0.3f),
                    Quaternion.identity, Vector3.one * 10f);
                Assert.AreEqual(0, Count(RimPixels()), "a wall closer behind than the threshold");

                wallRenderer.transform.position = new Vector3(0f, 0f, Threshold * 2f);
                Assert.Greater(Count(RimPixels()), 0, "a wall farther behind than the threshold");

                _material.SetFloat(Ids.RimDepthThreshold, Threshold * 4f);
                Assert.AreEqual(0, Count(RimPixels()), "the threshold raised past the gap");
            }
            finally
            {
                Object.DestroyImmediate(wallMaterial);
                Object.DestroyImmediate(wall);
            }
        }

        [Test]
        public void TheBand_FollowsItsWidth()
        {
            MakeStage(withFeature: true);
            _material.SetFloat(Ids.RimDepthWidth, 8f);
            int narrow = BandThickness();
            _material.SetFloat(Ids.RimDepthWidth, 16f);
            int wide = BandThickness();
            Assert.Greater(narrow, 1);
            Assert.That(wide, Is.InRange(2 * narrow - 2, 2 * narrow + 2), $"{narrow} then {wide}");
        }

        [Test]
        public void TheBand_ThinsWithDistance_PastTheFarDistance()
        {
            MakeStage(withFeature: true);
            _material.SetFloat(Ids.RimDepthWidth, 24f);
            _material.SetFloat(Ids.OutlineDistanceMax, 2f);
            _stage.Perspective(FieldOfView, 4f);
            int near = BandThickness();
            _stage.Perspective(FieldOfView, 8f);
            int far = BandThickness();
            Assert.Greater(near, 3);
            Assert.That(far, Is.InRange(near / 2 - 1, near / 2 + 1), $"{near} then {far}");
        }

        [Test]
        public void Orthographic_HoldsTheBandsWidth_AtAnyDistance()
        {
            MakeStage(withFeature: true);
            _stage.Camera.orthographic = true;
            _stage.Camera.orthographicSize = 2f;
            int near = BandThickness();
            _stage.Camera.transform.position = new Vector3(0f, 0f, -12f);
            int far = BandThickness();
            Assert.Greater(near, 1);
            Assert.That(far, Is.InRange(near - 1, near + 1), $"{near} then {far}");
        }

        [Test]
        public void AFadingBody_TakesNoRim()
        {
            MakeStage(withFeature: true);
            Assert.Greater(Count(RimPixels()), 0, "solid, it takes the rim");
            _sphere.SetShaderUserValue(128u);
            Assert.AreEqual(0, Count(RimPixels()));
        }

        [Test]
        public void ARendererWithoutTheFeature_DrawsNoDepthRim()
        {
            MakeStage(withFeature: true);
            Assert.Greater(Count(RimPixels()), 0, "with the feature, the rim shows");
            _stage.Dispose();
            _pipeline.Dispose();
            MakeStage(withFeature: false);
            Assert.AreEqual(0, Count(RimPixels()));
        }

        [Test]
        public void Brighten_LiftsThePartsOwnColour_AddAddsLight()
        {
            MakeStage(true);
            TestMaterials.SetLinear(_material, Ids.RimColor, Color.white);
            _material.SetFloat(Ids.RimStrength, 0.5f);
            AnimeMaterials.SetRimDepth(_material, false);
            Color[] off = _stage.Render();
            AnimeMaterials.SetRimDepth(_material, true);
            int brightest = 0;
            _material.SetFloat(Switches.RimDepthBlend, (float)BlendMode.DstColor);
            Color[] lifted = _stage.Render();
            for (int i = 0; i < lifted.Length; i++)
            {
                if (lifted[i].g - off[i].g > lifted[brightest].g - off[brightest].g)
                    brightest = i;
            }
            Color under = off[brightest];
            Color lift = lifted[brightest];
            Assert.AreEqual(1.5f, lift.r / under.r, 0.05f, $"red brightened by the rim: {under} to {lift}");
            Assert.AreEqual(lift.r / under.r, lift.g / under.g, 0.03f, "green in proportion");
            Assert.AreEqual(lift.r / under.r, lift.b / under.b, 0.03f, "blue in proportion: its own hue, no white");

            _material.SetFloat(Switches.RimDepthBlend, (float)BlendMode.One);
            Color added = _stage.Render()[brightest];
            Assert.AreEqual(added.r - under.r, added.g - under.g, 0.03f, "Add: the same light on each channel");
            Assert.AreEqual(0.5f, added.g - under.g, 0.05f, "Add: the rim's strength in white");
        }
    }
}
